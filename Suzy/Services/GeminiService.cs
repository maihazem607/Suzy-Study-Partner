using System.Text;
using System.Text.Json;

namespace Suzy.Services
{
    public class GeminiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration config, HttpClient httpClient, ILogger<GeminiService> logger)
        {
            _apiKey = config["Gemini:ApiKey"] ?? "test-mode";
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Flashcard>> GenerateFlashcardsAsync(string studyMaterial)
        {
            // Test mode if no real API key is provided
            if (_apiKey == "test-mode" || string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogInformation("Using test mode for flashcard generation");
                await Task.Delay(1000); // Simulate API delay
                return GenerateTestFlashcards(studyMaterial);
            }

            try
            {
                _logger.LogInformation("Calling Gemini API to generate flashcards...");

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = $@"Create exactly 5 flashcards from this study material. Return ONLY a valid JSON array with no additional text or formatting.

Study Material: {studyMaterial}

Format each flashcard as:
{{""Front"": ""question or term"", ""Back"": ""answer or definition""}}

Return format: [{{""Front"": ""..."", ""Back"": ""...""}}]"
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1000
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Gemini API response received successfully");

                    return ParseGeminiResponse(responseContent);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Gemini API failed with status {response.StatusCode}: {errorContent}");

                    // Fall back to test flashcards
                    return GenerateTestFlashcards(studyMaterial);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return GenerateTestFlashcards(studyMaterial);
            }
        }

        private List<Flashcard> ParseGeminiResponse(string responseContent)
        {
            try
            {
                using var document = JsonDocument.Parse(responseContent);

                // Gemini response structure: candidates[0].content.parts[0].text
                var text = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(text))
                {
                    _logger.LogWarning("Empty text received from Gemini API");
                    return GenerateTestFlashcards("fallback");
                }

                // Clean the text - remove markdown formatting if present
                text = text.Trim();
                if (text.StartsWith("```json"))
                {
                    text = text.Substring(7);
                }
                if (text.EndsWith("```"))
                {
                    text = text.Substring(0, text.Length - 3);
                }
                text = text.Trim();

                // Parse the flashcards JSON
                var flashcards = JsonSerializer.Deserialize<List<Flashcard>>(text);

                if (flashcards != null && flashcards.Count > 0)
                {
                    _logger.LogInformation($"Successfully parsed {flashcards.Count} flashcards from Gemini response");
                    return flashcards;
                }
                else
                {
                    _logger.LogWarning("No flashcards found in Gemini response");
                    return GenerateTestFlashcards("fallback");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Gemini response");
                return GenerateTestFlashcards("fallback");
            }
        }

        private List<Flashcard> GenerateTestFlashcards(string studyMaterial)
        {
            _logger.LogInformation("Generating test flashcards as fallback");

            // Create topic-aware test flashcards based on the study material
            var topic = ExtractTopic(studyMaterial);

            return new List<Flashcard>
            {
                new Flashcard { Front = $"What is the main concept in {topic}?", Back = "This is a test flashcard generated from your study material." },
                new Flashcard { Front = $"Why is {topic} important?", Back = "Test flashcards help verify the system is working correctly." },
                new Flashcard { Front = $"How does {topic} work?", Back = "The system processes your study material and creates relevant questions." },
                new Flashcard { Front = $"What are the key features of {topic}?", Back = "Automated generation, customizable difficulty, and progress tracking." },
                new Flashcard { Front = $"When should you use {topic}?", Back = "Use this system when you need to quickly create study materials for any subject." }
            };
        }

        private string ExtractTopic(string studyMaterial)
        {
            if (string.IsNullOrWhiteSpace(studyMaterial))
                return "your study topic";

            // Simple topic extraction - take first meaningful word or phrase
            var words = studyMaterial.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Look for capitalized words that might be topics
            var topic = words.FirstOrDefault(w => w.Length > 3 && char.IsUpper(w[0]));

            return topic?.ToLowerInvariant() ?? "the subject";
        }
    }
}
