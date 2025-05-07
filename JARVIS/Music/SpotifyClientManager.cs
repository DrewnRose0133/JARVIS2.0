// === SpotifyClientManager.cs ===
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace JARVIS.Music
{
    public class SpotifyClientManager
    {
        private const string clientId = "e14908ff4b3641f6afa13a01a6611544"; // Replace with your actual Client ID
        private const string clientSecret = "b12fc24aa14846968f9c2bd281326a3c"; // Replace with your actual Client Secret
        private const string redirectUri = "http://127.0.0.1:4002/callback/";

        private static EmbedIOAuthServer _server;
        public static SpotifyClient SpotifyClientInstance { get; private set; }

        public static async Task InitializeAsync()
        {
            _server = new EmbedIOAuthServer(new Uri(redirectUri), 4002);

            await _server.Start();
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            var loginRequest = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] {
                    Scopes.UserReadPlaybackState,
                    Scopes.UserModifyPlaybackState,
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistModifyPrivate,
                    Scopes.PlaylistModifyPublic,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserReadPrivate,
                    Scopes.Streaming
                }
            };

            var uri = loginRequest.ToUri();
            Console.WriteLine("Opening Spotify login page...");
            BrowserUtil.Open(uri);
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            var tokenRequest = new AuthorizationCodeTokenRequest(clientId, clientSecret, response.Code, new Uri(redirectUri));
            var tokenResponse = await new OAuthClient().RequestToken(tokenRequest);

            var config = SpotifyClientConfig.CreateDefault()
                .WithAuthenticator(new AuthorizationCodeAuthenticator(clientId, clientSecret, tokenResponse));

            SpotifyClientInstance = new SpotifyClient(config);
            Console.WriteLine("Spotify is now connected.");
        }

        public static async Task<bool> PlayTrackBySearchAsync(string query)
        {
            if (SpotifyClientInstance == null) return false;

            var search = await SpotifyClientInstance.Search.Item(new SearchRequest(SearchRequest.Types.Track, query));
            var firstTrack = search.Tracks?.Items?.FirstOrDefault();

            if (firstTrack == null) return false;

            var playback = new PlayerResumePlaybackRequest
            {
                Uris = new[] { firstTrack.Uri }
            };

            await SpotifyClientInstance.Player.ResumePlayback(playback);
            Console.WriteLine($"Now playing: {firstTrack.Name} by {firstTrack.Artists[0].Name}");
            return true;
        }
    }
}
