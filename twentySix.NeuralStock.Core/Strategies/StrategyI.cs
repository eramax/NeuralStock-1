using System;
using System.Collections.Generic;
using System.Linq;
using twentySix.NeuralStock.Core.Data.Countries;
using twentySix.NeuralStock.Core.Helpers;
using twentySix.NeuralStock.Core.Models;
using twentySix.NeuralStock.Core.Services.Interfaces;

namespace twentySix.NeuralStock.Core.Strategies
{
    public class StrategyI : StrategyBase
    {
        private static readonly object Locker = new object();

        private static readonly Dictionary<HistoricalData, Data> data = new Dictionary<HistoricalData, Data>();

        public StrategyI(StrategySettings settings, NeuralStockSettings appSettings)
        {
            Settings = settings ?? new StrategySettings();

            lock (Locker)
            {
                StatisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();
                DataProcessorService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IDataProcessorService>();
                DownloaderService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IDownloaderService>();
            }
        }

        public override int Id => 1;

        public sealed override StrategySettings Settings { get; protected set; }

        protected override IList<AnnDataPoint> GetRawAnnDataPoints(Stock stock, HistoricalData historicalData)
        {
            var result = new List<AnnDataPoint>();

            lock (Locker)
            {
                if (!data.ContainsKey(historicalData))
                {
                    data.Add(historicalData, Data.GetData(stock, DownloaderService, historicalData));
                }
            }

            var movingAverageFast = DataProcessorService.CalculateMovingAverage(data[historicalData].Quotes, Settings.MovingAverageCloseFast);
            var cci = DataProcessorService.CalculateCci(data[historicalData].Quotes, Settings.CCI);
            var rsi = DataProcessorService.CalculateRsi(data[historicalData].Quotes, Settings.RSI);
            var macD = DataProcessorService.CalculateMacd(data[historicalData].Quotes, Settings.MacdFast, Settings.MacdSlow, Settings.MacdSignal);
            var atr = DataProcessorService.CalculateAtr(data[historicalData].Quotes, Settings.Atr);
            var ema = DataProcessorService.CalculateEMA(data[historicalData].Quotes, Settings.Ema);
            var obv = DataProcessorService.CalculateObv(data[historicalData].Quotes, Settings.Obv);
            var crsi = DataProcessorService.CalculateConnorsRsi(data[historicalData].Quotes, Settings.ConnorsRsi);
            var pmo = DataProcessorService.CalculatePmo(data[historicalData].Quotes, Settings.Pmo);
            var roc = DataProcessorService.CalculateRoc(data[historicalData].Quotes, Settings.Roc);

            int fwdDays = Settings.FwdDays;
            int yesterdayStep = 1;

            for (int i = 0; i < historicalData.Quotes.Values.Count; i++)
            {
                var fwdDate = i + fwdDays >= historicalData.Quotes.Values.Count
                    ? historicalData.Quotes.Values.Count - 1
                    : i + fwdDays;

                var yesterdayIndex = i - yesterdayStep >= 0 ? i - yesterdayStep : 0;
                var yesterday = historicalData.Quotes.Values[yesterdayIndex];
                var today = historicalData.Quotes.Values[i];

                var future = historicalData.Quotes.Values[fwdDate];

                var percentageChange = (future.Close - today.Close) / today.Close * 100d;

                var annDataPoint = new AnnDataPoint
                {
                    Date = today.Date,
                    Inputs = new[]
                    {
                        today.Close,
                        today.High,
                        today.Volume,
                        Compare(today.Close, yesterday.Close),
                        Compare(today.High, yesterday.High),
                        Compare(today.Volume, yesterday.Volume),
                        (double)movingAverageFast.Single(x => x.Date.Date == today.Date.Date).Sma.GetValueOrDefault(),
                        (double)ema.Single(x => x.Date.Date == today.Date.Date).Ema.GetValueOrDefault(),
                        (double)cci.Single(x => x.Date.Date == today.Date.Date).Cci.GetValueOrDefault(),
                        (double)rsi.Single(x => x.Date.Date == today.Date.Date).Rsi.GetValueOrDefault(),
                        (double)atr.Single(x => x.Date.Date == today.Date.Date).Atrp.GetValueOrDefault(),
                        (double)macD.Single(x => x.Date.Date == today.Date.Date).Macd.GetValueOrDefault(),
                        (double)obv.Single(x => x.Date.Date == today.Date.Date).Obv,
                        (double)crsi.Single(x => x.Date.Date == today.Date.Date).ConnorsRsi.GetValueOrDefault(),
                        (double)pmo.Single(x => x.Date.Date == today.Date.Date).Pmo.GetValueOrDefault(),
                        (double)roc.Single(x => x.Date.Date == today.Date.Date).Roc.GetValueOrDefault()
                    },
                    Outputs = new[]
                    {
                        percentageChange > Settings.PercentageChangeHigh ? 1d :
                        percentageChange < Settings.PercentageChangeLow ? -1d : 0d
                    }
                };

                result.Add(annDataPoint);
            }

            return result;
        }

        private static double Compare(double value1, double value2)
        {
            if (value1 > value2)
            {
                return 1d;
            }

            if (Math.Abs(value1 - value2) < 1.0E-6)
            {
                return 0d;
            }

            return -1d;
        }

        internal class Data
        {
            public IList<Skender.Stock.Indicators.Quote> CommodityQuotes { get; set; }
            public IList<Skender.Stock.Indicators.Quote> MarketQuotes { get; set; }
            public IList<Skender.Stock.Indicators.Quote> Quotes { get; set; }

            public static Data GetData(Stock stock, IDownloaderService downloaderService, HistoricalData historicalData)
            {
                Data result = new Data();

                result.Quotes = historicalData.Quotes.Values.Select(x => new Skender.Stock.Indicators.Quote
                {
                    Date = x.Date,
                    Low = Convert.ToDecimal(x.Low),
                    High = Convert.ToDecimal(x.High),
                    Open = Convert.ToDecimal(x.Open),
                    Close = Convert.ToDecimal(x.Close),
                    Volume = Convert.ToDecimal(x.Volume)
                }).ToList();

                var market = new Stock() { Country = new Others(), Symbol = stock.Country.Id == Singapore.CountryId ? "^STI" : "^PSI20" };
                market.HistoricalData = downloaderService.GetHistoricalData(market, historicalData.BeginDate, historicalData.EndDate).Result;

                var commodity = new Stock() { Country = new Others(), Symbol = "O87.SI" };
                commodity.HistoricalData = downloaderService.GetHistoricalData(commodity, historicalData.BeginDate, historicalData.EndDate).Result;

                result.MarketQuotes = new List<Skender.Stock.Indicators.Quote>();
                result.CommodityQuotes = new List<Skender.Stock.Indicators.Quote>();

                for (var i = 0; i < historicalData.Quotes.Count; i++)
                {
                    var date = historicalData.Quotes.ElementAt(i).Key;
                    var closestMarketDate = market.HistoricalData.Quotes.Keys.LastOrDefault(x => x <= date);
                    var closestCommodityDate = commodity.HistoricalData.Quotes.Keys.LastOrDefault(x => x <= date);

                    if (closestMarketDate == default)
                    {
                        result.MarketQuotes.Add(new Skender.Stock.Indicators.Quote { Date = date });
                    }
                    else
                    {
                        result.MarketQuotes.Add(new Skender.Stock.Indicators.Quote
                        {
                            Date = date,
                            Low = Convert.ToDecimal(market.HistoricalData.Quotes[closestMarketDate].Low),
                            High = Convert.ToDecimal(market.HistoricalData.Quotes[closestMarketDate].High),
                            Open = Convert.ToDecimal(market.HistoricalData.Quotes[closestMarketDate].Open),
                            Close = Convert.ToDecimal(market.HistoricalData.Quotes[closestMarketDate].Close),
                            Volume = Convert.ToDecimal(market.HistoricalData.Quotes[closestMarketDate].Volume)
                        });
                    }

                    if (closestCommodityDate == default)
                    {
                        result.CommodityQuotes.Add(new Skender.Stock.Indicators.Quote { Date = date });
                    }
                    else
                    {
                        result.CommodityQuotes.Add(new Skender.Stock.Indicators.Quote
                        {
                            Date = date,
                            Low = Convert.ToDecimal(commodity.HistoricalData.Quotes[closestCommodityDate].Low),
                            High = Convert.ToDecimal(commodity.HistoricalData.Quotes[closestCommodityDate].High),
                            Open = Convert.ToDecimal(commodity.HistoricalData.Quotes[closestCommodityDate].Open),
                            Close = Convert.ToDecimal(commodity.HistoricalData.Quotes[closestCommodityDate].Close),
                            Volume = Convert.ToDecimal(commodity.HistoricalData.Quotes[closestCommodityDate].Volume)
                        });
                    }
                }

                return result;
            }
        }
    }
}