namespace twentySix.NeuralStock.Core.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Helpers;
    using Models;
    using Services.Interfaces;

    public class StrategyI : StrategyBase
    {
        private static readonly object Locker = new object();

        public StrategyI(StrategySettings settings)
        {
            Settings = settings ?? new StrategySettings();

            lock (Locker)
            {
                StatisticsService =
                    ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();
                DataProcessorService =
                    ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IDataProcessorService>();
            }
        }

        public override int Id => 1;

        public override StrategySettings Settings { get; protected set; }

        protected override IList<AnnDataPoint> GetRawAnnDataPoints(HistoricalData historicalData)
        {
            var result = new List<AnnDataPoint>();
            var historicalQuotes = historicalData.Quotes.Values;

            var close = historicalQuotes.Select(x => x.Close).ToArray();
            var volume = historicalQuotes.Select(x => x.Volume).ToArray();
            var high = historicalQuotes.Select(x => x.High).ToArray();
            var low = historicalQuotes.Select(x => x.Low).ToArray();

            var movingAverageCloseFast =
                DataProcessorService.CalculateMovingAverage(close, Settings.MovingAverageCloseFast);
            var movingAverageCloseSlow =
                DataProcessorService.CalculateMovingAverage(high, Settings.MovingAverageCloseSlow);
            var movingAverageVolume = DataProcessorService.CalculateMovingAverage(volume, Settings.MovingAverageVolume);
            var cci = DataProcessorService.CalculateCCI(historicalQuotes, Settings.CCI);
            var rsi = DataProcessorService.CalculateRSI(close, Settings.RSI);
            var macD = DataProcessorService.CalculateMacD(close, Settings.MacdFast, Settings.MacdSlow,
                Settings.MacdSignal);
            var atr = DataProcessorService.CalculateAtr(high, low, close, Settings.Atr);
            var ema = DataProcessorService.CalculateEMA(close, Settings.Ema);
            var wr = DataProcessorService.CalculateWR(historicalQuotes, Settings.Wr);
            var kama = DataProcessorService.CalculateKama(close, Settings.Kama);
            var aroon = DataProcessorService.CalculateAroon(high, low, Settings.Aroon);

            int fwdDays = Settings.FwdDays;
            int yesterdayStep = 1;

            for (int i = 0; i < historicalQuotes.Count; i++)
            {
                var fwdDate = i + fwdDays >= historicalQuotes.Count
                    ? historicalQuotes.Count - 1
                    : i + fwdDays;

                var yesterdayIndex = i - yesterdayStep >= 0 ? i - yesterdayStep : 0;
                var yesterday = historicalQuotes[yesterdayIndex];
                var today = historicalQuotes[i];
                var future = historicalQuotes[fwdDate];

                var percentageChange = (future.Close - today.Close) / today.Close * 100d;

                var annDataPoint = new AnnDataPoint
                {
                    Date = today.Date,
                    Inputs = new[]
                    {
                        today.Close,
                        today.High,
                        today.Open,
                        volume[i],
                        movingAverageVolume[i],
                        movingAverageCloseFast[i],
                        movingAverageCloseSlow[i],
                        cci[i],
                        macD.Item1[i],
                        macD.Item2[i],
                        macD.Item3[i],
                        rsi[i],
                        atr[i],
                        ema[i],
                        wr[i],
                        kama[i],
                        aroon[i]
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
    }
}