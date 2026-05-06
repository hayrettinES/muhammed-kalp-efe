using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Servisler;
using UdemyBagisSistemi.ViewModels;

namespace UdemyBagisSistemi.Controllers;

[Route("Assistant")]
[Authorize]
public class AssistantController : Controller
{
    private readonly IAiAssistantServisi _assistantServisi;
    private readonly PlatformServisi _platformServisi;

    public AssistantController(IAiAssistantServisi assistantServisi, PlatformServisi platformServisi)
    {
        _assistantServisi = assistantServisi;
        _platformServisi = platformServisi;
    }

    // Bu aksiyon mesaj gönderir ve yanıtı DB'ye kaydeder.
    [HttpPost("Chat")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "İstek gövdesi boş." });

        var message = request.Message?.Trim() ?? string.Empty;
        if (message.Length == 0)
            return BadRequest(new { error = "Mesaj boş olamaz." });

        if (message.Length > 2000)
            return BadRequest(new { error = "Mesaj çok uzun. Lütfen daha kısa yazın." });

        // Basit temizlik: kontrol karakterlerini kırpmak.
        message = Regex.Replace(message, @"\p{C}+", " ");

        var kullaniciId = KullaniciIdGetir();
        if (kullaniciId == 0)
            return Unauthorized(new { error = "Giriş yapmanız gerekiyor." });

        // Sohbet ID'si (yoksa yeni oluştur)
        var sohbetId = request.SohbetId;
        if (sohbetId == 0)
        {
            sohbetId = _platformServisi.AiSohbetOlustur(kullaniciId);
        }
        else
        {
            // Sohbetin bu kullanıcıya ait olduğunu doğrula
            if (!_platformServisi.AiSohbetSahibiMi(sohbetId, kullaniciId))
                return Forbid();
        }

        // Kullanıcı mesajını kaydet
        _platformServisi.AiMesajEkle(sohbetId, "user", message);

        // Sohbetteki mevcut mesajları getir (history olarak)
        var dbMesajlar = _platformServisi.AiMesajlariGetir(sohbetId);
        var history = dbMesajlar
            .Where(m => m.GetValueOrDefault("Rol") != "user" || m.GetValueOrDefault("Icerik") != message || m != dbMesajlar.Last())
            .Select(m => new ChatMessageDto
            {
                Role = m.GetValueOrDefault("Rol") ?? "user",
                Content = m.GetValueOrDefault("Icerik") ?? string.Empty
            })
            .TakeLast(12)
            .ToList();

        // Agent Bağlamı
        AgentBaglam baglam = new AgentBaglam
        {
            KullaniciId = kullaniciId,
            AdSoyad = User.FindFirst(ClaimTypes.Name)?.Value,
            Rol = User.FindFirst(ClaimTypes.Role)?.Value
        };

        try
        {
            var reply = await _assistantServisi.YanitUretAsync(
                message,
                history,
                baglam,
                cancellationToken);

            // Asistan yanıtını kaydet
            _platformServisi.AiMesajEkle(sohbetId, "assistant", reply);

            // İlk mesajsa başlığı otomatik ayarla
            if (dbMesajlar.Count <= 1)
            {
                _platformServisi.AiSohbetBasligiOtomatikAyarla(sohbetId, kullaniciId, message);
            }

            return Ok(new { reply, sohbetId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Bu aksiyon kullanıcının sohbet listesini döndürür.
    [HttpGet("Sohbetler")]
    public IActionResult Sohbetler()
    {
        var kullaniciId = KullaniciIdGetir();
        if (kullaniciId == 0) return Unauthorized();

        var sohbetler = _platformServisi.AiSohbetleriGetir(kullaniciId);
        var sonuc = sohbetler.Select(s => new
        {
            id = int.TryParse(s.GetValueOrDefault("Id"), out var id) ? id : 0,
            baslik = s.GetValueOrDefault("Baslik") ?? "Yeni Sohbet",
            tarih = s.GetValueOrDefault("SonGuncellemeTarihi") ?? ""
        }).ToList();

        return Json(sonuc);
    }

    // Bu aksiyon yeni sohbet oturumu oluşturur.
    [HttpPost("YeniSohbet")]
    [IgnoreAntiforgeryToken]
    public IActionResult YeniSohbet()
    {
        var kullaniciId = KullaniciIdGetir();
        if (kullaniciId == 0) return Unauthorized();

        var sohbetId = _platformServisi.AiSohbetOlustur(kullaniciId);
        return Json(new { sohbetId });
    }

    // Bu aksiyon belirli bir sohbetin mesajlarını döndürür.
    [HttpGet("Mesajlar/{sohbetId:int}")]
    public IActionResult Mesajlar(int sohbetId)
    {
        var kullaniciId = KullaniciIdGetir();
        if (kullaniciId == 0) return Unauthorized();

        if (!_platformServisi.AiSohbetSahibiMi(sohbetId, kullaniciId))
            return Forbid();

        var mesajlar = _platformServisi.AiMesajlariGetir(sohbetId);
        var sonuc = mesajlar.Select(m => new
        {
            role = m.GetValueOrDefault("Rol") ?? "user",
            content = m.GetValueOrDefault("Icerik") ?? ""
        }).ToList();

        return Json(sonuc);
    }

    // Bu aksiyon sohbet siler.
    [HttpPost("SohbetSil/{sohbetId:int}")]
    [IgnoreAntiforgeryToken]
    public IActionResult SohbetSil(int sohbetId)
    {
        var kullaniciId = KullaniciIdGetir();
        if (kullaniciId == 0) return Unauthorized();

        if (!_platformServisi.AiSohbetSahibiMi(sohbetId, kullaniciId))
            return Forbid();

        _platformServisi.AiSohbetSil(sohbetId, kullaniciId);
        return Ok(new { basarili = true });
    }

    private int KullaniciIdGetir()
    {
        var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimDegeri, out var id) ? id : 0;
    }
}
