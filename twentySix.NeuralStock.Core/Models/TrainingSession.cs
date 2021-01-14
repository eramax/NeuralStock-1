using System.Collections.Concurrent;
using MathNet.Numerics.Statistics;

namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DevExpress.Mvvm;
    using FANNCSharp;
    using FANNCSharp.Double;
    using twentySix.NeuralStock.Core.DTOs;
    using twentySix.NeuralStock.Core.Enums;
    using twentySix.NeuralStock.Core.Extensions;
    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Messages;
    using twentySix.NeuralStock.Core.Services.Interfaces;
    using twentySix.NeuralStock.Core.Strategies;

    public class TrainingSession : BindableBase
    {
        private static readonly object Locker = new object();

        private readonly IStatisticsService _statisticsService;

        private readonly List<Prediction> _cachedPredictions = new List<Prediction>();

        private volatile int _numberPredictionsComplete;

        private DateTime _startTime;

        private static readonly ActivationFunction[] possibleActivationFunctions =
        {
            ActivationFunction.ELLIOT,
            ActivationFunction.GAUSSIAN,
            ActivationFunction.SIGMOID,
            ActivationFunction.ELLIOT_SYMMETRIC,
            ActivationFunction.GAUSSIAN_SYMMETRIC,
            ActivationFunction.SIGMOID_SYMMETRIC
        };

        private static readonly TrainingAlgorithm[] possibleTrainingAlgorithms =
        {
            TrainingAlgorithm.TRAIN_RPROP,
            TrainingAlgorithm.TRAIN_QUICKPROP
        };

        public TrainingSession(StockPortfolio portfolio, Stock stock)
        {
            _statisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();

            Portfolio = portfolio;
            Stock = stock;
        }

        public TrainingSession(StockPortfolio portfolio, Stock stock, BestNetworkDTO dto, NeuralStockSettings settings)
        {
            _statisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();
            Portfolio = portfolio;
            Stock = stock;

            TrainSamplePercentage = settings.PercentageTraining;
            NumberAnns = settings.NumberANNs;
            NumberHiddenLayers = settings.NumberHiddenLayers;
            NumberNeuronsPerHiddenLayer = settings.NumberNeuronsHiddenLayer;
            NumberDaysBetweenTransactions = settings.NumberDaysBetweenTransactions;

            BuyLevel = dto.BuyLevel;
            SellLevel = dto.SellLevel;

            var strategy = new StrategyI(StrategySettings.FromJson(dto.StrategySettings))
            {
                TrainingMeansInput = dto.TrainingMeansInput?.ToArray(),
                TrainingStdDevsInput = dto.TrainingStdDevsInput?.ToArray(),
                TrainingMeansOutput = dto.TrainingMeansOutput?.ToArray(),
                TrainingStdDevsOutput = dto.TrainingStdDevsOutput?.ToArray()
            };

            var tmpFileName = Path.GetTempFileName();
            File.WriteAllBytes(tmpFileName, dto.BestNeuralNet);
            var net = new NeuralNet(tmpFileName);

            _cachedPredictions.Clear();
            SplitTrainTestData();

            var trainingTestingData = PrepareAnnData(strategy, false);

            var prediction = Predict(trainingTestingData, net, false);
            var profitLossCalculator = new ProfitLossCalculator(Portfolio.Reset(), this, prediction.Item1);
            _cachedPredictions.Add(new Prediction(profitLossCalculator, strategy, net, prediction.Item2,
                prediction.Item3));
        }

        public int NumberAnns { get; set; } = 10;

        public double TrainSamplePercentage { get; set; } = 0.6;

        public int NumberHiddenLayers { get; set; } = 1;

        public int NumberNeuronsPerHiddenLayer { get; set; } = 10;

        public int NumberDaysBetweenTransactions { get; set; } = 3;

        public double BuyLevel
        {
            get => GetProperty(() => BuyLevel);
            set => SetProperty(() => BuyLevel, value);
        }

        public double SellLevel
        {
            get => GetProperty(() => SellLevel);
            set => SetProperty(() => SellLevel, value);
        }

        public StockPortfolio Portfolio { get; set; }

        public Stock Stock { get; }

        public HistoricalData TrainingHistoricalData
        {
            get => GetProperty(() => TrainingHistoricalData);
            set => SetProperty(() => TrainingHistoricalData, value);
        }

        public HistoricalData TestingHistoricalData
        {
            get => GetProperty(() => TestingHistoricalData);
            set => SetProperty(() => TestingHistoricalData, value);
        }

        public double Progress
        {
            get => GetProperty(() => Progress);
            set => SetProperty(() => Progress, value);
        }

        public TimeSpan TimeLeft
        {
            get => GetProperty(() => TimeLeft);
            set => SetProperty(() => TimeLeft, value);
        }

        public IEnumerable<Prediction> CachePredictions => _cachedPredictions;

        public ProfitLossCalculator BestProfitLossCalculator
        {
            get
            {
                lock (Locker)
                {
                    return _cachedPredictions
                        ?.OrderByDescending(x =>
                            x.ProfitLossCalculator.PL * x.ProfitLossCalculator.PercentageWinningTransactions)
                        .FirstOrDefault()?.ProfitLossCalculator;
                }
            }
        }

        public Prediction BestPrediction => _cachedPredictions
            .OrderByDescending(x => x.ProfitLossCalculator.PL * x.ProfitLossCalculator.PercentageWinningTransactions)
            .FirstOrDefault();

        public Dictionary<string, int> AllNetworksPLs =>
            _statisticsService.Bucketize(_cachedPredictions.ToList().Select(x => x.ProfitLossCalculator.PL).ToArray(),
                14);

        public List<Tuple<double, double>> AllNetworksPLsStdDevs
        {
            get
            {
                lock (Locker)
                {
                    return _cachedPredictions.Select(x =>
                        Tuple.Create(x.ProfitLossCalculator.PL, x.ProfitLossCalculator.StandardDeviationPL)).ToList();
                }
            }
        }

        public double AllNetworksPL => _statisticsService.Median(AllNetworksPLsStdDevs.Select(x => x.Item1).ToArray());

        public double AllNetworksStdDev =>
            _statisticsService.StandardDeviation(AllNetworksPLsStdDevs.Select(x => x.Item1).ToArray());

        public double AllNetworksMin =>
            AllNetworksPLsStdDevs.Any() ? AllNetworksPLsStdDevs.Select(x => x.Item1).Min() : 0;

        public double AllNetworksSigma => AllNetworksPL != 0 ? AllNetworksStdDev / AllNetworksPL : 0;

        public void FindBestAnn(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            Messenger.Default.Send(new TrainStatusMessage("Preparing data ..."));
            _cachedPredictions.Clear();
            SplitTrainTestData();
            _startTime = DateTime.Now;
            Messenger.Default.Send(new TrainStatusMessage("Calculating random walk profit ..."));

            var randomPL = GetRandomPL(TestingHistoricalData.Quotes.Select(x => x.Key), token);

            Messenger.Default.Send(new TrainStatusMessage("Training ..."));

            Parallel.For(
                0,
                NumberAnns,
                new ParallelOptions {CancellationToken = token, MaxDegreeOfParallelism = 4},
                i =>
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var strategy = new StrategyI(new StrategySettings());

                    var trainingTestingData = PrepareAnnData(strategy);

                    var net = Train(trainingTestingData);
                    var prediction = Predict(trainingTestingData, net);

                    var profitLossCalculator = new ProfitLossCalculator(Portfolio.Reset(), this, prediction.Item1);

                    _numberPredictionsComplete++;

                    lock (Locker)
                    {
                        // compare prediction with random walk and only add network if better
                        if (profitLossCalculator.PL >= randomPL)
                        {
                            AddBestNeuralNet(profitLossCalculator, strategy, net, prediction.Item2, prediction.Item3);
                        }

                        var currentPercentage = (_numberPredictionsComplete + 1) / (double) NumberAnns;
                        Progress = currentPercentage * 100d;

                        var currentTimeTaken = DateTime.Now - _startTime;

                        TimeLeft = currentTimeTaken -
                                   TimeSpan.FromSeconds(currentTimeTaken.TotalSeconds / currentPercentage);

                        RaisePropertyChanged(() => BestProfitLossCalculator);
                        RaisePropertyChanged(() => AllNetworksPLs);
                        RaisePropertyChanged(() => AllNetworksPLsStdDevs);
                        RaisePropertyChanged(() => AllNetworksPL);
                        RaisePropertyChanged(() => AllNetworksStdDev);
                        RaisePropertyChanged(() => AllNetworksMin);
                        RaisePropertyChanged(() => AllNetworksSigma);
                        RaisePropertyChanged(() => BestPrediction);
                    }
                });

            BuyLevel = BestPrediction.BuyLevel;
            SellLevel = BestPrediction.SellLevel;

            Messenger.Default.Send(new TrainStatusMessage("Done"));
        }

        public double GetRandomPL(IEnumerable<DateTime> dates, CancellationToken token)
        {
            var pls = new ConcurrentBag<double>();

            Parallel.For(
                0,
                3000,
                new ParallelOptions {CancellationToken = token, MaxDegreeOfParallelism = 5},
                i =>
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var randomSignals = dates.ToDictionary(x => x, x => (SignalEnum) RandomExtensions.BetterRandomInteger(1, 3));
                    var randomProfitLostCalculator = new ProfitLossCalculator(Portfolio.Reset(), this, randomSignals);

                    pls.Add(randomProfitLostCalculator.PL);
                });


            return pls.Average();
        }

        public void SplitTrainTestData()
        {
            if (Stock?.HistoricalData == null)
            {
                throw new InvalidOperationException();
            }

            var splitData = SplitHistoricalData(Stock?.HistoricalData, TrainSamplePercentage);

            TrainingHistoricalData = splitData.Item1;
            TestingHistoricalData = splitData.Item2;
        }

        public BestNetworkDTO GetBestNetworkDTO()
        {
            var tmpFileName = Path.GetTempFileName();
            BestPrediction.Net.Save(tmpFileName);
            var neuralNetBytes = File.ReadAllBytes(tmpFileName);

            return new BestNetworkDTO
            {
                Id = Stock.GetUniqueId(),
                StockId = Stock.Id,
                StrategyId = BestPrediction.Strategy.Id,
                Timestamp = DateTime.Now,
                BuyLevel = BestPrediction.BuyLevel,
                SellLevel = BestPrediction.SellLevel,
                TrainingMeansInput = BestPrediction.Strategy.TrainingMeansInput?.ToArray(),
                TrainingStdDevsInput = BestPrediction.Strategy.TrainingStdDevsInput?.ToArray(),
                TrainingMeansOutput = BestPrediction.Strategy.TrainingMeansOutput?.ToArray(),
                TrainingStdDevsOutput = BestPrediction.Strategy.TrainingStdDevsOutput?.ToArray(),
                BestNeuralNet = neuralNetBytes,
                StrategySettings = BestPrediction.Strategy.Settings.GetJson()
            };
        }

        private static Tuple<HistoricalData, HistoricalData> SplitHistoricalData(HistoricalData data,
            double trainSamplePercentage)
        {
            if (data == null)
            {
                throw new InvalidOperationException();
            }

            var splitData = data / trainSamplePercentage;

            return Tuple.Create(splitData.Item1, splitData.Item2);
        }

        private Tuple<Dictionary<DateTime, SignalEnum>, double, double> Predict(
            Tuple<List<AnnDataPoint>, List<AnnDataPoint>> trainingTestingData, NeuralNet net, bool resetLevels = true)
        {
            double buyLevel = BuyLevel;
            double sellLevel = SellLevel;

            if (resetLevels)
            {
                buyLevel = RandomExtensions.BetterRandomDouble(0.80, 0.96);
                sellLevel = RandomExtensions.BetterRandomDouble(-0.85, -0.60);
            }

            var result = new Dictionary<DateTime, SignalEnum>();
            var testingData = trainingTestingData.Item2;

            foreach (var annDataPoint in testingData)
            {
                var predictedOutput = net.Run(annDataPoint.Inputs.ToArray())[0];

                var signal = SignalEnum.Neutral;

                if (predictedOutput >= buyLevel)
                {
                    signal = SignalEnum.Buy;
                }
                else if (predictedOutput <= sellLevel)
                {
                    signal = SignalEnum.Sell;
                }

                result.Add(annDataPoint.Date, signal);
            }

            return Tuple.Create(result, buyLevel, sellLevel);
        }

        private void AddBestNeuralNet(ProfitLossCalculator profitLossCalculator, StrategyI strategy, NeuralNet net,
            double buyLevel, double sellLevel)
        {
            _cachedPredictions.Add(new Prediction(profitLossCalculator, strategy, net, buyLevel, sellLevel));
        }

        private Tuple<List<AnnDataPoint>, List<AnnDataPoint>> PrepareAnnData(StrategyI strategy,
            bool recalculateMeans = true)
        {
            if (TrainingHistoricalData == null || TestingHistoricalData == null)
            {
                throw new InvalidOperationException();
            }

            var trainingData = strategy.GetAnnData(TrainingHistoricalData, recalculateMeans);
            var testingData = strategy.GetAnnData(TestingHistoricalData, false);

            return Tuple.Create(trainingData, testingData);
        }

        private NeuralNet Train(Tuple<List<AnnDataPoint>, List<AnnDataPoint>> trainingTestingData)
        {
            var training = trainingTestingData.Item1;

            var trainData = new TrainingData();
            trainData.SetTrainData(
                training.Select(x => x.Inputs.ToArray()).ToArray(),
                training.Select(x => x.Outputs.ToArray()).ToArray());

            var layers = new List<uint> {(uint) training.First().Inputs.Length};
            Enumerable.Range(0, NumberHiddenLayers)
                .ToList()
                .ForEach(x => layers.Add((uint) NumberNeuronsPerHiddenLayer));
            layers.Add((uint) training.First().Outputs.Length);

            trainData.ShuffleTrainData();

            var net = new NeuralNet(NetworkType.LAYER, layers);

            net.ActivationFunctionHidden =
                possibleActivationFunctions[
                    RandomExtensions.BetterRandomInteger(0, possibleActivationFunctions.Length - 1)];
            net.ActivationFunctionOutput = ActivationFunction.LINEAR;

            net.TrainErrorFunction = ErrorFunction.ERRORFUNC_TANH;
            net.TrainingAlgorithm =
                possibleTrainingAlgorithms[
                    RandomExtensions.BetterRandomInteger(0, possibleTrainingAlgorithms.Length - 1)];
            //net.RpropIncreaseFactor = 1.05f;
            //net.RpropDecreaseFactor = 0.95f;
            //net.RpropDeltaMax = 500f;
            //net.RpropDeltaZero = 0.01f;

            net.TrainOnData(trainData, (uint) RandomExtensions.BetterRandomInteger(700, 900), 0, 0.000001f);

            trainData.Dispose();

            return net;
        }
    }
}