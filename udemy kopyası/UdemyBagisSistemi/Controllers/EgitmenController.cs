// Bu dosya egitmen paneli islemlerini yoneten denetleyiciyi tanimlar.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Models;
using UdemyBagisSistemi.Servisler;

namespace UdemyBagisSistemi.Controllers;

// Bu denetleyici sadece egitmen rolu icin calisir.
[Authorize(Roles = "Egitmen")]
public class EgitmenController : Controller
{
    // Bu alan platform servislerine erisim saglar.
    private readonly PlatformServisi _platformServisi;

    // Bu alan yuklenen dosyalari kaydeden servise erisim saglar.
    private readonly DosyaYuklemeServisi _dosyaYuklemeServisi;

    // Bu kurucu servis bagimliligini alir.
    public EgitmenController(PlatformServisi platformServisi, DosyaYuklemeServisi dosyaYuklemeServisi)
    {
        // Bu satir bagimliligi saklar.
        _platformServisi = platformServisi;

        // Bu satir dosya yukleme servisini saklar.
        _dosyaYuklemeServisi = dosyaYuklemeServisi;
    }

    // Bu aksiyon egitmen panelini gosterir.
    public IActionResult Panel(int? kursId)
    {
        // Bu satir aktif egitmenin kimligini claim uzerinden alir.
        var egitmenId = KullaniciIdGetir();

        // Bu satir panel verisini gorunume yollar.
        return View(_platformServisi.EgitmenPanelVerisiniGetir(egitmenId, kursId));
    }

    // Bu aksiyon kurs ekleme veya guncelleme yapar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KursKaydet(
        int id,
        int kategoriId,
        string baslik,
        string aciklama,
        decimal fiyat,
        bool yayinlandiMi,
        IFormFile? onizlemeVideoDosyasi,
        IFormFile? dokumanDosyasi,
        string? mevcutOnizlemeVideoUrl,
        string? mevcutDokumanUrl)
    {
        try
        {
            // Bu satir aktif egitmen kimligini alir.
            var egitmenId = KullaniciIdGetir();

            // Bu satir yeni onizleme videosunu varsa diske kaydeder.
            var onizlemeVideoUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(onizlemeVideoDosyasi, "onizleme-videolari");

            // Bu satir yeni dokuman dosyasini varsa diske kaydeder.
            var dokumanUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(dokumanDosyasi, "dokumanlar");

            // Bu satir kaydedilecek kurs modelini olusturur.
            var kurs = new Kurs
            {
                Id = id,
                EgitmenId = egitmenId,
                KategoriId = kategoriId,
                Baslik = baslik,
                Aciklama = aciklama,
                Fiyat = fiyat,
                YayinlandiMi = yayinlandiMi,
                OnizlemeVideoUrl = string.IsNullOrWhiteSpace(onizlemeVideoUrl) ? mevcutOnizlemeVideoUrl ?? string.Empty : onizlemeVideoUrl,
                DokumanUrl = string.IsNullOrWhiteSpace(dokumanUrl) ? mevcutDokumanUrl ?? string.Empty : dokumanUrl
            };

            // Bu satir kurs kaydetme islemini yapar.
            var kursId = _platformServisi.KursKaydet(kurs);
            TempData["Mesaj"] = kurs.Id == 0 ? "Kurs eklendi." : "Kurs guncellendi.";

            // Bu satir yeni olusan veya guncellenen kurs sayfasina doner.
            return RedirectToAction(nameof(Panel), new { kursId });
        }
        catch (Exception hata)
        {
            // Bu satir olusan hatayi ekranda gostermek uzere saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir panele geri doner.
        return RedirectToAction(nameof(Panel));
    }

    // Bu aksiyon kursa yeni video bolumu ekler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumEkle(int kursId, string baslik, string aciklama, IFormFile? videoDosyasi, IFormFile? dokumanDosyasi)
    {
        try
        {
            // Bu satir bolum videosunu diske kaydeder.
            var videoUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(videoDosyasi, "kurs-videolari");

            // Bu satir bolum dokumanini diske kaydeder.
            var dokumanUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(dokumanDosyasi, "bolum-dokumanlari");

            // Bu satir bolum ekleme islemini yapar.
            _platformServisi.KursBolumuEkle(kursId, KullaniciIdGetir(), baslik, aciklama, videoUrl, dokumanUrl);
            TempData["Mesaj"] = "Yeni video bolumu eklendi.";
        }
        catch (Exception hata)
        {
            // Bu satir olusan hatayi ekranda gostermek uzere saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir secili kursla birlikte panele geri doner.
        return RedirectToAction(nameof(Panel), new { kursId });
    }

    // Bu aksiyon kurs yayin durumunu degistirir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult YayinDurumuDegistir(int kursId)
    {
        // Bu satir aktif egitmen kimligini alir.
        var egitmenId = KullaniciIdGetir();

        // Bu satir yayin durumunu tersine cevirir.
        _platformServisi.KursYayinDurumuDegistir(kursId, egitmenId);
        TempData["Mesaj"] = "Kurs yayin durumu guncellendi.";

        // Bu satir panele geri doner.
        return RedirectToAction(nameof(Panel));
    }

    // Bu yardimci metod claim icinden kullanici kimligini ceker.
    private int KullaniciIdGetir()
    {
        // Bu satir claim degerini bulur.
        var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Bu satir claim bulunamazsa hata verir.
        return int.TryParse(claimDegeri, out var id) ? id : 0;
    }
}
