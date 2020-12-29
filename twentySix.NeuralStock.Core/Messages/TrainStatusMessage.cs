namespace twentySix.NeuralStock.Core.Messages
{
    using twentySix.NeuralStock.Core.Enums;

    public class TrainStatusMessage
    {
        public TrainStatusMessage(string message, SeverityEnum severity = SeverityEnum.Info)
        {
            Message = message;
            Severity = severity;
        }

        public string Message { get; }

        public SeverityEnum Severity { get; }
    }
}