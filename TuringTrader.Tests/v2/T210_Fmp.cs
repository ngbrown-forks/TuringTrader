#region libraries
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endregion

namespace TuringTrader.SimulatorV2.Tests;

[TestClass]
public class T210_Fmp
{
    private class DataRetrieval : Algorithm
    {
        public string Description;
        public double FirstOpen;
        public double LastClose;
        public double HighestHigh = 0.0;
        public double LowestLow = 1e99;
        public double NumBars;
        public override void Run()
        {
            StartDate = DateTime.Parse("2019-01-01T16:00-05:00");
            EndDate = DateTime.Parse("2019-01-12T16:00-05:00");
            WarmupPeriod = TimeSpan.FromDays(0);

            SimLoop(() =>
            {
                var a = Asset("fmp:msft");

                if (IsFirstBar)
                {
                    Description = a.Description;
                    FirstOpen = a.Open[0];
                    NumBars = 0;
                }
                if (IsLastBar)
                {
                    LastClose = a.Close[0];
                }
                HighestHigh = Math.Max(HighestHigh, a.High[0]);
                LowestLow = Math.Min(LowestLow, a.Low[0]);
                NumBars++;
            });
        }
    }

    [TestMethod]
    public void Test_DataRetrieval()
    {
        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "TuringTrader", "Cache", "msft");
        if (Directory.Exists(cachePath))
            Directory.Delete(cachePath, true);

        for (int i = 0; i < 2; i++)
        {
            var algo = new DataRetrieval();
            algo.Run();

            Assert.IsTrue(algo.Description.ToLower().Contains("microsoft"));
            Assert.IsTrue(algo.NumBars == 8);
            Assert.IsTrue(Math.Abs(algo.LastClose / algo.FirstOpen - 98.48418 / 95.37062) < 1e-3);
            Assert.IsTrue(Math.Abs(algo.HighestHigh / algo.LowestLow - 100.47684 / 93.11927) < 1e-3);
        }
    }
}