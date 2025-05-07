using System.Threading.Tasks;

namespace JARVIS.Devices.Interfaces
{
    public interface ICameraService
    {
        Task<string> GetLiveStreamUrlAsync(string cameraId);
        Task<string> TakeSnapshotAsync(string cameraId);
    }
}
