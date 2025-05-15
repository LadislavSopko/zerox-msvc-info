namespace Zerox.Info.Logging
{
    public interface IExtensionLogger
    {
        void LogInfo(string message);
        void LogError(string message, Exception? ex = null);
        void LogWarning(string message);
        void LogDebug(string message);
    }
}