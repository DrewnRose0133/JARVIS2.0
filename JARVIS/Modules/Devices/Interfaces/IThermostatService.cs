using System.Threading.Tasks;

namespace JARVIS.Modules.Devices.Interfaces
{
    public interface IThermostatService
    {
        Task SetTemperatureAsync(string zoneId, double temp);
        Task<double?> GetCurrentTemperatureAsync(string zoneId);
    }
}
