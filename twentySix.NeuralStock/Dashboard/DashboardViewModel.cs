namespace twentySix.NeuralStock.Dashboard
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;

    using JetBrains.Annotations;

    using twentySix.NeuralStock.Common;
    using twentySix.NeuralStock.Core.DTOs;
    using twentySix.NeuralStock.Core.Enums;
    using twentySix.NeuralStock.Core.Messages;
    using twentySix.NeuralStock.Core.Models;
    using twentySix.NeuralStock.Core.Services.Interfaces;

    [POCOViewModel]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class DashboardViewModel : ComposedViewModelBase, IDataErrorInfo
    {
        private static readonly object Locker = new object();

        private CancellationTokenSource _cancellationTokenSource;

        private NeuralStockSettings _settings;

        protected DashboardViewModel()
        {
            Messenger.Default.Register<TrainStatusMessage>(this, OnTrainMessageStatus);

            // ReSharper disable once PossibleNullReferenceException
            Application.Current.MainWindow.Closing += (sender, args) => SaveFavourites();

            Task.Run(LoadData);
        }

        public virtual bool IsBusy { get; set; }

        [UsedImplicitly]
        public virtual StockPortfolio Portfolio { get; set; }

        public virtual ObservableCollection<DashboardPrediction> Predictions { get; set; }

        public virtual string Status { get; set; }

        public virtual SeverityEnum StatusSeverity { get; set; }

        public string Error => null;

        protected virtual INavigationService NavigationService => null;

        [Import]
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        protected IPersistenceService PersistenceService { get; set; }

        [Import]
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        protected IDownloaderService DownloaderService { get; set; }

        // ReSharper disable once StyleCop.SA1126
        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        [UsedImplicitly]
        public void NavigateTo(string view)
        {
            NavigationService.Navigate(view, null, true, this, true);
        }

        [UsedImplicitly]
        public void NavigateToTrainView(DashboardPrediction prediction)
        {
            NavigationService.Navigate("TrainView", null, prediction, this, true);
        }

        [UsedImplicitly]
        public async void Refresh()
        {
            await LoadData().ConfigureAwait(false);
            CommandManager.InvalidateRequerySuggested();
        }

        [UsedImplicitly]
        public bool CanRefresh()
        {
            return !IsBusy;
        }

        [UsedImplicitly]
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        [UsedImplicitly]
        public bool CanCancel()
        {
            return IsBusy && _cancellationTokenSource != null && _cancellationTokenSource.Token.CanBeCanceled;
        }

        [UsedImplicitly]
        public async void Delete(DashboardPrediction prediction)
        {
            if (await PersistenceService.DeleteStockWithId(prediction.StockId))
            {
                Messenger.Default.Send(new TrainStatusMessage($"Deleted {prediction.Name}"));
                Refresh();
            }
            else
            {
                Messenger.Default.Send(new TrainStatusMessage($"Could not delete {prediction.Name}"));
            }
        }

        [UsedImplicitly]
        public bool CanDelete(DashboardPrediction prediction)
        {
            return prediction != null;
        }

        protected override async void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();

            // save favourites
            await Task.Run(SaveFavourites);
        }

        private void SaveFavourites()
        {
            PersistenceService.DeleteFavourites().Wait();
            var favourites = Predictions.Where(x => x.Favourite)
                .Select(x => new FavouriteDTO { StockId = x.TrainingSession.Stock.GetUniqueId() }).ToList();

            if (favourites.Any())
            {
                PersistenceService.SaveFavourites(favourites).Wait();
            }
        }

        private async Task LoadData()
        {
            try
            {
                if (_cancellationTokenSource != null && _cancellationTokenSource.Token.CanBeCanceled && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();

                IsBusy = true;

                await LoadSettings().ConfigureAwait(false);
                SetPortfolio();

                // get list of all saved predictions
                var listBestPredictionsDtos = await PersistenceService.GetBestNetworkDTOs().ConfigureAwait(false);

                Predictions = new ObservableCollection<DashboardPrediction>();

                // for each
                foreach (var dto in listBestPredictionsDtos)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    var stock = await PersistenceService.GetStockFromId(dto.StockId);
                    stock.HistoricalData = await DownloaderService.GetHistoricalData(stock, _settings.StartDate, DateTime.Now, true).ConfigureAwait(false);

                    var dashboardPrediction = new DashboardPrediction { Symbol = stock.Symbol, Name = stock.Name, StockId = stock.GetUniqueId() };

                    lock (Locker)
                    {
                        Application.Current.Dispatcher?.Invoke(() => Predictions.Add(dashboardPrediction));
                    }

                    TrainingSession trainingSession;
                    try
                    {
                        trainingSession = await Task.Run(() => new TrainingSession(Portfolio, stock, dto, _settings)).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Messenger.Default.Send(new TrainStatusMessage($"Could setup training session for stock {stock.Name}: {ex.Message}", SeverityEnum.Error));
                        trainingSession = new TrainingSession(Portfolio, stock);
                    }

                    dashboardPrediction.TrainingSession = trainingSession;
                    dashboardPrediction.Close = trainingSession.Stock.HistoricalData?.Quotes?.LastOrDefault().Value?.Close ?? 0d;
                    dashboardPrediction.LastTrainingDate = dto.Timestamp;

                    if (trainingSession.Stock.HistoricalData != null)
                    {
                        dashboardPrediction.LastUpdate = trainingSession.Stock.HistoricalData.EndDate;
                    }
                }
            }
            finally
            {
                IsBusy = false;
                _cancellationTokenSource = null;
            }
        }

        private async Task LoadSettings()
        {
            _settings = await PersistenceService.GetSettings().ConfigureAwait(false) ?? new NeuralStockSettings();
        }

        private void SetPortfolio()
        {
            Portfolio = new StockPortfolio(_settings.StartDate, _settings.InitialCash);
        }

        private void OnTrainMessageStatus(TrainStatusMessage obj)
        {
            Status = obj.Message;
            StatusSeverity = obj.Severity;
        }
    }
}