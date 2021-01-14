namespace twentySix.NeuralStock.Core.Services.Interfaces
{
    using System;
    using System.Collections.Generic;

    using Models;

    public interface IDataProcessorService
    {
        double[] Normalize(double[] data, double mean, double std);

        double Normalize(double data, double mean, double std);

        double[] CalculateMovingAverage(double[] data, int period);

        double[] CalculateCCI(ICollection<Quote> quotes, int period);

        Tuple<double[], double[], double[]> CalculateMacD(double[] close, int periodFast, int periodSlow, int signal);

        double[] CalculateRSI(double[] data, int period);
        
        double[] CalculateKama(double[] data, int period);

        double[] CalculateWR(ICollection<Quote> quotes, int period);

        double[] CalculateEMA(double[] data, int period);

        double[] CalculateAroon(double[] high, double[] low, int period);

        double[] CalculateAtr(double[] high, double[] low, double[] close, int period);
    }
}