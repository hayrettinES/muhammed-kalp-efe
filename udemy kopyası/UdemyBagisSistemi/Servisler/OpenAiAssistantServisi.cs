using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UdemyBagisSistemi.ViewModels;

namespace UdemyBagisSistemi.Servisler;

public class OpenAiAssistantServisi : IAiAssistantServisi
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public OpenAiAssistantServisi(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> YanitUretAsync(
        string message,
        IReadOnlyList<ChatMessageDto> history,
        CancellationToken cancellationToken = default)
    {
        var provider = _config["AI:Provider"] ?? "OpenAI";

        // Provider'a göre API key, model ve base URL belirle
        string apiKey;
        string model;
        string baseUrl;

        if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            apiKey =
                _config["AI:OpenRouter:ApiKey"] ??
                Environment.GetEnvironmentVariable("AI_OPENROUTER_API_KEY") ??
                string.Empty;
            model = _config["AI:OpenRouter:Model"] ?? "openai/gpt-4o-mini";
            baseUrl = _config["AI:BaseUrl"] ?? "https://openrouter.ai/api/v1";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenRouter API key bulunamadı. AI:OpenRouter:ApiKey ayarla.");
            }
        }
        else
        {
            apiKey =
                _config["AI:OpenAI:ApiKey"] ??
                Environment.GetEnvironmentVariable("AI_OPENAI_API_KEY") ??
                string.Empty;
            model = _config["AI:OpenAI:Model"] ?? "gpt-4o-mini";
            baseUrl = "https://api.openai.com/v1";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key bulunamadı. AI:OpenAI:ApiKey ayarla.");
            }
        }

        var systemPrompt =
            "Sen EduVerse (UdemyBagisSistemi) için yardımcı bir asistansın. Kullanıcının sorularını Türkçe cevapla. " +
            "Kullanıcı eğitim platformu, kayıt/giriş, kurs yönetimi gibi konular hakkında soru sorabilir. " +
            "Gerçek dünya doğrulaması gerektiren iddialar yapma; emin değilsen sor. " +
            "Kısa ve net cevap ver. Gerektiğinde madde madde yaz.";

        // OpenAI mesaj formatı: system/user/assistant
        var messages = new List<Dictionary<string, string>>
        {
            new() { ["role"] = "system", ["content"] = systemPrompt }
        };

        // Önce history'yi ekleyelim (client rol olarak user/assistant gönderiyor).
        // Sınır: son 12 mesaj yeterli (aşırı payload istemeyiz).
        var trimmedHistory = history?.TakeLast(12).ToList() ?? [];
        foreach (var h in trimmedHistory)
        {
            var role = h.Role?.ToLowerInvariant() == "assistant" ? "assistant" : "user";
            messages.Add(new Dictionary<string, string>
            {
                ["role"] = role,
                ["content"] = h.Content ?? string.Empty
            });
        }

        // Son kullanıcı mesajı
        messages.Add(new Dictionary<string, string>
        {
            ["role"] = "user",
            ["content"] = message
        });

        var body = new
        {
            model,
            messages,
            temperature = 0.2,
            max_tokens = 700
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        // OpenRouter için gerekli header'lar
        if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Add("HTTP-Referer", "https://eduverse.app");
            request.Headers.Add("X-OpenRouter-Title", "EduVerse");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"AI isteği başarısız: {response.StatusCode} - {responseText}");
        }

        using var doc = JsonDocument.Parse(responseText);
        var reply =
            doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

        return reply ?? string.Empty;
    }
}

