namespace twentySix.NeuralStock.Core.Services
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using Interfaces;
    using Skender.Stock.Indicators;

    [Export(typeof(IDataProcessorService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DataProcessorService : IDataProcessorService
    {
        public double[] Normalize(double[] data, double mean, double std)
        {
            return data.Select(x => (x - mean) / std).ToArray();
        }

        public double Normalize(double data, double mean, double std)
        {
            return (data - mean) / std;
        }

        public SmaResult[] CalculateMovingAverage(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetSma(quotes, period).ToArray();
        }

        public EmaResult[] CalculateEMA(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetEma(quotes, period).ToArray();
        }

        public CciResult[] CalculateCci(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetCci(quotes, period).ToArray();
        }

        public RsiResult[] CalculateRsi(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetRsi(quotes, period).ToArray();
        }

        public AtrResult[] CalculateAtr(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetAtr(quotes, period).ToArray();
        }

        public MacdResult[] CalculateMacd(IEnumerable<IQuote> quotes, int fast, int slow, int signal)
        {
            return Indicator.GetMacd(quotes, fast, slow, signal).ToArray();
        }

        public ObvResult[] CalculateObv(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetObv(quotes, period).ToArray();
        }

        public ConnorsRsiResult[] CalculateConnorsRsi(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetConnorsRsi(quotes, period).ToArray();
        }

        public PmoResult[] CalculatePmo(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetPmo(quotes, period).ToArray();
        }

        public BetaResult[] CalculateBeta(IEnumerable<IQuote> market, IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetBeta(market, quotes, period).ToArray();
        }

        public VolSmaResult[] CalculateVolSma(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetVolSma(quotes, period).ToArray();
        }

        public MfiResult[] CalculateMfi(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetMfi(quotes, period).ToArray();
        }

        public PivotPointsResult[] CalculatePivotPoints(IEnumerable<IQuote> quotes, PeriodSize period)
        {
            return Indicator.GetPivotPoints(quotes, period).ToArray();
        }

        public SlopeResult[] CalculateSlope(IEnumerable<IQuote> quotes, int period)
        {
            return Indicator.GetSlope(quotes, period).ToArray();
        }

        public StdDevResult[] CalculateStdDev(IEnumerable<IQuote> quotes, int period, int? sma = null)
        {
            return Indicator.GetStdDev(quotes, period, sma).ToArray();
        }
    }
}