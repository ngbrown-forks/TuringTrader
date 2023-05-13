using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace TuringTrader.SimulatorV2;

public static partial class DataSource
{
    #region internal helpers
    private static string _fmpApiToken => Simulator.GlobalSettings.FmpApiKey;
    private static string _fmpConvertTicker(string ticker) => ticker;
    #endregion

    private class _fmpUniverse
    {
        private Algorithm _algorithm;
        private string _universe;
        private readonly List<JObject> changes;
        private readonly List<JObject> mostRecent;

        public _fmpUniverse(Algorithm algo, string universe)
        {
            _algorithm = algo;
            _universe = universe;

                        string constituent;
            switch (_universe.ToUpperInvariant())
            {
                case "$SPX":
                    constituent = "sp500";
                    break;
                case "$DJU":
                    constituent = "dowjones";
                    break;
                default:
                    throw new InvalidOperationException($"Unknown universe: {_universe}");
            }

            var info = new Dictionary<DataSourceParam, string>
            {
                { DataSourceParam.nickName, "fmp:" + _universe },
                { DataSourceParam.nickName2, _universe.ToUpperInvariant() },
                { DataSourceParam.dataFeed, "fmp" }
            };

            this.mostRecent = _loadCacheHelper<JArray, List<JObject>>(_algorithm, info,
                () =>
                {
                    // retrieve list from Financial Modeling Prep
                    string url = string.Format("https://financialmodelingprep.com/api/v3/{0}_constituent?apikey={1}",
                        constituent,
                        _fmpApiToken);

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (raw) =>
                {
                    // parse data and check validity
                    try
                    {
                        if (raw == null || raw.Length < 10)
                            return null;

                        var json = JArray.Parse(raw);
                        var data = ((JObject)json.First);

                        if (data == null || !data.HasValues || data["symbol"].Type == JTokenType.Null)
                            return null;

                        // this seems to be valid meta data
                        return json;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (jsonData) =>
                {
                    return jsonData.Select(x => (JObject)x).ToList();
                },
                $"universe_{constituent}");

            this.changes = _loadCacheHelper<JArray, List<JObject>>(_algorithm, info,
                () =>
                {
                    // retrieve changes from Financial Modeling Prep
                    string url = string.Format("https://financialmodelingprep.com/api/v3/historical/{0}_constituent?apikey={1}",
                        constituent,
                        _fmpApiToken);

                    using (var client = new HttpClient())
                        return client.GetStringAsync(url).Result;
                },
                (raw) =>
                {
                    // parse data and check validity
                    try
                    {
                        if (raw == null || raw.Length < 10)
                            return null;

                        var json = JArray.Parse(raw);
                        var data = ((JObject)json.First);

                        if (data == null || !data.HasValues || data["reason"].Type == JTokenType.Null)
                            return null;

                        // this seems to be valid meta data
                        return json;
                    }
                    catch
                    {
                        return null;
                    }
                },
                (jsonData) =>
                {
                    return jsonData.Select(x => (JObject)x).ToList();
                },
                $"universe_{constituent}_historical");

            // TODO: walk back through the history and re-construct the state on any given day
        }

        public HashSet<string> Constituents()
        {
            var localClose = _algorithm.SimDate;
            var exchangeTime = TimeZoneInfo.ConvertTime(localClose, _algorithm.TradingCalendar.ExchangeTimeZone);

            var constituents = new HashSet<string>();
            foreach (var obj in this.mostRecent)
            {
                var symbol = (string)obj["symbol"];
                constituents.Add("fmp:" + symbol);
            }
            return constituents;
        }
    }

    private static List<BarType<OHLCV>> FmpLoadData(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
        _loadDataHelper<JObject>(
            algo, info,
            () =>
            {   // retrieve data from Financial Modeling Prep
                string url = string.Format(
                    "https://financialmodelingprep.com/api/v3/historical-price-full/{0}"
                    + "?from={1:yyyy}-{1:MM}-{1:dd}"
                    + "&to={2:yyyy}-{2:MM}-{2:dd}"
                    + "&apikey={3}",
                    _fmpConvertTicker(info[DataSourceParam.symbolFmp]),
                    DateTime.Parse("01/01/1950", CultureInfo.InvariantCulture),
                    DateTime.Now + TimeSpan.FromDays(5),
                    _fmpApiToken);

                using (var client = new HttpClient())
                    return client.GetStringAsync(url).Result;
            },
            (raw) =>
            {   // parse data and check validity
                try
                {
                    if (raw == null || raw.Length < 25)
                        return null;

                    var json = JObject.Parse(raw);

                    if (!json.HasValues)
                        return null;

                    return json;
                }
                catch
                {
                    return null;
                }
            },
            (jsonData) =>
            {   // extract data for TuringTrader
                var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
                var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

                using var e = ((JArray)jsonData["historical"]).GetEnumerator();

                var bars = new List<BarType<OHLCV>>();
                while (e.MoveNext())
                {
                    var bar = e.Current;

                    var exchangeClose = DateTime.Parse((string)bar["date"], CultureInfo.InvariantCulture);
                    var localClose = exchangeClose;

                    double o = (double)bar["open"];
                    double h = (double)bar["high"];
                    double l = (double)bar["low"];
                    double c = (double)bar["close"];
                    long v = (long)bar["unadjustedVolume"];
                    double ac = (double)bar["adjClose"];

                    // adjust prices according to the adjusted close.
                    // note the volume is adjusted the opposite way.
                    double ao = o * ac / c;
                    double ah = h * ac / c;
                    double al = l * ac / c;
                    long av = (long)(v * c / ac);

                    bars.Add(new BarType<OHLCV>(
                        localClose,
                        new OHLCV(ao, ah, al, ac, av)));
                }

                bars.Reverse();
                return bars;
            });

    private static TimeSeriesAsset.MetaType FmpLoadMeta(Algorithm algo, Dictionary<DataSourceParam, string> info) =>
        _loadMetaHelper<JArray>(
            algo, info,
            () =>
            {   // retrieve meta from Financial Modeling Prep
                string url = string.Format("https://financialmodelingprep.com/api/v3/quote/{0}?apikey={1}",
                    _fmpConvertTicker(info[DataSourceParam.symbolFmp]),
                    _fmpApiToken);

                using (var client = new HttpClient())
                    return client.GetStringAsync(url).Result;
            },
            (raw) =>
            {   // parse data and check validity
                try
                {
                    if (raw == null || raw.Length < 10)
                        return null;

                    var json = JArray.Parse(raw);
                    var data = ((JObject)json.First);

                    if (data == null || !data.HasValues || data["name"].Type == JTokenType.Null)
                        return null;

                    // this seems to be valid meta data
                    return json;
                }
                catch
                {
                    return null;
                }
            },
            (jsonData) =>
            {
                var data = ((JObject)jsonData.First);
                return new TimeSeriesAsset.MetaType
                {
                    // extract meta for TuringTrader
                    Ticker = (string)data["symbol"],
                    Description = (string)data["name"],
                };
            });

    private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> FmpGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
    {
        return Tuple.Create(
            FmpLoadData(owner, info),
            FmpLoadMeta(owner, info));
    }

    private static HashSet<string> FmpGetUniverse(Algorithm owner, string universe)
    {
        var theUniverse = owner.ObjectCache.Fetch(
            string.Format("Universe({0})", universe),
            () => new _fmpUniverse(owner, universe));

        return theUniverse.Constituents();
    }
}