using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using UdemyBagisSistemi.ViewModels;

namespace UdemyBagisSistemi.Servisler;

public class OpenAiAssistantServisi : IAiAssistantServisi
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    // PlatformServisi Scoped olduğu için IServiceProvider üzerinden resolve edeceğiz
    public OpenAiAssistantServisi(HttpClient httpClient, IConfiguration config, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> YanitUretAsync(
        string message,
        IReadOnlyList<ChatMessageDto> history,
        AgentBaglam? baglam = null,
        CancellationToken cancellationToken = default)
    {
        var provider = _config["AI:Provider"] ?? "OpenAI";
        string apiKey;
        string model;
        string baseUrl;

        // PlatformServisi scope'u oluştur (hem tool calling hem API key havuzu için)
        using var scope = _serviceProvider.CreateScope();
        var platformServisi = scope.ServiceProvider.GetRequiredService<PlatformServisi>();

        if (provider.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase))
        {
            // API key havuzundan round-robin ile al
            try
            {
                apiKey = platformServisi.AiApiAnahtariGetir();
            }
            catch
            {
                // Havuzda key yoksa config'den dene
                apiKey = _config["AI:OpenRouter:ApiKey"] ?? string.Empty;
            }

            model = _config["AI:OpenRouter:Model"] ?? "openai/gpt-4o-mini";
            baseUrl = _config["AI:BaseUrl"] ?? "https://openrouter.ai/api/v1";

            if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("OpenRouter API key bulunamadı.");
        }
        else
        {
            apiKey = _config["AI:OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("AI_OPENAI_API_KEY") ?? string.Empty;
            model = _config["AI:OpenAI:Model"] ?? "gpt-4o-mini";
            baseUrl = "https://api.openai.com/v1";

            if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("OpenAI API key bulunamadı.");
        }

        var systemPrompt =
            $"Sen EduVerse (UdemyBagisSistemi) platformunda kullanıcılara kariyer ve eğitim yol haritası (roadmap) çizen ve platformdaki kursları öneren akıllı bir asistansın.\n" +
            $"Kullanıcının adı: {baglam?.AdSoyad ?? "Bilinmeyen Kullanıcı"}, Rolü: {baglam?.Rol ?? "Ziyaretçi"}.\n" +
            $"GÖREVLERİN:\n" +
            $"1. Kullanıcı bir konu öğrenmek istediğini söylediğinde, onun için bir öğrenme yol haritası (roadmap) oluştur.\n" +
            $"2. Sadece genel bir plan vermekle kalma, her adım için 'kurs_ara' fonksiyonunu kullanarak platformumuzdaki YAYINLANMIŞ GERÇEK KURSLARI bul ve öner.\n" +
            $"3. Kursları önerirken [ID: X] formatındaki ID'yi gizleme, mutlaka göster ki sonradan sepete ekleyebilelim.\n" +
            $"4. Gerekirse 'kategorileri_listele' ile hangi alanlarda eğitim olduğunu kontrol et.\n" +
            $"5. Eğer kullanıcı giriş yapmışsa (Ziyaretçi değilse), 'kullanici_bilgisi_getir' ile profilini analiz et ve hedefine uygun özel öneriler yap.\n" +
            $"6. Kullanıcı bir kursu beğendiğinde veya sepete atmanı istediğinde 'sepete_ekle' fonksiyonunu kullan. Ziyaretçi ise bu işlemi yapamayacağını belirt.\n" +
            $"7. Soruları kısa, dostça ve Türkçe cevapla. Roadmap'i markdown ile güzelce şekillendir.";

        // Messages listesi oluştur
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        var trimmedHistory = history?.TakeLast(12).ToList() ?? [];
        foreach (var h in trimmedHistory)
        {
            var role = h.Role?.ToLowerInvariant() == "assistant" ? "assistant" : "user";
            messages.Add(new { role = role, content = h.Content ?? string.Empty });
        }

        messages.Add(new { role = "user", content = message });

        // Tool tanımları
        var tools = new object[]
        {
            new {
                type = "function",
                function = new {
                    name = "kurs_ara",
                    description = "Platformda yayınlanmış kursları başlık, açıklama veya kategoriye göre arar.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            aramaMetni = new {
                                type = "string",
                                description = "Aranacak kelime veya konu (örn: 'c#', 'react', 'pazarlama')"
                            }
                        },
                        required = new[] { "aramaMetni" },
                        additionalProperties = false
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "kategorileri_listele",
                    description = "Platformdaki tüm mevcut kategorileri listeler.",
                    parameters = new {
                        type = "object",
                        properties = new Dictionary<string, object>(),
                        additionalProperties = false
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "tum_kurslari_listele",
                    description = "Platformdaki yayınlı kursların genel bir listesini (son 20 tane) getirir.",
                    parameters = new {
                        type = "object",
                        properties = new Dictionary<string, object>(),
                        additionalProperties = false
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "kullanici_bilgisi_getir",
                    description = "Şu anki kullanıcının profil bilgilerini (eğitim seviyesi, hedefi, aldığı kurslar vb.) getirir.",
                    parameters = new {
                        type = "object",
                        properties = new Dictionary<string, object>(),
                        additionalProperties = false
                    }
                }
            },
            new {
                type = "function",
                function = new {
                    name = "sepete_ekle",
                    description = "Belirtilen kursu kullanıcının sepetine ekler.",
                    parameters = new {
                        type = "object",
                        properties = new {
                            kursId = new {
                                type = "integer",
                                description = "Sepete eklenecek kursun ID numarası"
                            }
                        },
                        required = new[] { "kursId" },
                        additionalProperties = false
                    }
                }
            }
        };

        // Tool Calling Loop (Max 5 iteration)
        for (int i = 0; i < 5; i++)
        {
            var body = new
            {
                model,
                messages,
                tools,
                temperature = 0.2,
                max_tokens = 1000
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

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

            var doc = JsonNode.Parse(responseText);
            var choice = doc?["choices"]?[0];
            if (choice == null) break;

            var finishReason = choice["finish_reason"]?.ToString();
            var responseMessage = choice["message"];

            // Model yanıtını (assistant mesajını) içeriğe ekle
            if (responseMessage != null)
            {
                // OpenRouter/OpenAI tool call mesajını deserializeıp aynen listeye ekliyoruz
                messages.Add(responseMessage);
            }

            if (finishReason == "tool_calls")
            {
                var toolCalls = responseMessage?["tool_calls"]?.AsArray();
                if (toolCalls != null)
                {
                    foreach (var tc in toolCalls)
                    {
                        var toolCallId = tc["id"]?.ToString();
                        var functionName = tc["function"]?["name"]?.ToString();
                        var argsStr = tc["function"]?["arguments"]?.ToString() ?? "{}";
                        
                        string toolResult = "";

                        try
                        {
                            var args = JsonDocument.Parse(argsStr).RootElement;
                            if (functionName == "kurs_ara")
                            {
                                var query = args.TryGetProperty("aramaMetni", out var prop) ? prop.GetString() : "";
                                toolResult = platformServisi.AgentKursAra(query ?? "");
                            }
                            else if (functionName == "kategorileri_listele")
                            {
                                toolResult = platformServisi.AgentKategorileriListele();
                            }
                            else if (functionName == "tum_kurslari_listele")
                            {
                                toolResult = platformServisi.AgentTumKurslariListele();
                            }
                            else if (functionName == "kullanici_bilgisi_getir")
                            {
                                if (baglam?.KullaniciId != null && baglam.KullaniciId > 0)
                                    toolResult = platformServisi.AgentKullaniciBilgisiGetir(baglam.KullaniciId.Value);
                                else
                                    toolResult = "Kullanıcı giriş yapmamış. Ziyaretçi olarak görünüyor.";
                            }
                            else if (functionName == "sepete_ekle")
                            {
                                if (baglam?.KullaniciId != null && baglam.KullaniciId > 0)
                                {
                                    if (args.TryGetProperty("kursId", out var kursIdProp) && kursIdProp.TryGetInt32(out var kId))
                                        toolResult = platformServisi.AgentSepeteEkle(baglam.KullaniciId.Value, kId);
                                    else
                                        toolResult = "kursId parametresi hatalı.";
                                }
                                else
                                {
                                    toolResult = "Sepete eklemek için lütfen giriş yapın.";
                                }
                            }
                            else
                            {
                                toolResult = "Bilinmeyen fonksiyon: " + functionName;
                            }
                        }
                        catch (Exception ex)
                        {
                            toolResult = "Hata oluştu: " + ex.Message;
                        }

                        // Tool sonucunu listeye ekle
                        messages.Add(new {
                            role = "tool",
                            tool_call_id = toolCallId,
                            content = toolResult
                        });
                    }
                    continue; // Loop tekrar dönsün
                }
            }

            // Normal içerik dönüşü
            return responseMessage?["content"]?.ToString() ?? string.Empty;
        }

        return "Yanıt oluşturulamadı.";
    }
}
