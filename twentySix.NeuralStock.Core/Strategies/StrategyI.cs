using CoordinateSharp;

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
        private static readonly Tuple<double, double> coordinates = new Tuple<double, double>(38.766667, -9.15);
        private static readonly Dictionary<DateTime, Coordinate> coordinatesCache = new Dictionary<DateTime, Coordinate>();

        private static readonly object Locker = new object();

        public StrategyI(StrategySettings settings)
        {
            Settings = settings ?? new StrategySettings();

            lock (Locker)
            {
                StatisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();
                DataProcessorService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IDataProcessorService>();
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

            var movingAverageCloseFast = DataProcessorService.CalculateMovingAverage(close, Settings.MovingAverageCloseFast);
            var movingAverageCloseSlow = DataProcessorService.CalculateMovingAverage(close, Settings.MovingAverageCloseSlow);
            var cci = DataProcessorService.CalculateCCI(historicalQuotes, Settings.CCI);
            var rsi = DataProcessorService.CalculateRSI(close, Settings.RSI);
            var rsi2 = DataProcessorService.CalculateRSI(high, Settings.RSI2);
            var macD = DataProcessorService.CalculateMacD(close, Settings.MacdFast, Settings.MacdSlow, Settings.MacdSignal);
            var hv = DataProcessorService.CalculateHV(close, Settings.Hv1);
            var fitClose = DataProcessorService.CalculateMovingLinearFit(close, Settings.FitClose);
            var fitOfFit = DataProcessorService.CalculateMovingLinearFit(fitClose.Item2, Settings.FitOfFit);
            var fitRSI = DataProcessorService.CalculateMovingLinearFit(DataProcessorService.CalculateRSI(close, Settings.RSI1Fit), Settings.RSI2Fit);

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

                if (!coordinatesCache.ContainsKey(today.Date))
                {
                    coordinatesCache.Add(today.Date, new Coordinate(coordinates.Item1, coordinates.Item2, new DateTime(today.Date.Year, today.Date.Month, today.Date.Day, 10, 0, 0)));
                }
                if (!coordinatesCache.ContainsKey(yesterday.Date))
                {
                    coordinatesCache.Add(yesterday.Date, new Coordinate(coordinates.Item1, coordinates.Item2, new DateTime(yesterday.Date.Year, yesterday.Date.Month, yesterday.Date.Day, 10, 0, 0)));
                }

                var annDataPoint = new AnnDataPoint
                {
                    Date = today.Date,
                    Inputs = new[]
                                 {
                                     Math.Sinh(rsi[i]) / (Math.Sinh(rsi2[i])+1.0E-6),
                                     today.Close/today.High,
                                     yesterday.Close/yesterday.High,
                                     volume[i]/(volume[yesterdayIndex] + 1.0E-6),
                                     movingAverageCloseFast[i],
                                     movingAverageCloseSlow[i],
                                     cci[i],
                                     today.Date.Month,
                                     (int)today.Date.DayOfWeek,
                                     macD.Item1[i],
                                     macD.Item2[i],
                                     rsi[i],
                                     hv[i],
                                     fitClose.Item2[i],
                                     fitOfFit.Item2[i],
                                     fitRSI.Item2[i],
                                     (today.Close * yesterday.Volume - yesterday.Close * today.Volume) * today.Dividend,
                                     coordinatesCache[today.Date].CelestialInfo.SunAltitude,
                                     coordinatesCache[today.Date].CelestialInfo.SunAzimuth
                                 },
                    Outputs = new[]
                                  {
                                      percentageChange > Settings.PercentageChangeHigh ? 1d : percentageChange < Settings.PercentageChangeLow ? -1d : 0d
                                  }
                };

                result.Add(annDataPoint);
            }

            return result;
        }
    }
}