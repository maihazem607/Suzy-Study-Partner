using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Net.Http;

namespace Suzy.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _model = "models/gemini-1.5-flash";

        // MODIFIED: This path now points to the standardized filename.
        // This ensures it uses the file uploaded via the Settings page.
        private readonly string _serviceAccountPath = "suzy-gemini-key.json";

        public async Task<string> GenerateContentAsync(string prompt)
        {
            GoogleCredential credential;
            
            // This will now correctly look for the file uploaded by the user.
            await using (var stream = new FileStream(_serviceAccountPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(
                        "https://www.googleapis.com/auth/generative-language.retriever",
                        "https://www.googleapis.com/auth/generative-language"
                    );
            }

            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            var payload = new
            {
                contents = new[]
                {
                    new {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://generativelanguage.googleapis.com/v1beta/{_model}:generateContent"),
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", accessToken)
                },
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error calling Gemini: {response.StatusCode}, {responseContent}");
            }

            return responseContent;
        }

        // ... The rest of your service (GenerateContentWithRetryAsync) remains unchanged ...
        public async Task<string> GenerateContentWithRetryAsync(string prompt, int retries = 3)
        {
            for (int attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    return await GenerateContentAsync(prompt);
                }
                catch (Exception ex)
                {
                    if (attempt == retries || !ex.Message.Contains("503"))
                        throw;

                    await Task.Delay(1000 * attempt); // wait 1s, then 2s...
                }
            }

            throw new Exception("Max retries exceeded.");
        }
    }
}