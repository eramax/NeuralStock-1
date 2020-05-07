namespace twentySix.NeuralStock.Core.Data.Countries
{
    using System.ComponentModel.Composition;

    [Export(typeof(ICountry))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Others : ICountry
    {
        public static int CountryId => 999;

        public int Id => CountryId;

        public string Name => "Others";

        public double GetFees(double contractValue)
        {
            return 0d;
        }
    }
}