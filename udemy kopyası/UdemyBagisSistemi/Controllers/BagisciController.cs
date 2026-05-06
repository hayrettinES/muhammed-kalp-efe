using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Servisler;

namespace UdemyBagisSistemi.Controllers;

// Bu denetleyici bagis islemlerini yonetir.
public class BagisciController : Controller
{
    private readonly PlatformServisi _platformServisi;

    public BagisciController(PlatformServisi platformServisi)
    {
        _platformServisi = platformServisi;
    }

    // Bu aksiyon bagis sayfasini dondurur.
    public IActionResult BagisPanel()
    {
        return View();
    }

    // Bu aksiyon kullanicidan alinan bagisi veritabanina ekler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BagisYap(string bagisciAdSoyad, decimal tutar)
    {
        try
        {
            _platformServisi.BagisYap(bagisciAdSoyad, tutar);
            TempData["BagisBasarili"] = "Bağışınız başarıyla alınmıştır. Yönetici onayından sonra ana sayfaya yansıyacaktır. Teşekkür ederiz!";
        }
        catch (System.Exception ex)
        {
            TempData["BagisHata"] = ex.Message;
        }

        return RedirectToAction("BagisPanel");
    }
}
