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

    public override void Run()
    {
        //---------- initialization

        StartDate = DateTime.Parse("2015-01-01T16:00-07:00");
        EndDate = DateTime.Parse("2016-12-31T16:00-07:00");

        // note that the warmup period is specified in calendar days
        // while most indicators express their parameters in trading days
        WarmupPeriod = TimeSpan.FromDays(90);

        //---------- simulation

        // SimLoop loops through all timestamps in the range
        var asset = Asset("MSFT");
        var slow = asset.Close.EMA(SlowFilterLength);
        var fast = asset.Close.EMA(FastFilterLength);
        var benchmark = Asset(MarketIndex.SPX);
        var fredUnrate = Asset("FRED:UNRATE");

        SimLoop(() =>
        {
            asset.Allocate(
                // hold the asset while the fast MA is above the slow MA,
                fast[0] > slow[0] ? 1.0 : 0.0,
                // we set the order to fill on tomorrow's open
                OrderType.openNextBar);

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