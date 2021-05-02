namespace twentySix.NeuralStock.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    using DTOs;

    public class NeuralStockSettings
    {
        [Key]
        public int Id { get; set; }

        public double InitialCash { get; set; }

        public DateTime StartDate { get; set; }

        public double PercentageTraining { get; set; }

        public int NumberANNs { get; set; }

        public int NumberHiddenLayers { get; set; }

        public int NumberNeuronsHiddenLayer { get; set; }

        public int NumberDaysBetweenTransactions { get; set; }

        public int Threads { get; set; }

        public static NeuralStockSettings FromDTO(NeuralStockSettingsDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new NeuralStockSettings
            {
                Id = dto.Id,
                InitialCash = dto.InitialCash,
                StartDate = dto.StartDate,
                PercentageTraining = dto.PercentageTraining,
                NumberANNs = dto.NumberANNs,
                NumberHiddenLayers = dto.NumberHiddenLayers,
                NumberNeuronsHiddenLayer = dto.NumberNeuronsHiddenLayer,
                NumberDaysBetweenTransactions = dto.NumberDaysBetweenTransactions,
                Threads = dto.Threads
            };
        }

        public static NeuralStockSettings GetDefault()
        {
            var obj = new NeuralStockSettings
            {
                InitialCash = 20000,
                StartDate = DateTime.Today.AddYears(-1),
                PercentageTraining = 0.6,
                NumberANNs = 100,
                NumberHiddenLayers = 1,
                NumberNeuronsHiddenLayer = 15,
                NumberDaysBetweenTransactions = 3,
                Threads = 4
            };

            return obj;
        }

        public NeuralStockSettingsDTO GetDTO()
        {
            return new NeuralStockSettingsDTO
            {
                Id = Id,
                InitialCash = InitialCash,
                StartDate = StartDate,
                PercentageTraining = PercentageTraining,
                NumberANNs = NumberANNs,
                NumberHiddenLayers = NumberHiddenLayers,
                NumberNeuronsHiddenLayer = NumberNeuronsHiddenLayer,
                NumberDaysBetweenTransactions = NumberDaysBetweenTransactions,
                Threads = Threads
            };
        }
    }
}