namespace MauiApp1TestMicro.Services
{
    public interface ISpeechRecognitionService
    {
        Task<bool> RequestPermissionAsync();

        Task<string> StartListeningAsync();

        Task StopListeningAsync();

        event EventHandler<string> OnSpeechRecognized;
        event EventHandler<string> OnRecognitionError;
    }
}
