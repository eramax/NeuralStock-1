namespace twentySix.NeuralStock.Core.Strategies
{
    using Newtonsoft.Json;

    using Extensions;

    public class StrategySettings
    {
        public StrategySettings()
        {
            FwdDays = RandomExtensions.BetterRandomInteger(7, 11);

            PercentageChangeHigh = RandomExtensions.BetterRandomDouble(0.8, 1.6);
            PercentageChangeLow = RandomExtensions.BetterRandomDouble(-1.8, -0.8);

            MovingAverageCloseFast = RandomExtensions.BetterRandomInteger(5, 21);

            CCI = RandomExtensions.BetterRandomInteger(13, 24);
            RSI = RandomExtensions.BetterRandomInteger(13, 26);

            MacdFast = RandomExtensions.BetterRandomInteger(2, 7);
            MacdSlow = RandomExtensions.BetterRandomInteger(9, 21);
            MacdSignal = RandomExtensions.BetterRandomInteger(13, 30);

            Atr = RandomExtensions.BetterRandomInteger(3, 10);
            Ema = RandomExtensions.BetterRandomInteger(3, 10);
            Obv = RandomExtensions.BetterRandomInteger(3, 10);
            ConnorsRsi = RandomExtensions.BetterRandomInteger(5, 13);
            Pmo = RandomExtensions.BetterRandomInteger(7, 21);

            Roc = RandomExtensions.BetterRandomInteger(1, 21);
        }

        public int FwdDays { get; set; }

        public double PercentageChangeHigh { get; set; }

        public double PercentageChangeLow { get; set; }

        public int MovingAverageCloseFast { get; set; }

        public int CCI { get; set; }

        public int RSI { get; set; }

        public int MacdFast { get; set; }

        public int MacdSlow { get; set; }

        public int MacdSignal { get; set; }
        
        public int Atr { get; set; }
        
        public int Ema { get; set; }
        
        public int Obv { get; set; }
        
        public int ConnorsRsi { get; set; }
        
        public int Pmo { get; set; }

        public int Roc { get; set; }

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