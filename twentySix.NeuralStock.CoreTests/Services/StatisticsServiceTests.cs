namespace twentySix.NeuralStock.Core.Services.Tests
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using twentySix.NeuralStock.Core.Services.Interfaces;

    [TestFixture]
    public class StatisticsServiceTests
    {
        private IStatisticsService _statisticsService;
        private double[] _testData;

        [SetUp]
        public void SetUp()
        {
            _statisticsService = new StatisticsService();
            _testData = Enumerable.Range(1, 26).Select(Convert.ToDouble).ToArray();
        }

        [TearDown]
        public void TearDown()
        {
            _statisticsService = null;
            _testData = null;
        }

        [Test]
        public void Mean_ReturnsMean()
        {
            var mean = _statisticsService.Mean(_testData);

            Assert.AreEqual(13.5d, mean);
        }

        [Test]
        public void StdDev_ReturnsStdDev()
        {
            var std = _statisticsService.StandardDeviation(_testData);

            Assert.IsTrue(std - 7.6485 < 0.001);
        }
    }
}