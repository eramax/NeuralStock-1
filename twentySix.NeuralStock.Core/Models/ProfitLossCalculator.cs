namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Helpers;
    using Services.Interfaces;

    public class ProfitLossCalculator
    {
        private readonly IStatisticsService _statisticsService;

        public ProfitLossCalculator(StockPortfolio portfolio, TrainingSession trainingSession,
            Dictionary<DateTime, SignalEnum> signals)
        {
            _statisticsService = ApplicationHelper.CurrentCompositionContainer.GetExportedValue<IStatisticsService>();

            Portfolio = portfolio;
            TrainingSession = trainingSession;
            Signals = signals;

            Calculate();
        }

        public StockPortfolio Portfolio { get; }

        public TrainingSession TrainingSession { get; }

        public Dictionary<DateTime, SignalEnum> Signals { get; }

        public Dictionary<DateTime, Tuple<SignalEnum, double>> SignalsExtended => Signals
            .Zip(TrainingSession.TestingHistoricalData.Quotes.Values,
                (s, q) => new KeyValuePair<Quote, SignalEnum>(q, s.Value))
            .ToDictionary(x => x.Key.Date, x => Tuple.Create(x.Value, x.Key.Close));

        public Dictionary<DateTime, double> SellSignals => Signals
            .Zip(
                TrainingSession.TestingHistoricalData.Quotes.Values,
                (s, q) => new KeyValuePair<Quote, SignalEnum>(q, s.Value)).Where(x => x.Value == SignalEnum.Sell)
            .ToDictionary(x => x.Key.Date, x => x.Key.High);

        public Dictionary<DateTime, double> BuySignals => Signals
            .Zip(
                TrainingSession.TestingHistoricalData.Quotes.Values,
                (s, q) => new KeyValuePair<Quote, SignalEnum>(q, s.Value)).Where(x => x.Value == SignalEnum.Buy)
            .ToDictionary(x => x.Key.Date, x => x.Key.Low);

        public SignalEnum S1 => Signals.LastOrDefault().Value;

        public SignalEnum S2 => Signals.ElementAtOrDefault(Signals.Count - 2).Value;

        public SignalEnum S3 => Signals.ElementAtOrDefault(Signals.Count - 3).Value;
        
        public SignalEnum S4 => Signals.ElementAtOrDefault(Signals.Count - 4).Value;
        
        public SignalEnum S5 => Signals.ElementAtOrDefault(Signals.Count - 5).Value;

        public Dictionary<DateTime, double> PortfolioTotalValue =>
            TrainingSession.TestingHistoricalData.Quotes.Values.ToDictionary(
                x => x.Date,
                x => Portfolio.GetValue(x.Date));

        public Dictionary<DateTime, double> PortfolioSellSignals => CompleteTransactions
            .ToDictionary(x => x.SellTrade.Date, x => Portfolio.GetValue(x.SellTrade.Date));

        public Dictionary<DateTime, double> PortfolioBuySignals => CompleteTransactions
            .ToDictionary(x => x.BuyTrade.Date, x => Portfolio.GetValue(x.BuyTrade.Date));

        public List<CompleteTransaction> CompleteTransactions { get; } = new List<CompleteTransaction>();

        public double PL => Portfolio.GetValue(TrainingSession.TestingHistoricalData.EndDate) -
                            Portfolio.GetValue(TrainingSession.TestingHistoricalData.BeginDate);

        public double PLPercentage => PL / Portfolio.GetValue(TrainingSession.TestingHistoricalData.BeginDate);

        public double ProfitMonth => PL
                                          * (30.417d / (TrainingSession.TestingHistoricalData.EndDate
                                                     - TrainingSession.TestingHistoricalData.BeginDate).TotalDays);

        public double PLYear => PL
                                * (365d / (TrainingSession.TestingHistoricalData.EndDate
                                           - TrainingSession.TestingHistoricalData.BeginDate).TotalDays);

        public double BuyHold => (TrainingSession.TestingHistoricalData.Quotes.LastOrDefault().Value.Close
                                  - TrainingSession.TestingHistoricalData.Quotes.FirstOrDefault().Value.Close) /
                                 TrainingSession.TestingHistoricalData.Quotes.FirstOrDefault().Value.Close;

        public double BuyHoldDifference => BuyHold != 0d ? PLPercentage / BuyHold : 0d;

        public int NumberBuySignals => Signals.Count(x => x.Value == SignalEnum.Buy);

        public int NumberSellSignals => Signals.Count(x => x.Value == SignalEnum.Sell);

        public int NumberOfTrades => Portfolio.Trades.Count / 2;

        public int NumberOfCompleteTransactions => CompleteTransactions.Count;

        public double MaxPL => CompleteTransactions.Any() ? CompleteTransactions.Max(x => x.PL) : 0;

        public double MinPL => CompleteTransactions.Any() ? CompleteTransactions.Min(x => x.PL) : 0;

        public int NumberWinningTransactions => CompleteTransactions.Count(x => x.PL > 0);

        public double PercentageWinningTransactions => NumberWinningTransactions / (double) CompleteTransactions.Count;

        public int NumberLossingTransactions => CompleteTransactions.Count(x => x.PL < 0);

        public double MeanPL => _statisticsService.Mean(CompleteTransactions.Select(x => x.PL).ToArray());

        public double StandardDeviationPL =>
            _statisticsService.StandardDeviation(CompleteTransactions.Select(x => x.PL).ToArray());

        public double MedianPL => _statisticsService.Median(CompleteTransactions.Select(x => x.PL).ToArray());

        public double MedianWinningPL =>
            _statisticsService.Median(CompleteTransactions.Where(x => x.PL > 0).Select(x => x.PL).ToArray());

        public double MedianLossingPL =>
            _statisticsService.Median(CompleteTransactions.Where(x => x.PL < 0).Select(x => x.PL).ToArray());

        public Dictionary<string, int> CompleteTransactionsPLs =>
            _statisticsService.Bucketize(CompleteTransactions.Select(x => x.PL).ToArray(), 8);

        private void Calculate()
        {
            if (TrainingSession.TestingHistoricalData.Quotes.Count != Signals.Count)
            {
                return;
            }

            Trade lastSellTrade = null;

            foreach (var quote in TrainingSession.TestingHistoricalData.Quotes)
            {
                var indexOfToday = TrainingSession.TestingHistoricalData.Quotes.IndexOfKey(quote.Key);
                var indexTomorrow = indexOfToday < TrainingSession.TestingHistoricalData.Quotes.Count - 2
                    ? indexOfToday + 1
                    : indexOfToday;
                var transactionBuyPrice =
                    TrainingSession.TestingHistoricalData.Quotes.ElementAt(indexOfToday).Value.Close;
                var transactionSellPrice =
                    TrainingSession.TestingHistoricalData.Quotes.ElementAt(indexOfToday).Value.Close;

                if (lastSellTrade == null || (quote.Key.Date - lastSellTrade.Date.Date).Days >=
                    TrainingSession.NumberDaysBetweenTransactions)
                {
                    if (Signals[quote.Key] == SignalEnum.Buy &&
                        Portfolio.GetMaxPurchaseVolume(TrainingSession.Stock, quote.Key,
                            transactionBuyPrice) > 1)
                    {
                        int maxPurchaseVolume =
                            Portfolio.GetMaxPurchaseVolume(TrainingSession.Stock, quote.Key, transactionBuyPrice);

                        var trade = new Trade
                        {
                            Type = TransactionEnum.Buy,
                            Stock = TrainingSession.Stock,
                            Date = quote.Key,
                            NumberOfShares = maxPurchaseVolume,
                            Price = transactionBuyPrice
                        };

                        Portfolio.Add(trade);
                    }
                }

                if (Signals[quote.Key] == SignalEnum.Sell &&
                    Portfolio.GetHoldings(quote.Key).ContainsKey(TrainingSession.Stock))
                {
                    lastSellTrade = new Trade
                    {
                        Type = TransactionEnum.Sell,
                        Stock = TrainingSession.Stock,
                        Date = quote.Key,
                        NumberOfShares = Portfolio.GetHoldings(quote.Key)[TrainingSession.Stock],
                        Price = transactionSellPrice
                    };

                    CompleteTransactions.Add(new CompleteTransaction(Portfolio.Trades.Last().Value, lastSellTrade));
                    Portfolio.Add(lastSellTrade);
                }
            }

            if (Portfolio.GetHoldings(TrainingSession.TestingHistoricalData.EndDate).Any())
            {
                var trade = new Trade
                {
                    Type = TransactionEnum.Sell,
                    Stock = TrainingSession.Stock,
                    Date = TrainingSession.TestingHistoricalData.EndDate,
                    NumberOfShares =
                        Portfolio.GetHoldings(TrainingSession.TestingHistoricalData.EndDate)[TrainingSession.Stock],
                    Price = TrainingSession.TestingHistoricalData.Quotes[TrainingSession.TestingHistoricalData.EndDate]
                        .Close
                };

                CompleteTransactions.Add(new CompleteTransaction(Portfolio.Trades.Last().Value, trade));
                Portfolio.Add(trade);
            }
        }
    }
}