namespace twentySix.NeuralStock.Core.Strategies
{
    using Newtonsoft.Json;

    using Extensions;

    public class StrategySettings
    {
        public StrategySettings()
        {
            FwdDays = RandomExtensions.BetterRandomInteger(7, 29);

            PercentageChangeHigh = RandomExtensions.BetterRandomDouble(0.6, 1.6);
            PercentageChangeLow = RandomExtensions.BetterRandomDouble(-2.0, -0.8);

            MovingAverageCloseFast = RandomExtensions.BetterRandomInteger(10, 21);
            MovingAverageCloseSlow = RandomExtensions.BetterRandomInteger(33, 64);
            MovingAverageHighFast = RandomExtensions.BetterRandomInteger(3, 21);

            CCI = RandomExtensions.BetterRandomInteger(6, 21);
            RSI = RandomExtensions.BetterRandomInteger(3, 21);
            RSI2 = RandomExtensions.BetterRandomInteger(3, 21);

            MacdFast = RandomExtensions.BetterRandomInteger(3, 8);
            MacdSlow = RandomExtensions.BetterRandomInteger(26, 42);
            MacdSignal = RandomExtensions.BetterRandomInteger(24, 36);

            Hv1 = RandomExtensions.BetterRandomInteger(5, 50);

            FitClose = RandomExtensions.BetterRandomInteger(9, 21);
            FitOfFit = RandomExtensions.BetterRandomInteger(7, 17);

            RSI1Fit = RandomExtensions.BetterRandomInteger(7, 14);
            RSI2Fit = RandomExtensions.BetterRandomInteger(9, 15);
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