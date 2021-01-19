namespace twentySix.NeuralStock.Dashboard
{
    using System;

    using DevExpress.Mvvm;

    using twentySix.NeuralStock.Core.Models;

    public class DashboardPrediction : BindableBase
    {
        public int StockId { get; set; }

        public bool Favourite
        {
            get => GetProperty(() => Favourite);
            set => SetProperty(() => Favourite, value);
        }

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
        
        public string Country
        {
            get => GetProperty(() => Country);
            set => SetProperty(() => Country, value);
        }

        public DateTime LastUpdate
        {
            get => GetProperty(() => LastUpdate);
            set => SetProperty(() => LastUpdate, value);
        }

        public double Close
        {
            get => GetProperty(() => Close);
            set => SetProperty(() => Close, value);
        }

        public DateTime LastTrainingDate
        {
            get => GetProperty(() => LastTrainingDate);
            set => SetProperty(() => LastTrainingDate, value);
        }

        public TrainingSession TrainingSession
        {
            get => GetProperty(() => TrainingSession);
            set => SetProperty(() => TrainingSession, value);
        }
    }
}