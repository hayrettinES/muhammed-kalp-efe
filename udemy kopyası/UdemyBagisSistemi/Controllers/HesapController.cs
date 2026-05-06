// Bu dosya giris, cikis ve kayit islemlerini yoneten denetleyiciyi tanimlar.
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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

    // Bu aksiyon admin giriş ekranını gösterir.
    [HttpGet]
    public IActionResult AdminGiris()
    {
        return View(new GirisViewModel());
    }

    // Bu aksiyon admin giriş formunu işler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminGiris(GirisViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var kullanici = _platformServisi.GirisYap(model.Eposta, model.Sifre);
        if (kullanici is null)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        if (kullanici.Rol != "Admin")
        {
            ModelState.AddModelError(string.Empty, "Bu giriş yalnızca yöneticiler içindir.");
            return View(model);
        }

        var claimler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
            new(ClaimTypes.Name, kullanici.AdSoyad),
            new(ClaimTypes.Email, kullanici.Eposta),
            new(ClaimTypes.Role, kullanici.Rol)
        };

        var kimlik = new ClaimsIdentity(claimler, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(kimlik));

        return RedirectToAction("Panel", "Admin");
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

        // Bu satir kullanicinin epostasini dogrulayip dogrulamadigini kontrol eder.
        if (!kullanici.EpostaOnaylandiMi)
        {
            ModelState.AddModelError(string.Empty, "Lütfen sisteme giriş yapabilmek için e-posta adresinizi doğrulayın.");
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

        // Bu satir profil bilgileri eksikse tamamlama ekranina yonlendirir.
        if (_platformServisi.ProfilEksikMi(kullanici))
        {
            return RedirectToAction(nameof(ProfilTamamla));
        }

        // Bu satir rola gore ilgili panele yonlendirir.
        return kullanici.Rol switch
        {
            "Admin" => RedirectToAction("Panel", "Admin"),
            "Egitmen" => RedirectToAction("Panel", "Egitmen"),
            _ => RedirectToAction("Panel", "Ogrenci")
        };
    }

    // Bu aksiyon ogrenci kayit ekranini gosterir.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult KayitOgrenci()
    {
        // Bu satir ogrenci kayit sayfasini gosterir.
        return View("~/Views/Ogrenci/kayitOgrenci.cshtml");
    }

    // Bu aksiyon egitmen kayit ekranini gosterir.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult KayitEgitmen()
    {
        // Bu satir egitmen kayit sayfasini gosterir.
        return View("~/Views/Egitmen/kayitEgitmen.cshtml");
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
    public async Task<IActionResult> Kayit(KayitViewModel model)
    {
        // Bu alan uygun kayit sayfasina yonlendirme icin rolu hazirlar.
        var kayitSayfasi = model.Rol == "Egitmen"
            ? nameof(KayitEgitmen)
            : nameof(KayitOgrenci);

        // Bu satir dogrulama hatasi varsa uygun kayit sayfasina doner.
        if (!ModelState.IsValid)
        {
            TempData["Hata"] = "Lütfen tüm zorunlu alanlari doldurunuz.";
            return RedirectToAction(kayitSayfasi);
        }

        // Bu satir kayit islemini asenkron calistirir.
        var sonuc = await _platformServisi.KayitOlAsync(model);

        // Bu satir sonuc basarisizsa hata mesajiyla kayit sayfasina doner.
        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Mesaj;
            return RedirectToAction(kayitSayfasi);
        }

        // Bu satir basarili kayit mesaji saklar.
        TempData["Mesaj"] = sonuc.Mesaj;

        // Bu satir kullaniciyi e-posta dogrulama mesajiyla giris ekranina yonlendirir.
        return RedirectToAction(nameof(Giris));
    }

    // Bu aksiyon e-posta dogrulama baglantisini isler.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Dogrula(string eposta, string kod)
    {
        var sonuc = _platformServisi.EpostaDogrula(eposta, kod);

        if (sonuc.Basarili)
        {
            TempData["Mesaj"] = sonuc.Mesaj;
        }
        else
        {
            TempData["Hata"] = sonuc.Mesaj;
        }

        return RedirectToAction(nameof(Giris));
    }

    // Bu aksiyon Google hesaplari ile giris surecini baslatir.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GoogleLogin(string rol = "Ogrenci")
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleResponse), new { rol = rol }) };
        return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
    }

    // Bu aksiyon Google'dan donen kimlik bilgisini yakalar ve isler.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleResponse(string rol)
    {
        // Bu satir Google'dan gelen external authentication sonucunu okur.
        var result = await HttpContext.AuthenticateAsync("Google");
        if (!result.Succeeded)
        {
            TempData["Hata"] = "Google ile giriş başarısız oldu.";
            return RedirectToAction(nameof(Giris));
        }

        var claims = result.Principal?.Identities.FirstOrDefault()?.Claims.ToList();
        var email = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        var name = claims?.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            TempData["Hata"] = "Google hesabınızdan e-posta adresi okunamadı.";
            return RedirectToAction(nameof(Giris));
        }

        var kullanici = _platformServisi.KullaniciGetir(email);
        if (kullanici == null)
        {
            // Kullanici yoksa hemen google uzerinden kayit edelim.
            var model = new KayitViewModel
            {
                AdSoyad = name ?? "Google Kullanıcısı",
                Eposta = email,
                Sifre = Guid.NewGuid().ToString(), // Rastgele guvenli sifre
                Rol = string.IsNullOrEmpty(rol) ? "Ogrenci" : rol
            };
            await _platformServisi.KayitOlAsync(model);

            // Mail onayini bypass edelim cunku Google'dan geldi.
            _platformServisi.EpostaDogrulaByGoogle(email);
            kullanici = _platformServisi.KullaniciGetir(email);
        }
        else
        {
            // Kullanici var ama maili henuz onaylamamissa Google uzerinden girdigi icin onayliyalim.
            if (!kullanici.EpostaOnaylandiMi)
            {
                _platformServisi.EpostaDogrulaByGoogle(email);
                kullanici = _platformServisi.KullaniciGetir(email);
            }
        }

        // Kimligi cookie'ye basalim.
        var claimler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici!.Id.ToString()),
            new(ClaimTypes.Name, kullanici.AdSoyad),
            new(ClaimTypes.Email, kullanici.Eposta),
            new(ClaimTypes.Role, kullanici.Rol)
        };
        var kimlik = new ClaimsIdentity(claimler, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(kimlik));

        // Bu satir profil bilgileri eksikse tamamlama ekranina yonlendirir.
        if (_platformServisi.ProfilEksikMi(kullanici))
        {
            return RedirectToAction(nameof(ProfilTamamla));
        }

        TempData["Mesaj"] = $"Hoş geldiniz, {kullanici.AdSoyad}!";
        return kullanici.Rol switch
        {
            "Admin" => RedirectToAction("Panel", "Admin"),
            "Egitmen" => RedirectToAction("Panel", "Egitmen"),
            _ => RedirectToAction("Panel", "Ogrenci")
        };
    }

    // Bu aksiyon profil tamamlama ekranini gosterir.
    [HttpGet]
    [Authorize]
    public IActionResult ProfilTamamla()
    {
        // Bu satir mevcut kullanicinin rolunu claim'den alir.
        var rol = User.FindFirstValue(ClaimTypes.Role) ?? "Ogrenci";

        // Bu satir kullanici zaten tam profilse ilgili panele yonlendirir.
        var kullaniciId = KullaniciIdGetir();
        var kullanici = _platformServisi.KullaniciGetir(kullaniciId);
        if (kullanici != null && !_platformServisi.ProfilEksikMi(kullanici))
        {
            return kullanici.Rol switch
            {
                "Egitmen" => RedirectToAction("Panel", "Egitmen"),
                _ => RedirectToAction("Panel", "Ogrenci")
            };
        }

        // Bu satir bos profil tamamlama modelini gorunume yollar.
        return View(new ProfilTamamlaViewModel { Rol = rol });
    }

    // Bu aksiyon profil tamamlama formunu isler.
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult ProfilTamamla(ProfilTamamlaViewModel model)
    {
        // Bu satir aktif kullanici kimligini alir.
        var kullaniciId = KullaniciIdGetir();

        // Bu satir profil bilgilerini veritabanina kaydeder.
        _platformServisi.ProfilTamamla(kullaniciId, model);

        TempData["Mesaj"] = "Profiliniz başarıyla tamamlandı. Hoş geldiniz!";

        // Bu satir role gore panele yonlendirir.
        return model.Rol switch
        {
            "Egitmen" => RedirectToAction("Panel", "Egitmen"),
            _ => RedirectToAction("Panel", "Ogrenci")
        };
    }

    // Bu aksiyon kullaniciyi cikis yaptirir.
    public async Task<IActionResult> Cikis()
    {
        // Bu satir cookie oturumunu temizler.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Bu satir anasayfaya doner.
        return RedirectToAction("Index", "Home");
    }

    // Bu yardimci metod claim icinden kullanici kimligini ceker.
    private int KullaniciIdGetir()
    {
        var claimDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimDegeri, out var id) ? id : 0;
    }
}
