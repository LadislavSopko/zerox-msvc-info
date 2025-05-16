using System.Threading.Tasks;

namespace Msvc.Info.Server
{
    /// <summary>
    /// Interface for HTTP server transport
    /// </summary>
    public interface IHttpServer
    {
        bool HasMCPService { get; }
        void Start();
        Task StopAsync();
        bool IsRunning { get; }
        string BaseUrl { get; }
    }
}
