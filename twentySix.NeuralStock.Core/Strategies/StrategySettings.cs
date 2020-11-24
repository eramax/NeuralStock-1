namespace twentySix.NeuralStock.Core.Strategies
{
    using Newtonsoft.Json;

    using twentySix.NeuralStock.Core.Extensions;

    public class StrategySettings
    {
        public StrategySettings()
        {
            this.FwdDays = RandomExtensions.BetterRandomInteger(11, 19);

            this.PercentageChangeHigh = RandomExtensions.BetterRandomDouble(0.6, 1.6);
            this.PercentageChangeLow = RandomExtensions.BetterRandomDouble(-2.0, -0.8);

            this.MovingAverageCloseFast = RandomExtensions.BetterRandomInteger(12, 21);
            this.MovingAverageCloseSlow = RandomExtensions.BetterRandomInteger(33, 64);
            this.MovingAverageHighFast = RandomExtensions.BetterRandomInteger(7, 13);

            this.CCI = RandomExtensions.BetterRandomInteger(7, 19);
            this.RSI = RandomExtensions.BetterRandomInteger(4, 13);
            this.RSI2 = RandomExtensions.BetterRandomInteger(9, 15);

            this.MacdFast = RandomExtensions.BetterRandomInteger(3, 8);
            this.MacdSlow = RandomExtensions.BetterRandomInteger(26, 42);
            this.MacdSignal = RandomExtensions.BetterRandomInteger(24, 32);

            this.Hv1 = RandomExtensions.BetterRandomInteger(15, 50);

            this.FitClose = RandomExtensions.BetterRandomInteger(9, 21);
            this.FitOfFit = RandomExtensions.BetterRandomInteger(7, 17);

            this.RSI1Fit = RandomExtensions.BetterRandomInteger(7, 14);
            this.RSI2Fit = RandomExtensions.BetterRandomInteger(9, 15);
        }

        public int FwdDays { get; set; }

        public double PercentageChangeHigh { get; set; }

        public double PercentageChangeLow { get; set; }

        public int MovingAverageCloseFast { get; set; }

        public int MovingAverageCloseSlow { get; set; }

        public int MovingAverageHighFast { get; set; }

        public int CCI { get; set; }

        public int RSI { get; set; }

        public int RSI2 { get; set; }

        public int FitClose { get; set; }

        public int FitOfFit { get; set; }

        public int MacdFast { get; set; }

        public int MacdSlow { get; set; }

        public int MacdSignal { get; set; }

        public int Hv1 { get; set; }

        public int RSI1Fit { get; set; }

        public int RSI2Fit { get; set; }

        public static StrategySettings FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<StrategySettings>(json);
            }
            catch
            {
                return new StrategySettings();
            }
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}