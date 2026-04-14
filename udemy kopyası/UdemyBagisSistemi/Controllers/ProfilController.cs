// Bu dosya kullanici profil islemlerini yoneten denetleyiciyi tanimlar.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Servisler;

namespace UdemyBagisSistemi.Controllers;

// Bu denetleyici tum giris yapmis kullanicilar icin calisir.
[Authorize]
public class ProfilController : Controller
{
    // Bu alan platform servislerini tutar.
    private readonly PlatformServisi _platformServisi;

    // Bu alan profil resmi yukleme servisini tutar.
    private readonly DosyaYuklemeServisi _dosyaYuklemeServisi;

    // Bu kurucu bagimliliklari alir.
    public ProfilController(PlatformServisi platformServisi, DosyaYuklemeServisi dosyaYuklemeServisi)
    {
        // Bu satir servisleri saklar.
        _platformServisi = platformServisi;
        _dosyaYuklemeServisi = dosyaYuklemeServisi;
    }

    // Bu aksiyon profil ekranini gosterir.
    [HttpGet]
    public IActionResult Index()
    {
        // Bu satir aktif kullanici kimligini alir.
        var kullaniciId = KullaniciIdGetir();

        // Bu satir profil modelini gorunume yollar.
        return View(_platformServisi.ProfilGetir(kullaniciId));
    }

    // Bu aksiyon profil formunu kaydeder.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(string adSoyad, string eposta, string unvan, string hakkinda, IFormFile? profilFotoDosyasi, string? mevcutProfilFotoUrl)
    {
        try
        {
            // Bu satir varsa yeni profil fotografini kaydeder.
            var profilFotoUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(profilFotoDosyasi, "profil-fotograflari");

            // Bu satir profil bilgisini gunceller.
            _platformServisi.ProfilGuncelle(
                KullaniciIdGetir(),
                adSoyad,
                eposta,
                unvan,
                hakkinda,
                string.IsNullOrWhiteSpace(profilFotoUrl) ? mevcutProfilFotoUrl ?? string.Empty : profilFotoUrl);

            // Bu satir bilgi mesaji saklar.
            TempData["Mesaj"] = "Profil bilgilerin guncellendi.";
        }
        catch (Exception hata)
        {
            // Bu satir hata mesaji saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir profil ekranina geri doner.
        return RedirectToAction(nameof(Index));
    }

    // Bu yardimci metod aktif kullanici kimligini ceker.
    private int KullaniciIdGetir()
    {
        // Bu satir claim degerini bulur.
        var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Bu satir claim sayiya cevrilemezse hata verir.
        return int.TryParse(claimDegeri, out var id) ? id : 0;
    }
}

// Bu dosya yorum yaniti islemlerini yoneten denetleyiciyi tanimlar.
[Authorize]
public class YorumController : Controller
{
    // Bu alan platform servislerini tutar.
    private readonly PlatformServisi _platformServisi;

    // Bu kurucu bagimliligi alir.
    public YorumController(PlatformServisi platformServisi)
    {
        // Bu satir servisi saklar.
        _platformServisi = platformServisi;
    }

    // Bu aksiyon bir yoruma yanit ekler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult YanitEkle(int kursId, int kursYorumuId, string yanit)
    {
        try
        {
            // Bu satir aktif kullanici kimligini bulur.
            var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var kullaniciId = int.TryParse(claimDegeri, out var id) ? id : 0;

            // Bu satir yaniti kaydeder.
            _platformServisi.YorumYanitiEkle(kursYorumuId, kullaniciId, yanit);
            TempData["Mesaj"] = "Yanitin eklendi.";
        }
        catch (Exception hata)
        {
            // Bu satir olusan hatayi saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir kurs detay ekranina geri doner.
        return RedirectToAction("KursDetay", "Home", new { kursId });
    }
}
