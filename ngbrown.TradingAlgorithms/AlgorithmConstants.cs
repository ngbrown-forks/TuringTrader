namespace ngbrown.TradingAlgorithms;

public static class AlgorithmConstants
{
    public static readonly DateTime START_DATE = DateTime.Parse("2007-01-01T16:00-05:00"); // 4pm in New York
    public static readonly DateTime END_DATE = DateTime.Now.Date - TimeSpan.FromDays(5);
    public static readonly double FRICTION = 0.005; // $100.00 x 0.5% = $0.5
}