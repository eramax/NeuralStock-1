namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Linq;

    using Data.Countries;

    using DevExpress.Mvvm;

    using DTOs;

    using Helpers;

    [Export]
    [DebuggerDisplay("Symbol = {" + nameof(Symbol) + "}")]  
    public class Stock : BindableBase, IDataErrorInfo, IDisposable
    {
        public Stock()
        {
            Symbol = string.Empty;
            Name = string.Empty;
            HistoricalData = null;
            Country = new Singapore();
        }

        ~Stock()
        {
            Dispose(false);
        }

        [Key]
        public int Id { get; set; }

        public string Symbol
        {
            get => GetProperty(() => Symbol);
            set => SetProperty(() => Symbol, value);
        }

        public string Name
        {
            get => GetProperty(() => Name);
            set => SetProperty(() => Name, value);
        }

        public ICountry Country
        {
            get => GetProperty(() => Country);
            set => SetProperty(() => Country, value);
        }

        public HistoricalData HistoricalData
        {
            get => GetProperty(() => HistoricalData);
            set => SetProperty(() => HistoricalData, value);
        }

        public string Error => null;

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static Stock FromDTO(StockDTO dto, HistoricalDataDTO historicalDataDTO)
        {
            if (dto == null)
            {
                return null;
            }

            // available countries
            var availableCountries = ApplicationHelper.CurrentCompositionContainer.GetExportedValues<ICountry>();

            return new Stock
            {
                Id = dto.Id,
                Symbol = dto.Symbol,
                Name = dto.Name,
                Country = availableCountries.SingleOrDefault(x => x.Id == dto.CountryId),
                HistoricalData = HistoricalData.FromDTO(historicalDataDTO)
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public StockDTO GetDTO()
        {
            return new StockDTO
            {
                Id = Id,
                Symbol = Symbol,
                Name = Name,
                CountryId = Country.Id,
                HistoricalDataId = HistoricalData.Id
            };
        }

        public int GetUniqueId()
        {
            return (Symbol + Country?.Name).GetHashCode();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            HistoricalData = null;
        }
    }
}