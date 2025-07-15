using System.Text;
using System.Text.Json;

namespace Suzy.Services
{
    public class Flashcard
    {
        public string Front { get; set; } = string.Empty;
        public string Back { get; set; } = string.Empty;
    }

    public class OpenAIService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public OpenAIService(IConfiguration config, HttpClient httpClient)
        {
            _apiKey = config["OpenAI:ApiKey"] ?? "test-mode";
            _httpClient = httpClient;
        }

        public async Task<List<Flashcard>> GenerateFlashcardsAsync(string studyMaterial)
        {
            // Test mode if no real API key is provided
            if (_apiKey == "test-mode" || _apiKey == "your-actual-openai-api-key-here" || string.IsNullOrEmpty(_apiKey))
            {
                await Task.Delay(1000); // Simulate API delay
                return GenerateTestFlashcards(studyMaterial);
            }

            var messages = new[]
            {
                new { role = "system", content = "You are a flashcard generator. Create useful flashcards from study material. Return ONLY a JSON array of objects with 'front' and 'back' properties. Example: [{\"front\":\"What is photosynthesis?\",\"back\":\"The process by which plants convert light energy into chemical energy\"}]. Create 3-5 flashcards maximum." },
                new { role = "user", content = $"Create flashcards from this study material:\n\n{studyMaterial}" }
            };

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages,
                temperature = 0.7,
                max_tokens = 500
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                // Add retry logic for rate limits
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    var response = await _httpClient.SendAsync(request);
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(jsonResponse);
                        var content = doc.RootElement
                                         .GetProperty("choices")[0]
                                         .GetProperty("message")
                                         .GetProperty("content")
                                         .GetString();

                        // Parse the JSON array of flashcards
                        var flashcards = JsonSerializer.Deserialize<List<Flashcard>>(content ?? "[]", new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return flashcards ?? new List<Flashcard>();
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        if (attempt < 3)
                        {
                            // Wait before retrying (exponential backoff)
                            await Task.Delay(1000 * attempt);
                            continue;
                        }
                        else
                        {
                            // After 3 attempts, fall back to test mode
                            return GenerateTestFlashcards($"Rate limited after {attempt} attempts - using test flashcards for: {studyMaterial}");
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Invalid API key - fall back to test mode
                        return GenerateTestFlashcards($"API key issue - using test flashcards for: {studyMaterial}");
                    }
                    else
                    {
                        throw new Exception($"OpenAI API error: {response.StatusCode} - {jsonResponse}");
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to test flashcards if API fails
                return GenerateTestFlashcards($"API Error - using test flashcards for: {studyMaterial}");
            }

            return new List<Flashcard>();
        }

        private List<Flashcard> GenerateTestFlashcards(string studyMaterial)
        {
            // Generate test flashcards based on the study material
            var testFlashcards = new List<Flashcard>();

            if (studyMaterial.ToLower().Contains("photosynthesis"))
            {
                testFlashcards.AddRange(new[]
                {
                    new Flashcard { Front = "What is photosynthesis?", Back = "The process by which plants convert light energy into chemical energy using carbon dioxide and water." },
                    new Flashcard { Front = "Where does photosynthesis occur?", Back = "In the chloroplasts of plant cells, specifically in the thylakoids and stroma." },
                    new Flashcard { Front = "What is the equation for photosynthesis?", Back = "6CO2 + 6H2O + light energy → C6H12O6 + 6O2" },
                    new Flashcard { Front = "What are the two main stages of photosynthesis?", Back = "Light-dependent reactions (in thylakoids) and light-independent reactions (Calvin Cycle in stroma)." },
                    new Flashcard { Front = "What factors affect photosynthesis?", Back = "Light intensity, carbon dioxide concentration, temperature, and water availability." }
                });
            }
            else
            {
                // Generic test flashcards
                testFlashcards.AddRange(new[]
                {
                    new Flashcard { Front = "Test Mode Active", Back = "This is a test flashcard. Add your OpenAI API key to generate real flashcards." },
                    new Flashcard { Front = "Sample Question 1", Back = $"This flashcard was generated from: {studyMaterial.Substring(0, Math.Min(50, studyMaterial.Length))}..." },
                    new Flashcard { Front = "Sample Question 2", Back = "Replace the API key in user secrets with your actual OpenAI API key to get AI-generated flashcards." },
                    new Flashcard { Front = "How to add API key?", Back = "Run: dotnet user-secrets set \"OpenAI:ApiKey\" \"your-real-api-key\"" }
                });
            }

            return testFlashcards;
        }
    }
}
