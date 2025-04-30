using System.Threading.Tasks;

namespace JARVIS.Modules.Devices.Interfaces
{
    public interface ICameraService
    {
        Task<string> GetLiveStreamUrlAsync(string cameraId);
        Task<string> TakeSnapshotAsync(string cameraId);
    }
}
