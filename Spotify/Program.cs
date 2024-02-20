using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            string redirectUri = "http://192.168.1.48/callback/"; // Replace with your actual redirect URI
            string scope = "user-read-private user-read-email user-top-read playlist-modify-public playlist-modify-private";// Scopes requested by your application

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
                            List<string> userTopTrackUris = await GetUserTopTrackUris(accessToken);
                            var (playlistId, genres) = await CreatePersonalizedPlaylist(accessToken);
                            if (!string.IsNullOrEmpty(playlistId))
                            {
                                await AddNewTracksByGenreToPlaylist(accessToken, playlistId, genres);
                            }
                            //await AddGenreToPlaylist(accessToken, playlistId);
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
        static bool GenresMatch(List<string> artistGenres, List<string> userGenres)
        {
            var normalizedUserGenres = userGenres.Select(g => g.ToLower().Replace("-", "").Replace(" ", ""));
            var normalizedArtistGenres = artistGenres.Select(g => g.ToLower().Replace("-", "").Replace(" ", ""));

            return normalizedArtistGenres.Any(artistGenre => normalizedUserGenres.Any(userGenre => artistGenre.Contains(userGenre)));
        }


        static async Task<(string playlistId, List<string> genres)> CreatePersonalizedPlaylist(string accessToken)
        {
            Console.WriteLine("Enter the genres separated by comma (e.g., rock,pop,hip-hop):");
            string genreInput = Console.ReadLine();
            List<string> genres = genreInput.Split(',').Select(g => g.Trim()).ToList();

            Console.WriteLine("Enter the number of songs you want in the playlist:");
            if (!int.TryParse(Console.ReadLine(), out int playlistLength))
            {
                Console.WriteLine("Invalid input for the number of songs. Please enter a valid number.");
                return (null, null); // Return nulls in case of invalid input
            }

            // Fetch the user's top artists and their genres
            List<(string artistId, List<string> genres)> topArtistsGenres = await GetUserTopArtistsGenres(accessToken);

            // Filter artists by the specified genres
            var filteredArtists = topArtistsGenres.Where(artist => GenresMatch(artist.genres, genres)).ToList();

            Console.WriteLine($"User specified genres: {string.Join(", ", genres)}");
            Console.WriteLine($"Filtered artists count: {filteredArtists.Count}");

            if (filteredArtists.Count == 0)
            {
                Console.WriteLine("No artists matched the specified genres.");
                return (null, genres); // Return genres but null playlist ID if no artists match
            }

            // Initialize variables for playlist creation
            List<string> playlistTracks = new List<string>();
            HashSet<string> addedArtistIds = new HashSet<string>();

            // Add tracks from filtered artists, respecting the limit of 2 songs per artist
            foreach (var artist in filteredArtists)
            {
                if (playlistTracks.Count >= playlistLength) break; // Check if the playlist already has the desired number of songs
                var artistTracks = await FetchTopTracksByArtist(accessToken, artist.artistId);
                foreach (var track in artistTracks)
                {
                    if (addedArtistIds.Count(artistId => artistId == artist.artistId) < 2)
                    {
                        playlistTracks.Add(track);
                        addedArtistIds.Add(artist.artistId);
                        if (playlistTracks.Count >= playlistLength) break;
                    }
                }
            }

            // Create the playlist and add tracks to it
            string playlistId = await CreatePlaylist(accessToken, "Your Personalized Playlist");
            if (playlistTracks.Count == 0)
            {
                Console.WriteLine("No tracks were found for the specified genres or top artists.");
                return (playlistId, genres); // Return the created playlist ID and genres, even if no tracks were added
            }
            else
            {
                await AddTracksToPlaylist(accessToken, playlistId, playlistTracks);
                Console.WriteLine($"Playlist created with ID: {playlistId}. Tracks added: {playlistTracks.Count}");
                return (playlistId, genres); // Return both the playlist ID and the genres
            }
        }

        static async Task<List<string>> FetchTopTracksByArtist(string accessToken, string artistId)
        {
            List<string> topTracksUris = new List<string>();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                string url = $"https://api.spotify.com/v1/artists/{artistId}/top-tracks?market=US";

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Top tracks for artist ID {artistId}: {content}"); // Debugging line
                    dynamic jsonResponse = JsonConvert.DeserializeObject(content);

                    foreach (var track in jsonResponse.tracks)
                    {
                        topTracksUris.Add((string)track.uri);
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to fetch top tracks for artist {artistId}. Response: {response.StatusCode}");
                }
            }
            return topTracksUris;
        }



        static async Task<List<(string artistId, List<string> genres)>> GetUserTopArtistsGenres(string accessToken)
        {
            List<(string artistId, List<string> genres)> topArtistsGenres = new List<(string artistId, List<string> genres)>();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                string url = "https://api.spotify.com/v1/me/top/artists?limit=20";

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(content);

                    foreach (var item in jsonResponse.items)
                    {
                        List<string> genres = new List<string>();
                        foreach (var genre in item.genres)
                        {
                            genres.Add((string)genre);
                        }
                        topArtistsGenres.Add((item.id.ToString(), genres));
                        Console.WriteLine($"Artist ID: {item.id}, Genres: {string.Join(", ", genres)}"); // Debugging line
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve user's top artists. Status code: {response.StatusCode}");
                }
            }
            return topArtistsGenres;
        }


        static async Task<List<string>> GetUserTopTrackUris(string accessToken)
        {
            List<string> topTrackUris = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                // Set the authorization header with the access token
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Define the URL for fetching the user's top tracks. You can adjust the limit and time_range as needed.
                string url = "https://api.spotify.com/v1/me/top/tracks?limit=50";

                try
                {
                    // Make the GET request to Spotify's API
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the response body
                        string content = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(content);

                        // Extract the URIs from the top tracks
                        foreach (var item in jsonResponse.items)
                        {
                            topTrackUris.Add((string)item.uri);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve user's top tracks. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while fetching user's top tracks: {ex.Message}");
                }
            }

            return topTrackUris;
        }

        static async Task<List<(string uri, string artistId)>> SearchTracksByGenre(string accessToken, string genre)
        {
            List<(string uri, string artistId)> trackData = new List<(string uri, string artistId)>();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                string searchUrl = $"https://api.spotify.com/v1/search?q=genre:{Uri.EscapeDataString(genre)}&type=artist&limit=10";
                HttpResponseMessage response = await client.GetAsync(searchUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                    foreach (var artist in jsonResponse.artists.items)
                    {
                        string artistId = artist.id.ToString();
                        string topTracksUrl = $"https://api.spotify.com/v1/artists/{artistId}/top-tracks?country=US";
                        HttpResponseMessage topTracksResponse = await client.GetAsync(topTracksUrl);

                        if (topTracksResponse.IsSuccessStatusCode)
                        {
                            string topTracksResponseBody = await topTracksResponse.Content.ReadAsStringAsync();
                            dynamic topTracksJsonResponse = JsonConvert.DeserializeObject(topTracksResponseBody);

                            foreach (var track in topTracksJsonResponse.tracks)
                            {
                                trackData.Add((track.uri.ToString(), artistId));
                            }
                        }
                    }
                }
            }
            return trackData;
        }

        static List<string> SelectTracksForPlaylist(List<(string uri, string artistId)> trackData, int remainingSlots, HashSet<string> addedArtistIds)
        {
            List<string> playlistTracks = new List<string>();
            foreach (var track in trackData)
            {
                if (playlistTracks.Count >= remainingSlots) break;
                if (addedArtistIds.Count(artistId => artistId == track.artistId) < 2)
                {
                    playlistTracks.Add(track.uri);
                    addedArtistIds.Add(track.artistId);
                }
            }
            return playlistTracks;
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
        //CREATE PLAYLIST
        static async Task<string> CreatePlaylist(string accessToken, string playlistName)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    // Define the endpoint URL for creating a playlist
                    string createPlaylistUrl = "https://api.spotify.com/v1/me/playlists";

                    // Define the request body with the playlist name
                    var requestBody = new
                    {
                        name = playlistName
                    };

                    // Serialize the request body to JSON
                    var requestBodyJson = JsonConvert.SerializeObject(requestBody);

                    // Create a new HttpRequestMessage for the POST request
                    var request = new HttpRequestMessage(HttpMethod.Post, createPlaylistUrl)
                    {
                        Content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json")
                    };

                    // Send the POST request
                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                        string playlistId = jsonResponse.id;
                        Console.WriteLine($"Playlist created successfully. ID: {playlistId}");
                        return playlistId;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to create playlist. Status code: {response.StatusCode}");
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
                Console.WriteLine($"An error occurred while creating playlist: {ex.Message}");
                return null;
            }
        }

        //ADD TRACKS TO PLAYLIST
        static async Task<bool> AddTracksToPlaylist(string accessToken, string playlistId, List<string> trackUris)
        {
            if (trackUris == null || !trackUris.Any())
            {
                Console.WriteLine("No tracks specified for adding to the playlist.");
                return false;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var content = new StringContent(JsonConvert.SerializeObject(new { uris = trackUris }), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"https://api.spotify.com/v1/playlists/{playlistId}/tracks", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Tracks added to playlist successfully.");
                    return true;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to add tracks to playlist. Status code: {response.StatusCode}");
                    Console.WriteLine($"Error response: {errorResponse}");
                    return false;
                }
            }
        }
        static async Task AddNewTracksByGenreToPlaylist(string accessToken, string playlistId, List<string> genres)
        {
            foreach (var genre in genres)
            {
                // Fetch new artists by genre
                var newArtistsTracks = await SearchTracksByGenre(accessToken, genre);

                // Filter to get a distinct list of tracks, avoiding duplicates and respecting new artists
                var distinctTracks = newArtistsTracks
                    .Where(t => !string.IsNullOrWhiteSpace(t.uri))
                    .GroupBy(t => t.artistId)
                    .Select(grp => grp.First()) // This ensures one track per artist, modify as needed
                    .ToList();

                if (distinctTracks.Count > 0)
                {
                    // Extract URIs from the distinct tracks
                    var trackUris = distinctTracks.Select(t => t.uri).ToList();

                    // Add these tracks to the playlist
                    bool success = await AddTracksToPlaylist(accessToken, playlistId, trackUris);

                    if (success)
                    {
                        Console.WriteLine($"Added {trackUris.Count} new tracks from genre '{genre}' to the playlist.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to add new tracks from genre '{genre}' to the playlist.");
                    }
                }
                else
                {
                    Console.WriteLine($"No new tracks found for genre '{genre}'.");
                }
            }
        }



        //ADD GENRE TO PLAYLIST
        static async Task AddGenreToPlaylist(string accessToken, string playlistId)
        {
            try
            {
                Console.WriteLine("Enter a genre:");
                string genre = Console.ReadLine();

                Console.WriteLine("Enter the desired length of the playlist in minutes:");
                if (!int.TryParse(Console.ReadLine(), out int desiredLength))
                {
                    Console.WriteLine("Invalid input for playlist length. Please enter a valid number of minutes.");
                    return;
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    // Define the endpoint URL for searching artists by genre
                    string searchUrl = $"https://api.spotify.com/v1/search?q=genre:{Uri.EscapeDataString(genre)}&type=artist&limit=10";

                    // Send a GET request to search for artists by the specified genre
                    HttpResponseMessage response = await client.GetAsync(searchUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                        List<string> artistIds = new List<string>();
                        foreach (var artist in jsonResponse.artists.items)
                        {
                            // Add artist ID to the list
                            artistIds.Add(artist.id.ToString());
                        }

                        List<(string uri, string artistId)> trackData = new List<(string uri, string artistId)>();
                        foreach (var artistId in artistIds)
                        {
                            // Define the endpoint URL for retrieving top tracks by the artist
                            string topTracksUrl = $"https://api.spotify.com/v1/artists/{artistId}/top-tracks?country=US";

                            // Send a GET request to retrieve top tracks by the artist
                            HttpResponseMessage topTracksResponse = await client.GetAsync(topTracksUrl);

                            if (topTracksResponse.IsSuccessStatusCode)
                            {
                                string topTracksResponseBody = await topTracksResponse.Content.ReadAsStringAsync();
                                dynamic topTracksJsonResponse = JsonConvert.DeserializeObject(topTracksResponseBody);

                                // Add top track URI and artist ID to the list
                                foreach (var track in topTracksJsonResponse.tracks)
                                {
                                    trackData.Add((track.uri.ToString(), artistId));
                                }
                            }
                        }

                        // Shuffle the track data to ensure randomness in song selection
                        var shuffledTrackData = trackData.OrderBy(x => Guid.NewGuid()).ToList();

                        List<string> trackUris = new List<string>();
                        HashSet<string> artistSet = new HashSet<string>();

                        int totalDuration = 0;
                        foreach (var trackInfo in shuffledTrackData)
                        {
                            // Check if adding this track exceeds the desired length or if the artist's song limit is reached
                            if (totalDuration >= desiredLength * 60 || artistSet.Count(artistId => artistId == trackInfo.artistId) >= 3)
                            {
                                break;
                            }

                            // Get track duration
                            string trackUri = trackInfo.uri;
                            string trackId = trackUri.Split(':').Last();
                            string trackInfoUrl = $"https://api.spotify.com/v1/tracks/{trackId}";
                            HttpResponseMessage trackInfoResponse = await client.GetAsync(trackInfoUrl);
                            if (trackInfoResponse.IsSuccessStatusCode)
                            {
                                string trackInfoResponseBody = await trackInfoResponse.Content.ReadAsStringAsync();
                                dynamic trackInfoJsonResponse = JsonConvert.DeserializeObject(trackInfoResponseBody);
                                int trackDuration = trackInfoJsonResponse.duration_ms / 1000; // Convert duration to seconds

                                // Add track URI to the list
                                trackUris.Add(trackUri);
                                artistSet.Add(trackInfo.artistId);
                                totalDuration += trackDuration;
                            }
                        }

                        // Add the retrieved tracks to the playlist
                        bool addedSuccessfully = await AddTracksToPlaylist(accessToken, playlistId, trackUris);
                        if (addedSuccessfully)
                        {
                            Console.WriteLine($"Tracks from artists of the genre '{genre}' added to the playlist.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to add tracks from artists of the genre '{genre}' to the playlist.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to search for artists by genre '{genre}'. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An HTTP request error occurred: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while adding tracks from artists of the genre to playlist: {ex.Message}");
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