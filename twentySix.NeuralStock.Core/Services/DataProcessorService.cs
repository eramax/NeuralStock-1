// ReSharper disable StyleCop.SA1407
namespace twentySix.NeuralStock.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using TicTacTec.TA.Library;

    using Models;
    using Interfaces;

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

        public double[] CalculateMovingAverage(double[] data, int period)
        {
            double[] result = new double[data.Length];

            Core.MovingAverage(
                0,
                data.Length - 1,
                data,
                period,
                Core.MAType.Sma,
                out _,
                out _,
                result);
            
            return result;
        }

        public double[] CalculateCCI(ICollection<Quote> quotes, int period)
        {
            double[] result = new double[quotes.Count];

            Core.Cci(
                0,
                quotes.Count - 1,
                quotes.Select(x => x.High).ToArray(),
                quotes.Select(x => x.Low).ToArray(),
                quotes.Select(x => x.Close).ToArray(),
                period,
                out _,
                out _,
                result);
            
            return result;
        }

        public double[] CalculateWR(ICollection<Quote> quotes, int period)
        {
            double[] result = new double[quotes.Count];

            Core.WillR(
                0,
                quotes.Count - 1,
                quotes.Select(x => x.High).ToArray(),
                quotes.Select(x => x.Low).ToArray(),
                quotes.Select(x => x.Close).ToArray(),
                period,
                out _,
                out _,
                result);
            
            return result;
        }

        public double[] CalculateEMA(double[] data, int period)
        {
            double[] result = new double[data.Length];

            Core.Ema(
                0,
                data.Length - 1,
                data,
                period,
                out _,
                out _,
                result);

            return result;
        }

        public Tuple<double[], double[], double[]> CalculateMacD(double[] close, int periodFast, int periodSlow, int signal)
        {
            double[] macd = new double[close.Length];
            double[] macdSignal = new double[close.Length];
            double[] macdHist = new double[close.Length];

            Core.Macd(
                0,
                close.Length - 1,
                close,
                periodFast,
                periodSlow,
                signal,
                out _,
                out _,
                macd,
                macdSignal,
                macdHist
            );

            return Tuple.Create(macd, macdSignal, macdHist);
        }

        public double[] CalculateRSI(double[] data, int period)
        {
            double[] result = new double[data.Length];

            Core.Rsi(
                0,
                data.Length - 1,
                data,
                period,
                out _,
                out _,
                result);

            return result;
        }

        public double[] CalculateKama(double[] data, int period)
        {
            double[] result = new double[data.Length];

            Core.Kama(
                0,
                data.Length - 1,
                data,
                period,
                out _,
                out _,
                result);

            return result;
        }
        
        public double[] CalculateAroon(double[] high, double[] low, int period)
        {
            double[] result = new double[high.Length];

            Core.AroonOsc(
                0,
                high.Length - 1,
                high,
                low,
                period,
                out _,
                out _,
                result);
            
            return result;
        }

        public double[] CalculateAtr(double[] high, double[] low, double[] close, int period)
        {
            double[] result = new double[high.Length];
            
            Core.Atr(
                0,
                high.Length - 1,
                high,
                low,
                close,
                period,
                out _,
                out _,
                result);
            return result;
        }
    }
}