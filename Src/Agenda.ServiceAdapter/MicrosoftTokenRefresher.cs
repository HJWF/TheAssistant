//using Microsoft.Graph.Models.ExternalConnectors;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using TheAssistant.Agenda.ServiceAdapter;
//using TheAssistant.Core.Authentication;

//namespace TheAssistant.Agents.ServiceAdapter
//{
//    public class MicrosoftTokenRefresher
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _clientId;
//        private readonly string _clientSecret;
//        private readonly string _redirectUri;
//        private readonly AgendaSettings _agendaSettings;

//        public MicrosoftTokenRefresher(HttpClient httpClient, IOp)
//        {
//            _httpClient = httpClient;
//            _clientId = config["Agenda:ClientId"];
//            _clientSecret = config["Agenda:ClientSecret"];
//            _redirectUri = config["Agenda:RedirectUri"];
//        }

//        public async Task<Token?> RefreshTokenAsync(string refreshToken)
//        {
//            var content = new FormUrlEncodedContent(new[]
//            {
//            new KeyValuePair<string, string>("client_id", _clientId),
//            new KeyValuePair<string, string>("client_secret", _clientSecret),
//            new KeyValuePair<string, string>("refresh_token", refreshToken),
//            new KeyValuePair<string, string>("grant_type", "refresh_token"),
//            new KeyValuePair<string, string>("redirect_uri", _redirectUri),
//            new KeyValuePair<string, string>("scope", "offline_access Calendars.Read")
//        });

//            var response = await _httpClient.PostAsync("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", content);
//            if (!response.IsSuccessStatusCode)
//            {
//                var body = await response.Content.ReadAsStringAsync();
//                throw new Exception($"Failed to refresh token: {response.StatusCode}, Body: {body}");
//            }

//            var json = await response.Content.ReadAsStringAsync();
//            var doc = JsonDocument.Parse(json);
//            var root = doc.RootElement;

//            return new Token
//            {
//                AccessToken = root.GetProperty("access_token").GetString()!,
//                RefreshToken = root.GetProperty("refresh_token").GetString()!,
//                ExpiresAt = DateTime.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32())
//            };
//        }
//    }

//}
