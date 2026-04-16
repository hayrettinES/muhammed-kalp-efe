// Bu dosya ogrenci paneli islemlerini yoneten denetleyiciyi tanimlar.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Servisler;

namespace UdemyBagisSistemi.Controllers;

// Bu denetleyici sadece ogrenci rolu icin calisir.
[Authorize(Roles = "Ogrenci")]
public class OgrenciController : Controller
{
    // Bu alan platform servislerine erisim saglar.
    private readonly PlatformServisi _platformServisi;

    // Bu alan fotoğraf yükleme işlemleri için gereklidir.
    private readonly DosyaYuklemeServisi _dosyaYuklemeServisi;

    // Bu kurucu servis bagimliliklarini alir.
    public OgrenciController(PlatformServisi platformServisi, DosyaYuklemeServisi dosyaYuklemeServisi)
    {
        // Bu satir bagimliliklari saklar.
        _platformServisi = platformServisi;
        _dosyaYuklemeServisi = dosyaYuklemeServisi;
    }

    // Bu aksiyon ogrenci panelini gosterir.
    public IActionResult Panel()
    {
        // Bu satir aktif ogrenci kimligini claim uzerinden alir.
        var ogrenciId = KullaniciIdGetir();

        // Bu satir panel verisini gorunume yollar.
        return View(_platformServisi.OgrenciPanelVerisiniGetir(ogrenciId));
    }

    // Bu aksiyon ogrencinin tam ekran egitim izleme sayfasini gosterir.
    public IActionResult KursDetay(int kursId)
    {
        try
        {
            var ogrenciId = KullaniciIdGetir();
            var model = _platformServisi.OgrenciKursIzlemeVerisiniGetir(kursId, ogrenciId);
            return View(model);
        }
        catch(Exception hata)
        {
            TempData["Hata"] = hata.Message;
            return RedirectToAction(nameof(Panel));
        }
    }

    // Bu aksiyon bagis yapma islemini isler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BagisYap(string bagisciAdSoyad, decimal tutar)
    {
        try
        {
            // Bu satir bagis islemini yapar.
            _platformServisi.BagisYap(bagisciAdSoyad, tutar);
            TempData["Mesaj"] = "Bagis basariyla eklendi.";
        }
        catch (Exception hata)
        {
            // Bu satir is kurali hatasini saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir panele geri doner.
        return RedirectToAction(nameof(Panel));
    }

    // Bu aksiyon ogrencinin kendi hesabina bakiye yuklemesini saglar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BakiyeYukle(decimal tutar)
    {
        try
        {
            // Bu satir aktif ogrenci kimligini alir.
            var ogrenciId = KullaniciIdGetir();

            // Bu satir bakiye yukleme islemini yapar.
            _platformServisi.OgrenciBakiyesiYukle(ogrenciId, tutar);
            TempData["Mesaj"] = "Bakiyen basariyla yuklendi.";
        }
        catch (Exception hata)
        {
            // Bu satir hatayi saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir ogrenci paneline geri doner.
        return RedirectToAction(nameof(Panel));
    }

    // Bu aksiyon kurs satin alma islemini isler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KursAl(int kursId)
    {
        try
        {
            // Bu satir aktif ogrenci kimligini alir.
            var ogrenciId = KullaniciIdGetir();

            // Bu satir satin alma sonuc mesajini alir.
            TempData["Mesaj"] = _platformServisi.KursSatinAl(kursId, ogrenciId);
        }
        catch (Exception hata)
        {
            // Bu satir olusan hatayi saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir panele geri doner.
        return RedirectToAction(nameof(Panel));
    }

    // Bu aksiyon ogrencinin puan ve yorum girmesini saglar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult YorumYap(int kursId, int puan, string yorum)
    {
        try
        {
            // Bu satir aktif ogrenci kimligini alir.
            var ogrenciId = KullaniciIdGetir();

            // Bu satir yorum ve puan kaydini yapar.
            _platformServisi.KursYorumYap(kursId, ogrenciId, puan, yorum);
            TempData["Mesaj"] = "Puanin ve yorumun kaydedildi.";
        }
        catch (Exception hata)
        {
            // Bu satir olusan hatayi saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir kurs detay sayfasina geri doner.
        return RedirectToAction("KursDetay", "Home", new { kursId });
    }

    // Bu aksiyon ogrencinin video ilerlemesini kaydeder.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BolumDurumuKaydet(int kursId, int kursBolumuId, bool tamamlandiMi)
    {
        try
        {
            // Bu satir aktif ogrenci kimligini alir.
            var ogrenciId = KullaniciIdGetir();

            // Bu satir bolum ilerlemesini kaydeder.
            _platformServisi.BolumIlerlemesiniKaydet(kursId, kursBolumuId, ogrenciId, tamamlandiMi);
            TempData["Mesaj"] = tamamlandiMi ? "Bolum tamamlandi olarak isaretlendi." : "Bolum son izlenen yer olarak kaydedildi.";
        }
        catch (Exception hata)
        {
            // Bu satir olusan hatayi saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir kurs detayina geri doner.
        return RedirectToAction("KursDetay", "Home", new { kursId });
    }

    // Bu aksiyon ogrencinin kendi profilini guncellemesini saglar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProfilGuncelle(string adSoyad, string eposta, string egitimSeviyesi, string hedef, string ilgiAlanlari, IFormFile? profilFoto, string? mevcutFotoUrl)
    {
        try
        {
            var ogrenciId = KullaniciIdGetir();

            var fotoUrl = await _dosyaYuklemeServisi.DosyaKaydetAsync(profilFoto, "profil-fotograflari");
            var eklenecekFoto = string.IsNullOrWhiteSpace(fotoUrl) ? mevcutFotoUrl ?? "" : fotoUrl;

            // PlatformServisi.ProfilGuncelle, rol bazında özel alanları doğrudan desteklemiyor (sadece unvan/hakkinda güncelliyor).
            // Bu nedenle Ogrenci tablosu için özelleştirilmiş alanları PlatformServisi üzerinden veya direkt komut ile cậpnceliyoruz.
            // Ama önce standart ProfilGuncelle'yi çağıralım:
            _platformServisi.ProfilGuncelle(ogrenciId, adSoyad, eposta, "", "", eklenecekFoto);

            // Ogrenci özel alanları (EgitimSeviyesi, Hedef, IlgiAlanlari) için Sql güncellemesi işlemi:
            var model = new UdemyBagisSistemi.ViewModels.ProfilTamamlaViewModel 
            { 
                Rol = "Ogrenci",
                EgitimSeviyesi = egitimSeviyesi,
                Hedef = hedef,
                IlgiAlanlari = ilgiAlanlari
            };
            _platformServisi.ProfilTamamla(ogrenciId, model);

            TempData["Mesaj"] = "Profil bilgilerin güncellendi.";
        }
        catch (Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }

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
