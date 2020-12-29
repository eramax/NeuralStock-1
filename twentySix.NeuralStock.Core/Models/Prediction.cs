namespace twentySix.NeuralStock.Core.Models
{
    using FANNCSharp.Double;

    using twentySix.NeuralStock.Core.Strategies;

    public class Prediction
    {
        public Prediction(ProfitLossCalculator profitLossCalculator, StrategyI strategy, NeuralNet net, double buyLevel, double sellLevel)
        {
            ProfitLossCalculator = profitLossCalculator;
            Strategy = strategy;
            Net = net;
            BuyLevel = buyLevel;
            SellLevel = sellLevel;
        }

        public ProfitLossCalculator ProfitLossCalculator { get; }

        public StrategyI Strategy { get; }

        public NeuralNet Net { get; }

        public double BuyLevel { get; }

        public double SellLevel { get; }
    }
}