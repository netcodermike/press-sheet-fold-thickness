using System.Text.Json;

try
{
    if (args.Length != 1) throw new ArgumentException("Pass one JSON input file");
    using var data = JsonDocument.Parse(File.ReadAllText(args[0]));
    Console.WriteLine(JsonSerializer.Serialize(Analyze(data.RootElement)));
    return 0;
}
catch (Exception error) when (error is IOException or JsonException or FormatException
                               or ArgumentException or InvalidOperationException
                               or KeyNotFoundException or ArithmeticException or IndexOutOfRangeException)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}

static object Analyze(JsonElement input)
{
    int folds = input.GetProperty("folds").GetInt32();
    decimal clearance = input.GetProperty("clearance_mm").GetDecimal();
    if (folds < 0 || folds > 20 || clearance <= 0)
        throw new ArgumentException("Use 0–20 folds and a positive clearance.");
    var stocks = new List<object>();
    foreach (var stock in input.GetProperty("stocks").EnumerateArray())
    {
        string? paper = stock.GetProperty("paper").GetString();
        decimal caliper = stock.GetProperty("microns").GetDecimal() / 1000m;
        if (string.IsNullOrWhiteSpace(paper) || caliper <= 0)
            throw new ArgumentException("Each stock needs a name and positive caliper.");
        int? lastFit = null;
        for (int n = 0; n <= folds; n++)
            if (caliper * (1 << n) <= clearance) lastFit = n;
        stocks.Add(new { paper, stack_mm = caliper * (1 << folds), last_fit = lastFit });
    }
    if (stocks.Count == 0) throw new ArgumentException("Supply at least one paper stock.");
    return new { layers = 1 << folds, stocks };
}
