using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using JARVIS.Modules.Devices.Interfaces;
using KoenZomers.Ring.Api;
using KoenZomers.Ring.Api.Exceptions;
using Microsoft.Extensions.Logging;

namespace JARVIS.Modules.Devices
{
    /// <summary>
    /// Ring-based implementation of ICameraService using KoenZomers.Ring.Api and Video on Demand for pseudo-live streaming.
    /// </summary>
    public class RingCameraService : ICameraService
    {
        private readonly Session _session;
        private readonly ILogger<RingCameraService> _logger;
        private readonly IHttpClientFactory _httpFactory;

        public RingCameraService(
            Session session,
            ILogger<RingCameraService> logger,
            IHttpClientFactory httpFactory)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));

            // initial authentication
            try
            {
                _session.Authenticate();
                _logger.LogInformation("Ring session authenticated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed initial Ring authentication");
                throw new InvalidOperationException("Ring session is not authenticated. Please check credentials.", ex);
            }
        }

        public async Task<string> GetLiveStreamUrlAsync(string cameraId)
        {
            try
            {
                // use named client configured in Program.cs
                var client = _httpFactory.CreateClient("RingClient");

                // attach bearer token for OAuth
                var token = _session.OAuthToken?.AccessToken;
                if (string.IsNullOrEmpty(token))
                    throw new InvalidOperationException("Ring session is not authenticated. Please check credentials.");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // 1) trigger VOD clip
                var vodEndpoint = $"doorbots/{cameraId}/vod";
                var vodResp = await client.PostAsync(vodEndpoint, null);
                vodResp.EnsureSuccessStatusCode();
                _logger.LogInformation("Requested VOD for {CameraId}", cameraId);

                // 2) poll history
                VoodHistoryEvent latest = null;
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(1000);
                    var histJson = await client.GetStringAsync("doorbots/history?limit=1");
                    var history = JsonSerializer.Deserialize<DoorbotHistoryResponse>(histJson);
                    if (history.Events?.FirstOrDefault()?.Kind == "on_demand")
                    {
                        latest = history.Events.First();
                        break;
                    }
                }
                if (latest == null)
                    throw new InvalidOperationException("Timed out waiting for VOD event.");

                // 3) get download URL
                var dlJson = await client.GetStringAsync($"dings/{latest.Id}/share/download?disable_redirect=true");
                var dlObj = JsonSerializer.Deserialize<DownloadResponse>(dlJson);
                return dlObj.Url;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                _logger.LogError(ex, "Unauthorized VOD request for {CameraId}", cameraId);
                throw new InvalidOperationException("Ring VOD request unauthorized. Check your Ring session token.", ex);
            }
            catch (SessionNotAuthenticatedException ex)
            {
                _logger.LogError(ex, "Ring session not authenticated in GetLiveStreamUrlAsync");
                throw new InvalidOperationException("Ring session is not authenticated. Please check credentials.", ex);
            }
        }

        public async Task<string> TakeSnapshotAsync(string cameraId)
        {
            try
            {
                var devices = await _session.GetRingDevices();
                var doorbot = devices.Doorbots.FirstOrDefault(d => d.Id.ToString() == cameraId);
                if (doorbot == null)
                {
                    _logger.LogWarning("Camera {CameraId} not found.", cameraId);
                    return null;
                }

                var path = Path.Combine(Path.GetTempPath(), $"snapshot_{cameraId}.jpg");
                await _session.GetLatestSnapshot(doorbot.Id, path);
                _logger.LogInformation("Snapshot saved to {Path}", path);
                return path;
            }
            catch (SessionNotAuthenticatedException ex)
            {
                _logger.LogError(ex, "Ring session not authenticated in TakeSnapshotAsync");
                throw new InvalidOperationException("Ring session is not authenticated. Please check credentials.", ex);
            }
        }

        private class DoorbotHistoryResponse
        {
            [JsonPropertyName("items")]
            public List<VoodHistoryEvent> Events { get; set; }
        }

        private class VoodHistoryEvent
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }
            [JsonPropertyName("kind")]
            public string Kind { get; set; }
        }

        private class DownloadResponse
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public class NoOpCameraService : ICameraService
        {
            public Task<string> TakeSnapshotAsync(string cameraId) =>
                Task.FromResult<string>(null);
            public Task<string> GetLiveStreamUrlAsync(string cameraId) =>
                Task.FromResult<string>(null);
        }


    }
}
