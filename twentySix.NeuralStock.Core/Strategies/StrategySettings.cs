namespace twentySix.NeuralStock.Core.Strategies
{
    using Newtonsoft.Json;

    using Extensions;

    public class StrategySettings
    {
        public StrategySettings()
        {
            FwdDays = RandomExtensions.BetterRandomInteger(4, 14);

            PercentageChangeHigh = RandomExtensions.BetterRandomDouble(0.5, 1.6);
            PercentageChangeLow = RandomExtensions.BetterRandomDouble(-2.5, -0.6);

            MovingAverageCloseFast = RandomExtensions.BetterRandomInteger(3, 26);
            MovingAverageCloseSlow = RandomExtensions.BetterRandomInteger(18, 40);
            MovingAverageVolume = RandomExtensions.BetterRandomInteger(10, 26);

            CCI = RandomExtensions.BetterRandomInteger(3, 21);
            RSI = RandomExtensions.BetterRandomInteger(3, 21);

            MacdFast = RandomExtensions.BetterRandomInteger(3, 8);
            MacdSlow = RandomExtensions.BetterRandomInteger(21, 42);
            MacdSignal = RandomExtensions.BetterRandomInteger(24, 36);

            Atr = RandomExtensions.BetterRandomInteger(3, 21);
            Ema = RandomExtensions.BetterRandomInteger(3, 40);
            Wr = RandomExtensions.BetterRandomInteger(3, 21);
            Kama = RandomExtensions.BetterRandomInteger(3, 21);
            Aroon = RandomExtensions.BetterRandomInteger(3, 26);
        }

        public int FwdDays { get; set; }

        public double PercentageChangeHigh { get; set; }

        public double PercentageChangeLow { get; set; }

        public int MovingAverageCloseFast { get; set; }

        public int MovingAverageCloseSlow { get; set; }
        
        public int MovingAverageVolume { get; set; }
        
        public int CCI { get; set; }

        public int RSI { get; set; }

        public int MacdFast { get; set; }

        public int MacdSlow { get; set; }

        public int MacdSignal { get; set; }
        
        public int Atr { get; set; }
        
        public int Ema { get; set; }
        
        public int Wr { get; set; }
        
        public int Kama { get; set; }
        
        public int Aroon { get; set; }
        
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