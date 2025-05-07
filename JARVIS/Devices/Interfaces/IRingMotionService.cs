using System.Threading.Tasks;

namespace JARVIS.Devices.Interfaces
{
    /// <summary>
    /// Abstraction for Ring motion detection service.
    /// </summary>
    public interface IRingMotionService
    {
        /// <summary>
        /// Checks if motion has been detected by the specified Ring camera.
        /// </summary>
        /// <param name="cameraId">The identifier of the Ring camera.</param>
        /// <returns>True if motion was detected; otherwise, false.</returns>
        Task<bool> IsMotionDetectedAsync(string cameraId);

        /// <summary>
        /// Starts continuous monitoring of motion for the specified Ring camera.
        /// Motion events should be delivered via configured callbacks or event handlers.
        /// </summary>
        /// <param name="cameraId">The identifier of the Ring camera.</param>
        /// <returns>A task that completes when monitoring has started.</returns>
        Task StartMonitoringAsync(string cameraId);

        /// <summary>
        /// Stops motion monitoring for the specified Ring camera.
        /// </summary>
        /// <param name="cameraId">The identifier of the Ring camera.</param>
        /// <returns>A task that completes when monitoring has stopped.</returns>
        Task StopMonitoringAsync(string cameraId);
    }
}
