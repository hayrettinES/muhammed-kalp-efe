// Bu dosya giris, cikis ve kayit islemlerini yoneten denetleyiciyi tanimlar.
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Servisler;
using UdemyBagisSistemi.ViewModels;

namespace UdemyBagisSistemi.Controllers;

// Bu denetleyici kullanici kimlik islemlerini yonetir.
public class HesapController : Controller
{
    // Bu alan platform servislerine erisim saglar.
    private readonly PlatformServisi _platformServisi;

    // Bu kurucu servis bagimliligini alir.
    public HesapController(PlatformServisi platformServisi)
    {
        // Bu satir bagimliligi saklar.
        _platformServisi = platformServisi;
    }

    // Bu aksiyon giris ekranini gosterir.
    [HttpGet]
    public IActionResult Giris()
    {
        // Bu satir bos form modelini gorunume yollar.
        return View(new GirisViewModel());
    }

    // Bu aksiyon giris formunu isler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Giris(GirisViewModel model)
    {
        // Bu satir dogrulama hatasi varsa ayni sayfaya dondurur.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Bu satir giris kontrolu yapar.
        var kullanici = _platformServisi.GirisYap(model.Eposta, model.Sifre);

        // Bu satir kullanici bulunamazsa hata gosterir.
        if (kullanici is null)
        {
            ModelState.AddModelError(string.Empty, "Eposta veya sifre hatali.");
            return View(model);
        }

        // Bu blok cookie kimligi icin gerekli claim listesini hazirlar.
        var claimler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new(ClaimTypes.Name, kullanici.AdSoyad),
            new(ClaimTypes.Email, kullanici.Eposta),
            new(ClaimTypes.Role, kullanici.Rol)
        };

        // Bu satir claims identity nesnesini olusturur.
        var kimlik = new ClaimsIdentity(claimler, CookieAuthenticationDefaults.AuthenticationScheme);

        // Bu satir principal nesnesini olusturur.
        var principal = new ClaimsPrincipal(kimlik);

        // Bu satir kullaniciyi sisteme cookie ile giris yaptirir.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Bu satir rola gore ilgili panele yonlendirir.
        return kullanici.Rol switch
        {
            "Admin" => RedirectToAction("Panel", "Admin"),
            "Egitmen" => RedirectToAction("Panel", "Egitmen"),
            _ => RedirectToAction("Panel", "Ogrenci")
        };
    }

    // Bu aksiyon kayit ekranini gosterir.
    [HttpGet]
    public IActionResult Kayit()
    {
        // Bu satir bos kayit modelini gorunume yollar.
        return View(new KayitViewModel());
    }

    // Bu aksiyon kayit formunu isler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Kayit(KayitViewModel model)
    {
        // Bu satir dogrulama hatasi varsa ayni sayfaya dondurur.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Bu satir kayit islemini calistirir.
        var sonuc = _platformServisi.KayitOl(model);

        // Bu satir sonuc basarisizsa ekrana mesaji basar.
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            return View(model);
        }

        // Bu satir basarili kayit mesaji saklar.
        TempData["Mesaj"] = sonuc.Mesaj;

        // Bu satir kullaniciyi giris ekranina yonlendirir.
        return RedirectToAction(nameof(Giris));
    }

    // Bu aksiyon kullaniciyi cikis yaptirir.
    public async Task<IActionResult> Cikis()
    {
        // Bu satir cookie oturumunu temizler.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Bu satir anasayfaya doner.
        return RedirectToAction("Index", "Home");
    }
}
