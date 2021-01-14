namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using twentySix.NeuralStock.Core.Common;

    public class StockPortfolio
    {
        public StockPortfolio()
        {
        }

        public StockPortfolio(DateTime startDate, double initialCash)
        {
            Add(startDate, initialCash);
        }

        public SortedList<DateTime, double> CashTransactions { get; } = new SortedList<DateTime, double>(new DuplicateKeyComparer<DateTime>());

        public SortedList<DateTime, Trade> Trades { get; } = new SortedList<DateTime, Trade>(new DuplicateKeyComparer<DateTime>());

        public string Name { get; set; }

        public double GetCash(DateTime date)
        {
            return CashTransactions.Where(x => x.Key <= date).Select(x => x.Value).Sum();
        }

        public void Add(Trade trade)
        {
            if (GetCash(trade.Date) - trade.TotalValue < 0)
            {
                throw new InvalidOperationException("Not enough cash");
            }

            // adjust cash
            Add(trade.Date, -trade.TotalValue);
            
            Trades.Add(trade.Date, trade);
        }

        public void Add(DateTime date, double value)
        {
            if (GetCash(date) + value < 0)
            {
                throw new InvalidOperationException("Not enough cash");
            }

            CashTransactions.Add(date, value);
        }

        public int GetMaxPurchaseVolume(Stock stock, DateTime date, double price)
        {
            var cashAtDate = GetCash(date);

            int adjVolume = (int)(cashAtDate / price);

            for (; adjVolume >= 0; adjVolume--)
            {
                var value = new Trade
                {
                    Stock = stock,
                    Type = TransactionEnum.Buy,
                    Price = price,
                    NumberOfShares = adjVolume
                }.TotalValue;

                if (value <= cashAtDate)
                {
                    break;
                }
            }

            return adjVolume;
        }

        public Dictionary<Stock, int> GetHoldings(DateTime date)
        {
            return Trades
                .Where(x => x.Key <= date)
                .Select(x => new { stock = x.Value.Stock, volume = (x.Value.Type == TransactionEnum.Buy ? 1 : -1) * x.Value.NumberOfShares })
                .GroupBy(x => x.stock)
                .Select(x => new { stock = x.Key, total = x.Sum(_ => _.volume) })
                .Where(x => x.total != 0)
                .ToDictionary(x => x.stock, x => x.total);
        }

        public double GetValue(DateTime date)
        {
            var cashValue = GetCash(date);
            var sharesValue = -GetHoldings(date).Sum(
                                  holding => new Trade
                                  {
                                      Date = date,
                                      Stock = holding.Key,
                                      NumberOfShares = holding.Value,
                                      Price = holding.Key.HistoricalData.Quotes[date].Close,
                                      Type = TransactionEnum.Sell
                                  }.TotalValue);

            return cashValue + sharesValue;
        }

        public StockPortfolio Reset()
        {
            return new StockPortfolio(
                CashTransactions.FirstOrDefault().Key,
                CashTransactions.FirstOrDefault().Value);
        }
    }
}