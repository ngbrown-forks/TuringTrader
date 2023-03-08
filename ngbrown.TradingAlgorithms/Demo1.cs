using TuringTrader.Optimizer;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Assets;
using TuringTrader.SimulatorV2.Indicators;

namespace ngbrown.TradingAlgorithms;

public class Demo1 : Algorithm
{
    [OptimizerParam(20, 90, 2)]
    public int SlowFilterLength { get; set; } = 63;

    [OptimizerParam(5, 40, 2)]
    public int FastFilterLength { get; set; } = 21;

    protected virtual bool IS_TRADING_DAY
        => IsFirstBar || (SimDate.DayOfWeek == DayOfWeek.Tuesday);

    public override void Run()
    {
        //---------- initialization

        StartDate = DateTime.Parse("2015-01-01T16:00-07:00");
        EndDate = DateTime.Parse("2016-12-31T16:00-07:00");
        ((Account_Default)Account).Friction = AlgorithmConstants.FRICTION;

        // note that the warmup period is specified in calendar days
        // while most indicators express their parameters in trading days
        WarmupPeriod = TimeSpan.FromDays(90);

        //---------- simulation

        // SimLoop loops through all timestamps in the range
        var asset = Asset("MSFT");
        var slow = asset.Close.EMA(SlowFilterLength);
        var fast = asset.Close.EMA(FastFilterLength);
        var benchmark = Asset(MarketIndex.SPX);
        var fredUnrate = Asset(FredEconomicData.UNEMPLOYMENT);

        SimLoop(() =>
        {
            if (IS_TRADING_DAY)
            {
                var isHolding = asset.Position > 0.1;
                if (isHolding && asset[-5].Open < (asset[-1].Open / (1 + AlgorithmConstants.FRICTION)))
                {
                    asset.Allocate(
                        0.0,
                        // we set the order to fill on tomorrow's open
                        OrderType.openNextBar);
                }
                else if (!isHolding && asset[-5].Open > (asset[-1].Open * (1 + AlgorithmConstants.FRICTION)))
                {
                    // hold the asset
                    asset.Allocate(
                        1.0,
                        // we set the order to fill on tomorrow's open
                        OrderType.openNextBar);
                }
            }

            if (!IsOptimizing && !IsDataSource && SimDate >= StartDate)
            {
                Plotter.SelectChart(Name, "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot(Name, NetAssetValue);
                Plotter.Plot(asset.Description, asset.Close[0]);
                Plotter.Plot("S&P 500", benchmark.Close[0]);

                Plotter.SelectChart("Signals", "Date");
                Plotter.SetX(SimDate);
                Plotter.Plot(asset.Description, asset.Close[0]);
                Plotter.Plot("slow-ema", slow[0]);
                Plotter.Plot("fast-ema", fast[0]);
                Plotter.Plot("Unrate", fredUnrate.Close[0]);
            }
        });

        //========== post processing ==========

        if (!IsOptimizing && !IsDataSource)
        {
            Plotter.AddTargetAllocation();
            Plotter.AddHistoricalAllocations();
            Plotter.AddTradeLog();
        }
    }

    // to render the charts, we use pre-defined templates
    //public override void Report() => Plotter.OpenWith("SimpleChart");
}