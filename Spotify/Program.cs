using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Spotify
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string clientId = "3767cb2750d04375a5604e6045041732"; // Replace with your actual client ID
            string redirectUri = "http://localhost/callback/"; // Replace with your actual redirect URI
            string scope = "user-read-private user-read-email user-top-read"; // Scopes requested by your application

            // Set up HTTP listener
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(redirectUri);
                listener.Start();
                Console.WriteLine("HTTP listener started successfully.");

                try
                {
                    string authorizationCode = await GetAuthorizationCode(clientId, redirectUri, scope, listener);
                    if (!string.IsNullOrEmpty(authorizationCode))
                    {
                        string accessToken = await GetAccessToken(clientId, redirectUri, authorizationCode);
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            Console.WriteLine($"Access Token: {accessToken}");

                            // Fetch top tracks
                            await GetTopTracks(accessToken);

                            // Fetch top artists
                            await GetTopArtists(accessToken);
                        }
                    }

                    // Keep the listener running until explicitly stopped
                    Console.WriteLine("Press any key to stop the HTTP listener.");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                // Stop the listener
                listener.Stop();
                Console.WriteLine("HTTP listener stopped.");
            }
        }

        static async Task<string> GetAuthorizationCode(string clientId, string redirectUri, string scope, HttpListener listener)
        {
            var state = GenerateRandomString(16); // Generate a random state
            var authorizationUrl = $"https://accounts.spotify.com/authorize?" +
                $"response_type=code" +
                $"&client_id={clientId}" +
                $"&scope={scope}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&state={state}";

            Console.WriteLine($"Please visit the following URL to authorize the application: {authorizationUrl}");

            // Wait for the callback and handle it
            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];
            var receivedState = context.Request.QueryString["state"];

            if (receivedState != state)
            {
                throw new Exception("State mismatch.");
            }

            Console.WriteLine($"Authorization code received: {code}");

            // Close the listener
            return code;
        }

        static async Task<string> GetAccessToken(string clientId, string redirectUri, string authorizationCode)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string tokenUrl = "https://accounts.spotify.com/api/token";
                    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
                    tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "grant_type", "authorization_code" },
                        { "code", authorizationCode },
                        { "redirect_uri", redirectUri }
                    });
                    tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:56b31926bcb34db4a52649d9f383d1fa"))); // Replace YOUR_CLIENT_SECRET with your actual client secret

                    HttpResponseMessage response = await client.SendAsync(tokenRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                        string accessToken = jsonResponse.access_token;
                        Console.WriteLine("Access token retrieved successfully.");
                        return accessToken;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve access token. Status code: {response.StatusCode}");
                        string errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error response: {errorResponse}");
                        return null;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An HTTP request error occurred: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while retrieving access token: {ex.Message}");
                return null;
            }
        }

        static async Task GetTopTracks(string accessToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                    HttpResponseMessage response = await client.GetAsync("https://api.spotify.com/v1/me/top/tracks?limit=5");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                        Console.WriteLine("\nTop 5 Tracks:");
                        foreach (var track in jsonResponse.items)
                        {
                            Console.WriteLine($"- {track.name} by {track.artists[0].name}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve top tracks. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An HTTP request error occurred: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while retrieving top tracks: {ex.Message}");
            }
        }

        static async Task GetTopArtists(string accessToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                    HttpResponseMessage response = await client.GetAsync("https://api.spotify.com/v1/me/top/artists?limit=5");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                        Console.WriteLine("\nTop 5 Artists:");
                        foreach (var artist in jsonResponse.items)
                        {
                            Console.WriteLine($"- {artist.name}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve top artists. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An HTTP request error occurred: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while retrieving top artists: {ex.Message}");
            }
        }

        static string GenerateRandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
