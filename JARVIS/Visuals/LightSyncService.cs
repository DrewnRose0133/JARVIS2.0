// === LightSyncService.cs ===
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JARVIS.Devices.Interfaces;


namespace JARVIS.Visuals
{
    public class LightSyncService
    {
        private readonly ILightsService _lightsService;
        private readonly string _room;
        private CancellationTokenSource _cts;

        public LightSyncService(ILightsService lightsService, string room)
        {
            _lightsService = lightsService;
            _room = room;
        }

        public void StartBeatSync(int bpm = 120)
        {
            Stop();
            _cts = new CancellationTokenSource();
            var interval = 60000 / bpm;

            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await _lightsService.PulseAsync(_room);
                    await Task.Delay(interval, _cts.Token);
                }
            }, _cts.Token);
        }

        public void Stop()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }
}
