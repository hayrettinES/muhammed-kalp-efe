// Bu dosya admin paneli islemlerini yoneten denetleyiciyi tanimlar.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Servisler;

namespace UdemyBagisSistemi.Controllers;

// Bu denetleyici sadece admin rolu icin calisir.
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // Bu alan platform servislerine erisim saglar.
    private readonly PlatformServisi _platformServisi;

    // Bu kurucu servis bagimliligini alir.
    public AdminController(PlatformServisi platformServisi)
    {
        // Bu satir bagimliligi saklar.
        _platformServisi = platformServisi;
    }

    // Bu aksiyon admin panelini gosterir.
    public IActionResult Panel(int? kategoriId, string arama = "")
    {
        // Bu satir panel verisini gorunume yollar.
        return View(_platformServisi.AdminPanelVerisiniGetir(kategoriId, arama));
    }

    // Bu aksiyon yeni kategori ekler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KategoriEkle(string ad, string aciklama)
    {
        // Bu satir yeni kategori olusturur.
        _platformServisi.KategoriEkle(ad, aciklama);

        // Bu satir bilgi mesaji ayarlar.
        TempData["Mesaj"] = "Kategori eklendi.";

        // Bu satir panele geri doner.
        return RedirectToAction(nameof(Panel));
    }

    // Bu aksiyon secili kategoriyi gunceller.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KategoriGuncelle(int id, string ad, string aciklama)
    {
        // Bu satir kategori guncellemesini yapar.
        _platformServisi.KategoriGuncelle(id, ad, aciklama);

        // Bu satir bilgi mesaji ayarlar.
        TempData["Mesaj"] = "Kategori guncellendi.";

        // Bu satir panele geri doner.
        return RedirectToAction(nameof(Panel));
    }

    // Bu aksiyon kategori siler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KategoriSil(int id)
    {
        try
        {
            // Bu satir kategori silme islemini dener.
            _platformServisi.KategoriSil(id);
            TempData["Mesaj"] = "Kategori silindi.";
        }
        catch (Exception hata)
        {
            // Bu satir olusan is kurali hatasini saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir panele geri doner.
        return Redirect($"{Url.Action(nameof(Panel))}#tab-kat");
    }

    // Bu aksiyon kullanıcıyı siler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KullaniciSil(int id)
    {
        try
        {
            _platformServisi.KullaniciSil(id);
            TempData["Mesaj"] = "Kullanıcı başarıyla silindi.";
        }
        catch (Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }
        return Redirect($"{Url.Action(nameof(Panel))}#tab-users");
    }

    // Bu aksiyon kursun yayın durumunu değiştirir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KursYayinToggle(int kursId)
    {
        try
        {
            _platformServisi.KursYayinDurumDegistir(kursId);
            TempData["Mesaj"] = "Kurs yayın durumu güncellendi.";
        }
        catch (Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }
        return Redirect($"{Url.Action(nameof(Panel))}#tab-kurs");
    }
    // Bu aksiyon bağışı onaylar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BagisOnayla(int bagisId)
    {
        try
        {
            _platformServisi.BagisOnayla(bagisId);
            TempData["Mesaj"] = "Bağış başarıyla onaylandı ve havuza eklendi.";
        }
        catch (Exception hata)
        {
            TempData["Hata"] = hata.Message;
        }
        return Redirect($"{Url.Action(nameof(Panel))}#tab-bagis");
    }
}
