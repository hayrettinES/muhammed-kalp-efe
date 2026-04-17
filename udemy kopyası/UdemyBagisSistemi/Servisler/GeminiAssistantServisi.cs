using System.Text;
using System.Text.Json;
using UdemyBagisSistemi.ViewModels;

namespace UdemyBagisSistemi.Servisler;

public class GeminiAssistantServisi : IAiAssistantServisi
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public GeminiAssistantServisi(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> YanitUretAsync(
        string message,
        IReadOnlyList<ChatMessageDto> history,
        CancellationToken cancellationToken = default)
    {
        var apiKey =
            _config["AI:Gemini:ApiKey"] ??
            Environment.GetEnvironmentVariable("AI_GEMINI_API_KEY") ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key bulunamadı. AI:Gemini:ApiKey veya AI_GEMINI_API_KEY ayarla.");
        }

        var model = _config["AI:Gemini:Model"];
        if (string.IsNullOrWhiteSpace(model))
        {
            model = "gemini-2.0-flash";
        }

        var systemPrompt =
            "Sen EduVerse (UdemyBagisSistemi) için yardımcı bir asistansın. Kullanıcının sorularını Türkçe cevapla. " +
            "Kullanıcı eğitim platformu, kayıt/giriş, kurs yönetimi gibi konular hakkında soru sorabilir. " +
            "Gerçek dünya doğrulaması gerektiren iddialar yapma; emin değilsen sor. " +
            "Kısa ve net cevap ver. Gerektiğinde madde madde yaz.";

        // Gemini contents formatı
        var contents = new List<object>();

        // History'yi ekle (son 12 mesaj)
        var trimmedHistory = history?.TakeLast(12).ToList() ?? [];
        foreach (var h in trimmedHistory)
        {
            var role = h.Role?.ToLowerInvariant() == "assistant" ? "model" : "user";
            contents.Add(new
            {
                role,
                parts = new[] { new { text = h.Content ?? string.Empty } }
            });
        }

        // Son kullanıcı mesajı
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = message } }
        });

        var body = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents,
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 700
            }
        };

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini isteği başarısız: {response.StatusCode} - {responseText}");
        }

        using var doc = JsonDocument.Parse(responseText);
        var reply =
            doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

        return reply ?? string.Empty;
    }
}
