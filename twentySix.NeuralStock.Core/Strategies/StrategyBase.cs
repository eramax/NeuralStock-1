namespace twentySix.NeuralStock.Core.Strategies
{
    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using Services.Interfaces;

    public abstract class StrategyBase : IStrategy
    {
        public abstract int Id { get; }

        public abstract StrategySettings Settings { get; protected set; }

        public double[] TrainingMeansInput { get; set; }

        public double[] TrainingMeansOutput { get; set; }

        public double[] TrainingStdDevsInput { get; set; }

        public double[] TrainingStdDevsOutput { get; set; }

        protected IDataProcessorService DataProcessorService { get; set; }

        protected IStatisticsService StatisticsService { get; set; }

        protected IDownloaderService DownloaderService {get; set; }

        public List<AnnDataPoint> GetAnnData(Stock stock, HistoricalData historicalData, bool recalculateMeans = true)
        {
            var rawAnnDataPoints = GetRawAnnDataPoints(stock, historicalData);
            var numberOfInputs = rawAnnDataPoints.First().Inputs.Length;
            var numberOfOutputs = rawAnnDataPoints.First().Outputs.Length;

            // calculate means and stddevs if we are dea
            if (recalculateMeans)
            {
                TrainingMeansInput = new double[numberOfInputs];
                TrainingStdDevsInput = new double[numberOfInputs];
                TrainingMeansOutput = new double[numberOfOutputs];
                TrainingStdDevsOutput = new double[numberOfOutputs];

                for (int i = 0; i < numberOfInputs; i++)
                {
                    TrainingMeansInput[i] = StatisticsService.Mean(rawAnnDataPoints.Select(x => x.Inputs[i]).ToArray());
                    TrainingStdDevsInput[i] = StatisticsService.StandardDeviation(rawAnnDataPoints.Select(x => x.Inputs[i]).ToArray());
                }

                for (int i = 0; i < numberOfOutputs; i++)
                {
                    TrainingMeansOutput[i] = StatisticsService.Mean(rawAnnDataPoints.Select(x => x.Outputs[i]).ToArray());
                    TrainingStdDevsOutput[i] = StatisticsService.StandardDeviation(rawAnnDataPoints.Select(x => x.Outputs[i]).ToArray());
                }
            }

            return Normalize(rawAnnDataPoints);
        }

        protected abstract IList<AnnDataPoint> GetRawAnnDataPoints(Stock stock, HistoricalData historicalData);

        private List<AnnDataPoint> Normalize(IList<AnnDataPoint> data)
        {
            var result = new List<AnnDataPoint>();
            var numberOfInputs = data.First().Inputs.Length;
            var numberOfOutputs = data.First().Outputs.Length;

            foreach (var annPoint in data)
            {
                result.Add(new AnnDataPoint
                {
                    Date = annPoint.Date,
                    Inputs = Enumerable.Range(0, numberOfInputs).Select(i => DataProcessorService.Normalize(annPoint.Inputs[i], TrainingMeansInput[i], TrainingStdDevsInput[i])).ToArray(),
                    Outputs = Enumerable.Range(0, numberOfOutputs).Select(i => DataProcessorService.Normalize(annPoint.Outputs[i], TrainingMeansOutput[i], TrainingStdDevsOutput[i])).ToArray()
                });
            }

            return result;
        }
    }
}