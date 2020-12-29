namespace twentySix.NeuralStock.CoreTests.Services
{
    using System;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using twentySix.NeuralStock.Core.Data.Countries;
    using twentySix.NeuralStock.Core.Data.Sources;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [TestFixture]
    public class DownloadServiceTests
    {
        private IDownloaderService _downloadService;
        private Stock _stock;

        [SetUp]
        public void SetUp()
        {
            _downloadService = new DownloaderService(
               null,
                new YahooFinanceDataSource(),
               new MorningStarDataSource());

            _stock = new Stock
            {
                Country = new Singapore(),
                Symbol = "C31"
            };
        }

        [TearDown]
        public void TearDown()
        {
            _downloadService = null;
            _stock = null;
        }

        [Test]
        public async Task GetName_ValidSymbol_ReturnsName()
        {
            var name = await _downloadService.GetName(_stock);

            Assert.IsFalse(string.IsNullOrEmpty(name));
            StringAssert.StartsWith("Cap", name);
        }

        [Test]
        public async Task GetName_InvalidSymbol_ReturnsNull()
        {
            //_stock.Symbol = "000";

            var name = await _downloadService.GetName(_stock);

            Assert.IsFalse(string.IsNullOrEmpty(name));
            Assert.AreEqual("000", name);
        }

        [Test]
        public async Task GetHistoricalData_ValidSymbol_ReturnsData()
        {
            var data = await _downloadService.GetHistoricalData(_stock, DateTime.Now.AddDays(-10), refresh: true);

            Assert.NotNull(data);
            //Assert.IsTrue(data.Quotes.Any());
            //Assert.AreEqual(data.Quotes.First().Value.Date, data.BeginDate);
            //Assert.AreEqual(data.Quotes.Last().Value.Date, data.EndDate);
        }
    }
}