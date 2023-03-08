namespace ngbrown.TradingAlgorithms;

/// <summary>
/// Selection of FRED economic data series.
/// Find more at https://fred.stlouisfed.org/categories
/// </summary>
public static class FredEconomicData
{
    /// <summary>
    /// 20-year T-Bond Yield (since 01/1962)
    /// </summary>
    public static string BOND_LT_YIELD { get; } = "fred:DGS20";

    /// <summary>
    /// 13-week T-Bill Yield (since 01/1954)
    /// </summary>
    public static string BOND_ST_YIELD { get; } = "fred:DTB3";

    /// <summary>
    /// 3-Month Treasury Bill: Secondary Market Rate (since 01/1954)
    /// </summary>
    public static string RF_YIELD { get; } = "fred:DTB3";

    /// <summary>
    /// producer price index to substitute CRB (since 01/1913)
    /// </summary>
    public static string COMMODITY_INDEX { get; } = "fred:PPIACO";

    /// <summary>
    /// Unemployment Rate, monthly, seasonally adjusted (since 01/1948)
    /// </summary>
    public static string UNEMPLOYMENT { get; } = "fred:UNRATE";

    /// <summary>
    /// Effective Federal Funds Rate (since 07/2000)
    /// </summary>
    public static string FED_RATE { get; } = "fred:EFFR";
}