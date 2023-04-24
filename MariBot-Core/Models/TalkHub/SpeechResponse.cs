namespace MariBot.Core.Models.TalkHub
{
    public class SpeechResponse
    {
        public Guid RequestId { get; set; }

        public RequestStatus Status { get; set; }

        public ErrorDetail? ErrorDetail { get; set; }

        public string Provider { get; set; }

        public string ProviderRequestId { get; set; }

        public string VoiceName { get; set; }

        public string TextToSpeak { get; set; }
    }
}
