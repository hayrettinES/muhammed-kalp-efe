using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UdemyBagisSistemi.Servisler;

namespace UdemyBagisSistemi.Controllers;

[Authorize]
public class SepetController : Controller
{
    private readonly PlatformServisi _platformServisi;

    public SepetController(PlatformServisi platformServisi)
    {
        _platformServisi = platformServisi;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var kullaniciId = KullaniciIdGetir();
        var sepetViewModel = _platformServisi.SepetiGetir(kullaniciId);
        return View("~/Views/Ogrenci/Sepet.cshtml", sepetViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Ekle(int kursId, string? odemeYontemi)
    {
        try
        {
            var kullaniciId = KullaniciIdGetir();
            _platformServisi.SepeteEkle(kullaniciId, kursId, odemeYontemi ?? "Bakiye");
            TempData["Mesaj"] = "Kurs başarıyla sepete eklendi.";
        }
        catch(Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }
        string url = Request.Headers["Referer"].ToString();
        return Redirect(string.IsNullOrWhiteSpace(url) ? "/" : url);
    }

    // AJAX ile sepete ekleme
    [HttpPost]
    [Route("Sepet/EkleAjax")]
    public IActionResult EkleAjax([FromBody] SepetEkleRequest req)
    {
        try
        {
            var kullaniciId = KullaniciIdGetir();
            _platformServisi.SepeteEkle(kullaniciId, req.KursId, req.OdemeYontemi ?? "Bakiye");
            var sayi = _platformServisi.SepetUrunSayisi(kullaniciId);
            return Json(new { basarili = true, mesaj = "Kurs sepete eklendi!", sayi });
        }
        catch (Exception hata)
        {
            return Json(new { basarili = false, mesaj = hata.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cikar(int kursId)
    {
        var kullaniciId = KullaniciIdGetir();
        _platformServisi.SepettenCikar(kullaniciId, kursId);
        TempData["Mesaj"] = "Kurs sepetten çıkarıldı.";
        return RedirectToAction(nameof(Index));
    }

    // AJAX ile sepetten çıkarma
    [HttpPost]
    [Route("Sepet/CikarAjax")]
    public IActionResult CikarAjax([FromBody] SepetCikarRequest req)
    {
        try
        {
            var kullaniciId = KullaniciIdGetir();
            _platformServisi.SepettenCikar(kullaniciId, req.KursId);
            var sayi = _platformServisi.SepetUrunSayisi(kullaniciId);
            return Json(new { basarili = true, mesaj = "Kurs sepetten çıkarıldı.", sayi });
        }
        catch (Exception hata)
        {
            return Json(new { basarili = false, mesaj = hata.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Onayla()
    {
        try
        {
            var kullaniciId = KullaniciIdGetir();
            TempData["Mesaj"] = _platformServisi.SepetiSatinAl(kullaniciId);
            TempData["SepetOnay"] = "true"; // Eğitimlerim sekmesine yönlendirme işareti
        }
        catch (Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }
        return RedirectToAction("Panel", "Ogrenci");
    }

    // AJAX ile satın alma
    [HttpPost]
    [Route("Sepet/OnaylaAjax")]
    public IActionResult OnaylaAjax()
    {
        try
        {
            var kullaniciId = KullaniciIdGetir();
            var mesaj = _platformServisi.SepetiSatinAl(kullaniciId);
            return Json(new { basarili = true, mesaj, yonlendir = "tab-kurslarim" });
        }
        catch (Exception hata)
        {
            return Json(new { basarili = false, mesaj = hata.Message });
        }
    }

    [HttpGet]
    public IActionResult Sayi()
    {
        var kullaniciId = KullaniciIdGetir();
        int sayi = _platformServisi.SepetUrunSayisi(kullaniciId);
        return Json(new { sayi });
    }

    private int KullaniciIdGetir()
    {
        var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimDegeri, out var id) ? id : 0;
    }
}

// DTO'lar AJAX istekleri için
public class SepetEkleRequest
{
    public int KursId { get; set; }
    public string? OdemeYontemi { get; set; }
}

public class SepetCikarRequest
{
    public int KursId { get; set; }
}
