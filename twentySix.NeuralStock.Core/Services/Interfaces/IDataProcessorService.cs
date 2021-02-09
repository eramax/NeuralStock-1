using System.Collections.Generic;
using Skender.Stock.Indicators;

namespace twentySix.NeuralStock.Core.Services.Interfaces
{
    public interface IDataProcessorService
    {
        double[] Normalize(double[] data, double mean, double std);

        double Normalize(double data, double mean, double std);

        SmaResult[] CalculateMovingAverage(IEnumerable<IQuote> quotes, int period);

        EmaResult[] CalculateEMA(IEnumerable<IQuote> quotes, int period);

        CciResult[] CalculateCci(IEnumerable<IQuote> quotes, int period);

        RsiResult[] CalculateRsi(IEnumerable<IQuote> quotes, int period);

        AtrResult[] CalculateAtr(IEnumerable<IQuote> quotes, int period);

        MacdResult[] CalculateMacd(IEnumerable<IQuote> quotes, int fast, int slow, int signal);

        ObvResult[] CalculateObv(IEnumerable<IQuote> quotes, int period);

        ConnorsRsiResult[] CalculateConnorsRsi(IEnumerable<IQuote> quotes, int period);

        PmoResult[] CalculatePmo(IEnumerable<IQuote> quotes, int period);

        BetaResult[] CalculateBeta(IEnumerable<IQuote> market, IEnumerable<IQuote> quotes, int period);

        VolSmaResult[] CalculateVolSma(IEnumerable<IQuote> quotes, int period);

        MfiResult[] CalculateMfi(IEnumerable<IQuote> quotes, int period);

        PivotPointsResult[] CalculatePivotPoints(IEnumerable<IQuote> quotes, PeriodSize period);

        SlopeResult[] CalculateSlope(IEnumerable<IQuote> quotes, int period);

        StdDevResult[] CalculateStdDev(IEnumerable<IQuote> quotes, int period, int? sma = null);
    }
}