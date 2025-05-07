using System.Threading.Tasks;

namespace JARVIS.Devices.Interfaces
{
    public interface ILightsService
    {
        /// <summary>Turn a named light on or off.</summary>
        Task SetLightStateAsync(string lightId, bool on);

        /// <summary>Get the current on/off state.</summary>
        Task<bool> GetLightStateAsync(string lightId);

        Task PulseAsync(string room)
        {
            return Task.FromResult(false);
        }
    }
}