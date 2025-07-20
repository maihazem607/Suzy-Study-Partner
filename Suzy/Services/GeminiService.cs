using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Suzy.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _model = "models/gemini-1.5-flash";
        private readonly string _serviceAccountPath = "suzy-466413-536698e4fca7.json"; // Change path if needed

        public async Task<string> GenerateContentAsync(string prompt)
        {
            GoogleCredential credential;
            using (var stream = new FileStream(_serviceAccountPath, FileMode.Open, FileAccess.Read))
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

        // ✅ Retry wrapper to handle 503 "model is overloaded" errors
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

                    await Task.Delay(1000 * attempt); // wait 1s, then 2s, then 3s...
                }
            }

            throw new Exception("Max retries exceeded.");
        }
    }
}
