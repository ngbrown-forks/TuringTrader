﻿//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        TimeSeries
// Description: Time series class.
// History:     2022x26, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TuringTrader.Simulator.v2
{
    public class TimeSeries<T>
    {
        public readonly Algorithm Algo;
        public readonly string CacheId;
        public readonly Task<List<Tuple<DateTime, T>>> Data;

        public TimeSeries(Algorithm algo, string cacheId, Task<List<Tuple<DateTime, T>>> data)
        {
            Algo = algo;
            CacheId = cacheId;
            Data = data;
        }

        private int _CurrentIndex = 0;
        private int CurrentIndex
        {
            get
            {
                var data = Data.Result;
                var currentDate = Algo.SimDate;

                // move forward in time
                while (_CurrentIndex < data.Count - 1 && data[_CurrentIndex + 1].Item1 <= currentDate)
                    _CurrentIndex++;

                // move back in time
                while (_CurrentIndex > 0 && data[_CurrentIndex - 1].Item1 > currentDate)
                    _CurrentIndex--;

                return _CurrentIndex;

            }
        }
        public T this[int offset]
        {
            get
            {
                var data = Data.Result;
                var baseIdx = CurrentIndex;
                var idx = Math.Max(0, Math.Min(data.Count - 1, baseIdx + offset));

                return data[idx].Item2;
            }
        }
    }

    /// <summary>
    /// Simple container for open/ high/ low/ close prices and volume.
    /// </summary>
    public class OHLCV
    {
        /// <summary>
        /// Opening price.
        /// </summary>
        public readonly double Open;
        /// <summary>
        /// Highest price.
        /// </summary>
        public readonly double High;
        /// <summary>
        /// Lowest price.
        /// </summary>
        public readonly double Low;
        /// <summary>
        /// Closing price.
        /// </summary>
        public readonly double Close;
        /// <summary>
        /// Bar volume.
        /// </summary>
        public readonly double Volume;

        /// <summary>
        /// Construct new OHLCV object
        /// </summary>
        /// <param name="o">opening price</param>
        /// <param name="h">highest price</param>
        /// <param name="l">lowest price</param>
        /// <param name="c">closing price</param>
        /// <param name="v">volume</param>
        public OHLCV(double o, double h, double l, double c, double v)
        {
            Open = o;
            High = h;
            Low = l;
            Close = c;
            Volume = v;
        }

        public override string ToString()
        {
            return string.Format("o={0:C2}, h={1:C2}, l={2:C2}, c={3:C2}, v={4:F0}", Open, High, Low, Close, Volume);
        }
    }

    public class TimeSeriesOHLCV : TimeSeries<OHLCV>
    {
        public TimeSeriesOHLCV(Algorithm algo, string myId, Task<List<Tuple<DateTime, OHLCV>>> myData) : base(algo, myId, myData)
        {
        }

        private TimeSeriesFloat ExtractFieldSeries(string fieldName, Func<OHLCV, double> extractFun)
        {
            List<Tuple<DateTime, double>> extractAsset()
            {
                var ohlcv = Data.Result; // wait until async result is available
                var data = new List<Tuple<DateTime, double>>();

                foreach (var it in ohlcv)
                    data.Add(Tuple.Create(it.Item1, extractFun(it.Item2)));

                return data;
            }

            var cacheId = CacheId + "." + fieldName;
            return new TimeSeriesFloat(
                Algo,
                cacheId,
                Algo.Cache(cacheId, extractAsset));
        }
        public TimeSeriesFloat Open { get => ExtractFieldSeries("Open", bar => bar.Open); }
        public TimeSeriesFloat High { get => ExtractFieldSeries("High", bar => bar.High); }
        public TimeSeriesFloat Low { get => ExtractFieldSeries("Low", bar => bar.Low); }
        public TimeSeriesFloat Close { get => ExtractFieldSeries("Close", bar => bar.Close); }
        public TimeSeriesFloat Volume { get => ExtractFieldSeries("Volume", bar => bar.Volume); }
    }

    public class TimeSeriesFloat : TimeSeries<double>
    {
        public TimeSeriesFloat(Algorithm algo, string myId, Task<List<Tuple<DateTime, double>>> myData) : base(algo, myId, myData)
        {
        }
    }
}

//==============================================================================
// end of file
