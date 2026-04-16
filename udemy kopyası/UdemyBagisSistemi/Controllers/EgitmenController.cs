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

        // Bu satir kurs detay sayfasina geri doner.
        return RedirectToAction(nameof(KursDetay), new { kursId });
    }

    // Bu aksiyon egitmene ait kursun detay ve yonetim sayfasini gosterir.
    public IActionResult KursDetay(int kursId)
    {
        var egitmenId = KullaniciIdGetir();
        var kurs = _platformServisi.KursGetir(kursId, egitmenId);
        if (kurs is null) return RedirectToAction(nameof(Panel));

        var bolumler   = _platformServisi.KursBolumleriniGetir(kursId);
        var kategoriler = _platformServisi.KategorileriGetir();

        ViewBag.Kurs       = kurs;
        ViewBag.Bolumler   = bolumler;
        ViewBag.Kategoriler = kategoriler;
        return View();
    }

    // Bu aksiyon kurs bilgilerini gunceller (KursDetay sayfasindan POST).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KursGuncelle(
        int kursId,
        int kategoriId,
        string baslik,
        string aciklama,
        decimal fiyat,
        bool yayinlandiMi,
        IFormFile? onizlemeVideoDosyasi,
        IFormFile? dokumanDosyasi)
    {
        var egitmenId = KullaniciIdGetir();
        try
        {
            var mevcutKurs = _platformServisi.KursGetir(kursId, egitmenId);
            if (mevcutKurs is null) return RedirectToAction(nameof(Panel));

            var onizlemeUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(onizlemeVideoDosyasi, "onizleme-videolari");
            var dokumanUrl  = await _dosyaYuklemeServisi.DosyaKaydetAsync(dokumanDosyasi, "dokumanlar");

            var guncelKurs = new Kurs
            {
                Id              = kursId,
                EgitmenId       = egitmenId,
                KategoriId      = kategoriId,
                Baslik          = baslik,
                Aciklama        = aciklama,
                Fiyat           = fiyat,
                YayinlandiMi    = yayinlandiMi,
                OnizlemeVideoUrl = string.IsNullOrWhiteSpace(onizlemeUrl) ? mevcutKurs.OnizlemeVideoUrl : onizlemeUrl,
                DokumanUrl      = string.IsNullOrWhiteSpace(dokumanUrl)   ? mevcutKurs.DokumanUrl       : dokumanUrl
            };

            _platformServisi.KursKaydet(guncelKurs);
            TempData["Mesaj"] = "Kurs başarıyla güncellendi.";
        }
        catch (Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }
        return RedirectToAction(nameof(KursDetay), new { kursId });
    }

    // Bu aksiyon yeni bolum ekler (KursDetay sayfasindan POST).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumEkleDetay(int kursId, string baslik, string aciklama, IFormFile? videoDosyasi, IFormFile? dokumanDosyasi)
    {
        try
        {
            var videoUrl   = await _dosyaYuklemeServisi.DosyaKaydetAsync(videoDosyasi, "kurs-videolari");
            var dokumanUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(dokumanDosyasi, "bolum-dokumanlari");
            _platformServisi.KursBolumuEkle(kursId, KullaniciIdGetir(), baslik, aciklama, videoUrl, dokumanUrl);
            TempData["Mesaj"] = "Yeni bölüm eklendi.";
        }
        catch (Exception hata) { TempData["Hata"] = hata.Message; }
        return RedirectToAction(nameof(KursDetay), new { kursId });
    }

    // Bu aksiyon mevcut bolumu gunceller (KursDetay sayfasindan POST).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumGuncelle(int bolumId, int kursId, string baslik, string aciklama, IFormFile? videoDosyasi, IFormFile? dokumanDosyasi)
    {
        try
        {
            var videoUrl   = await _dosyaYuklemeServisi.DosyaKaydetAsync(videoDosyasi, "kurs-videolari");
            var dokumanUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(dokumanDosyasi, "bolum-dokumanlari");
            _platformServisi.BolumGuncelle(bolumId, KullaniciIdGetir(), baslik, aciklama, videoUrl, dokumanUrl);
            TempData["Mesaj"] = "Bölüm güncellendi.";
        }
        catch (Exception hata) { TempData["Hata"] = hata.Message; }
        return RedirectToAction(nameof(KursDetay), new { kursId });
    }

    // Bu aksiyon bir bolumu siler (KursDetay sayfasindan POST).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BolumSil(int bolumId, int kursId)
    {
        try
        {
            _platformServisi.BolumSil(bolumId, KullaniciIdGetir());
            TempData["Mesaj"] = "Bölüm silindi.";
        }
        catch (Exception hata) { TempData["Hata"] = hata.Message; }
        return RedirectToAction(nameof(KursDetay), new { kursId });
    }

    // Bu aksiyon kursu tamamen siler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KursSil(int kursId)
    {
        try
        {
            _platformServisi.KursSil(kursId, KullaniciIdGetir());
            TempData["Mesaj"] = "Kurs silindi.";
        }
        catch (Exception hata) { TempData["Hata"] = hata.Message; }
        return RedirectToAction(nameof(Panel));
    }

    // Bu aksiyon eğitmen profil bilgilerini günceller.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProfilGuncelle(
        string adSoyad,
        string eposta,
        string unvan,
        string hakkinda,
        int deneyimYili,
        string uzmanlikAlanlari,
        string linkedinProfili,
        string kursFormati,
        string fiyatlandirmaTercihi,
        IFormFile? yeniProfilFoto,
        string? mevcutProfilFotoUrl,
        string? yeniSifre)
    {
        try
        {
            var egitmenId = KullaniciIdGetir();
            
            // Eğer yeni şifre girilmişse hashle, aksi halde null bırak.
            string? sifreHash = null;
            if (!string.IsNullOrWhiteSpace(yeniSifre))
            {
                // Controller seviyesinde dependency injection üzerinden SifrelemeServisi alinabilir
                // veya manuel olarak instance olusturulabilir eger DI eksikse. Normalde DI yapisi kullanilmistir.
                var sifreleyici = new UdemyBagisSistemi.Servisler.SifrelemeServisi();
                sifreHash = sifreleyici.HashOlustur(yeniSifre);
            }

            // Yeni foto yüklendiyse onu al, yoksa mevcut olanı kullan
            var fotoUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(yeniProfilFoto, "profil-fotograflari");
            if (string.IsNullOrWhiteSpace(fotoUrl)) 
            {
                fotoUrl = mevcutProfilFotoUrl ?? string.Empty;
            }

            _platformServisi.EgitmenProfilGuncelle(
                egitmenId, 
                adSoyad,
                eposta,
                unvan, 
                hakkinda, 
                fotoUrl, 
                deneyimYili, 
                uzmanlikAlanlari, 
                linkedinProfili, 
                kursFormati, 
                fiyatlandirmaTercihi,
                sifreHash
            );

            TempData["Mesaj"] = "Profiliniz başarıyla güncellendi.";
        }
        catch (Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }

        // Profil sekmesine (tab-profil) direkt dönmek yerine normal panele dön,
        // kullanıcı tıkladığında JS ile ayarlanabilir ama şimdilik standart panel yeterlidir.
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
