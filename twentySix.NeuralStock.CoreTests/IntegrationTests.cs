namespace twentySix.NeuralStock.CoreTests
{
    using System;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Data.Sources;
    using twentySix.NeuralStock.Core.Helpers;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [TestFixture]
    public class IntegrationTests
    {
        private IDownloaderService _downloadService;
        private TrainingSession _trainingSession;
        private StockPortfolio _portfolio;
        private Stock _stock;
        private DateTime _startDate;

        [SetUp]
        public void SetUp()
        {
            _startDate = new DateTime(2014, 1, 1);

            _downloadService = new DownloaderService(
                null,
                new YahooFinanceDataSource(),
                new MorningStarDataSource());

            _stock = new Stock
            {
                Country = new Singapore(),
                Symbol = "Y92"
            };

            _portfolio = new StockPortfolio(_startDate, 50000);

            var callingAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var aggCatalog = new AggregateCatalog();
            aggCatalog.Catalogs.Add(new DirectoryCatalog(callingAssemblyLocation ?? throw new InvalidOperationException(), "twentySix.*.dll"));
            var compositionContainer = new CompositionContainer(aggCatalog);
            ApplicationHelper.StartUp(new LoggingService(), compositionContainer);
        }

        [TearDown]
        public void TearDown()
        {
            _downloadService = null;
            _stock = null;
        }

        [Test]

        [TestCase(1, 15)]
        public async Task FullTest(int numberOfHiddenLayers, int numberOfNeurons)
        {
            _stock.Name = await _downloadService.GetName(_stock);

            _stock.HistoricalData = await _downloadService.GetHistoricalData(_stock, _startDate, refresh: true);

            _trainingSession =
                new TrainingSession(_portfolio, _stock)
                {
                    NumberAnns = 5000,
                    NumberHiddenLayers = numberOfHiddenLayers,
                    NumberNeuronsPerHiddenLayer = numberOfNeurons,
                    TrainSamplePercentage = 0.55
                };

            var timer = new Stopwatch();
            timer.Start();

            _trainingSession.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == "BestProfitLossCalculator" && timer.ElapsedMilliseconds > 7000)
                    {
                        Trace.Write($"\n{_trainingSession.AllNetworksPLsStdDevs.Count:N0}");
                        Trace.Write($" -> PL: {_trainingSession.BestProfitLossCalculator.PL:C2}");
                        Trace.Write($" ({_trainingSession.BestProfitLossCalculator.PLPercentage:P2})");
                        Trace.Write($" | median: {_trainingSession.AllNetworksPL:C2}");
                        Trace.Write($" | acc: {_trainingSession.BestProfitLossCalculator.PercentageWinningTransactions:P2}");
                        Trace.Write($" | study1: {_trainingSession.BestPrediction.BuyLevel}");
                        Trace.Write($" | study2: {_trainingSession.BestPrediction.SellLevel}");

                        timer.Restart();
                    }
                };

            _trainingSession.FindBestAnn(new CancellationToken());

            Console.Write("PL: {0:C2}", _trainingSession.BestProfitLossCalculator.PL);
            Console.Write(" ({0:P2})", _trainingSession.BestProfitLossCalculator.PLPercentage);
            Console.Write(" | median: {0:C2}", _trainingSession.AllNetworksPL);
            Console.Write(" | acc: {0:P2}", _trainingSession.BestProfitLossCalculator.PercentageWinningTransactions);

            Assert.Pass();
        }
    }
}