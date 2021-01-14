namespace twentySix.NeuralStock.Train
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;

    using JetBrains.Annotations;

    using Common;
    using Core.Data.Countries;
    using Core.Enums;
    using Core.Messages;
    using Core.Models;
    using Core.Services.Interfaces;
    using Dashboard;

    [POCOViewModel]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class TrainViewModel : ComposedViewModelBase, IDataErrorInfo
    {
        private CancellationTokenSource _cancellationTokenSource;

        private NeuralStockSettings _settings;

        protected TrainViewModel()
        {
            Messenger.Default.Register<TrainStatusMessage>(this, OnTrainMessageStatus);
        }

        public virtual bool IsBusy { get; set; }

        public virtual bool IsTraining { get; set; }

        public virtual Stock Stock { get; set; }

        public virtual string StockSymbol { get; set; }

        [UsedImplicitly]
        public virtual StockPortfolio Portfolio { get; set; }

        public virtual TrainingSession TrainingSession { get; set; }

        [UsedImplicitly]
        public virtual HistoricalData TrainingData { get; set; }

        public virtual HistoricalData TestingData { get; set; }

        public virtual string Status { get; set; }

        public virtual SeverityEnum StatusSeverity { get; set; }

        [ImportMany]
        public virtual IEnumerable<ICountry> AvailableCountries { get; set; }

        public virtual ICountry SelectedCountry { get; set; }

        public virtual Quote LastQuote => Stock?.HistoricalData?.Quotes.LastOrDefault().Value;

        public string Error => null;

        protected virtual INavigationService NavigationService => null;

        [UsedImplicitly]
        [Import]
        protected IPersistenceService PersistenceService { get; set; }

        [UsedImplicitly]
        [Import]
        protected IDownloaderService DownloaderService { get; set; }

        // ReSharper disable once StyleCop.SA1126
        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        [UsedImplicitly]
        public void NavigateTo(string view)
        {
            NavigationService.Navigate(view, null, this);
        }

        [UsedImplicitly]
        public async Task DownloadData()
        {
            IsBusy = true;

            try
            {
                ClearStatus();

                Stock = new Stock
                {
                    Symbol = StockSymbol,
                    Country = SelectedCountry
                };
                Stock.Id = Stock.GetUniqueId();

                RaisePropertyChanged(() => LastQuote);

                Messenger.Default.Send(new TrainStatusMessage("Loading settings"));

                await LoadSettings().ConfigureAwait(false);

                Messenger.Default.Send(new TrainStatusMessage($"Downloading data for stock {Stock.Symbol}"));

                var name = await DownloaderService.GetName(Stock).ConfigureAwait(false);

                if (name.Equals(Stock.Symbol))
                {
                    Messenger.Default.Send(new TrainStatusMessage($"Could not get the name for the stock {Stock.Symbol}", SeverityEnum.Warning));
                }

                Stock.Name = name;
                Stock.HistoricalData = await DownloaderService.GetHistoricalData(Stock, _settings.StartDate, DateTime.Now)
                                                .ConfigureAwait(false);

                ResetTrainingSession();
                SplitHistoricalData();

                RaisePropertyChanged(() => LastQuote);
                CommandManager.InvalidateRequerySuggested();

                await PersistenceService.SaveStock(Stock).ConfigureAwait(false);

                Messenger.Default.Send(new TrainStatusMessage($"Finished downloading data for stock {Stock.Symbol}"));
            }
            catch (Exception ex)
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not download data for stock {Stock.Symbol}: {ex.Message}", SeverityEnum.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [UsedImplicitly]
        public bool CanDownloadData()
        {
            return !string.IsNullOrEmpty(StockSymbol);
        }

        [UsedImplicitly]
        public async void Train()
        {
            IsBusy = true;
            IsTraining = true;

            if (!Stock.HistoricalData.Quotes.Any())
            {
                Messenger.Default.Send(new TrainStatusMessage($"Historical data for stock {Stock.Name} not downloaded.", SeverityEnum.Error));
                return;
            }

            try
            {
                if (_cancellationTokenSource != null && _cancellationTokenSource.Token.CanBeCanceled && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                    return;
                }

                await DownloadData().ConfigureAwait(false);

                _cancellationTokenSource = new CancellationTokenSource();
                await Task.Run(() => TrainingSession.FindBestAnn(_cancellationTokenSource.Token)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not train {Stock.Name}. Exception: {ex.Message}", SeverityEnum.Error));
            }
            finally
            {
                IsTraining = false;
                IsBusy = false;
                _cancellationTokenSource = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        [UsedImplicitly]
        public bool CanTrain()
        {
            return !string.IsNullOrEmpty(StockSymbol) && (Stock?.HistoricalData?.Quotes.Any() ?? false);
        }

        [UsedImplicitly]
        public async void Save()
        {
            try
            {
                IsBusy = true;
                if (!await PersistenceService.SaveBestNetwork(TrainingSession).ConfigureAwait(false))
                {
                    throw new Exception("Error found while saving the best network");
                }

                Messenger.Default.Send(new TrainStatusMessage($"Saved best network for {Stock.Symbol}"));
            }
            catch (Exception ex)
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not save {Stock.Symbol}: {ex.Message}", SeverityEnum.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [UsedImplicitly]
        public bool CanSave()
        {
            return !IsBusy && !IsTraining && TrainingSession?.BestPrediction != null;
        }

        protected override void OnNavigatedTo()
        {
            if (Parameter is DashboardPrediction dashboardPrediction)
            {
                Stock = dashboardPrediction.TrainingSession.Stock;
                StockSymbol = Stock.Symbol;
                SelectedCountry = Stock.Country;
                TrainingSession = dashboardPrediction.TrainingSession;
                TrainingData = TrainingSession.TrainingHistoricalData;
                TestingData = TrainingSession.TestingHistoricalData;
                RaisePropertyChanged(() => LastQuote);
            }

            if (Parameter is bool clearData)
            {
                // ReSharper disable once StyleCop.SA1126
                if (clearData)
                {
                    ClearStatus();
                    StockSymbol = string.Empty;
                    Stock = null;
                    SelectedCountry = AvailableCountries.FirstOrDefault();
                    TrainingSession = null;
                    RaisePropertyChanged(() => LastQuote);
                }
            }
        }

        private void ClearStatus()
        {
            TrainingData = null;
            TestingData = null;

            Messenger.Default.Send(new TrainStatusMessage(string.Empty));
        }

        private void ResetTrainingSession()
        {
            Portfolio = new StockPortfolio(_settings.StartDate, _settings.InitialCash);

            TrainingSession = new TrainingSession(Portfolio, Stock)
            {
                TrainSamplePercentage = _settings.PercentageTraining,
                NumberAnns = _settings.NumberANNs,
                NumberHiddenLayers = _settings.NumberHiddenLayers,
                NumberNeuronsPerHiddenLayer = _settings.NumberNeuronsHiddenLayer
            };
        }

        private void SplitHistoricalData()
        {
            TrainingSession.SplitTrainTestData();
            TrainingData = TrainingSession.TrainingHistoricalData;
            TestingData = TrainingSession.TestingHistoricalData;
        }

        private async Task LoadSettings()
        {
            _settings = await PersistenceService.GetSettings().ConfigureAwait(false) ?? new NeuralStockSettings();
        }

        private void OnTrainMessageStatus(TrainStatusMessage obj)
        {
            Status = obj.Message;
            StatusSeverity = obj.Severity;
        }
    }
}