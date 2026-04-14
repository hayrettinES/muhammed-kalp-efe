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
    public IActionResult Panel(int? kategoriId)
    {
        // Bu satir panel verisini gorunume yollar.
        return View(_platformServisi.AdminPanelVerisiniGetir(kategoriId));
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
        return RedirectToAction(nameof(Panel));
    }
}
