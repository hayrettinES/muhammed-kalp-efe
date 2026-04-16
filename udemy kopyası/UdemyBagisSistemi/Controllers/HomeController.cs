// Bu dosya genel ziyaretci sayfalarini yoneten denetleyiciyi tanimlar.
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Models;
using UdemyBagisSistemi.Servisler;

namespace UdemyBagisSistemi.Controllers;

// Bu denetleyici anasayfa ve hata ekranlarindan sorumludur.
public class HomeController : Controller
{
    // Bu alan platform servislerine erisim saglar.
    private readonly PlatformServisi _platformServisi;

    // Bu kurucu gerekli servis bagimliligini alir.
    public HomeController(PlatformServisi platformServisi)
    {
        // Bu satir bagimliligi saklar.
        _platformServisi = platformServisi;
    }

    // Bu aksiyon anasayfayi gosterir.
    public IActionResult Index()
    {
        // View klasöründeki basic altındaki index gösterilir.
        return View("~/Views/basic/index.cshtml");
    }

    // Bu aksiyon herkese acik bagis sayfasini gosterir.
    [HttpGet]
    public IActionResult Bagis()
    {
        // Bu satir bagis sayfa modelini gorunume yollar.
        return View(_platformServisi.BagisSayfasiVerisiniGetir());
    }

    // Bu aksiyon herkese acik bagis formunu isler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Bagis(string bagisciAdSoyad, decimal tutar)
    {
        try
        {
            // Bu satir bagis islemini yapar.
            _platformServisi.BagisYap(bagisciAdSoyad, tutar);
            TempData["Mesaj"] = "Bagisin icin tesekkurler.";
        }
        catch (Exception hata)
        {
            // Bu satir olusan hatayi saklar.
            TempData["Hata"] = hata.Message;
        }

        // Bu satir ayni bagis sayfasina geri doner.
        return RedirectToAction(nameof(Bagis));
    }

    // Bu aksiyon herkese acik kurs detay ekranini gosterir.
    [HttpGet]
    public IActionResult KursDetay(int kursId)
    {
        try
        {
            // Bu satir varsa aktif kullanici kimligini alir.
            var aktifKullaniciId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var kullaniciId)
                ? kullaniciId
                : (int?)null;

            // Bu satir kurs detay modelini gorunume yollar.
            return View(_platformServisi.KursDetayiGetir(kursId, aktifKullaniciId));
        }
        catch (Exception hata)
        {
            // Bu satir hatayi ekranda gostermek uzere saklar.
            TempData["Hata"] = hata.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // Bu aksiyon hata ekranini gosterir.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Bu satir hata modelini gorunume gonderir.
        return View(new HataGorunumModeli { IstekKimligi = HttpContext.TraceIdentifier });
    }
}
