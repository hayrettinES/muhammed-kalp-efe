// Bu dosya uygulamanin baslangic noktasidir.
using Microsoft.AspNetCore.Authentication.Cookies;
using UdemyBagisSistemi.Servisler;

// Bu satir web uygulamasi kurucusunu olusturur.
var builder = WebApplication.CreateBuilder(args);

// Bu satir MVC servislerini ekler.
builder.Services.AddControllersWithViews();

// Bu satir cookie tabanli kimlik dogrulamayi etkinlestirir.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(secenekler =>
    {
        secenekler.LoginPath = "/Hesap/Giris";
        secenekler.AccessDeniedPath = "/Hesap/Giris";
    })
    .AddGoogle(secenekler =>
    {
        var googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        secenekler.ClientId = googleAuthNSection["ClientId"]!;
        secenekler.ClientSecret = googleAuthNSection["ClientSecret"]!;
        secenekler.CallbackPath = "/signin-google";
    });

// Bu satir uygulama servislerini dependency injection sistemine ekler.
builder.Services.AddSingleton<SifrelemeServisi>();
builder.Services.AddSingleton<SqliteKomutServisi>();
builder.Services.AddSingleton<VeritabaniHazirlayici>();
builder.Services.AddScoped<IEpostaServisi, SmtpEpostaServisi>();
builder.Services.AddScoped<DosyaYuklemeServisi>();
builder.Services.AddScoped<PlatformServisi>();

// Bu satir web uygulamasini olusturur.
var app = builder.Build();

// Bu blok uretim ortami hata yonetimini ayarlar.
if (!app.Environment.IsDevelopment())
{
    // Bu satir genel hata sayfasini kullanir.
    app.UseExceptionHandler("/Home/Error");

    // Bu satir guvenli baglanti basligini etkinlestirir.
    app.UseHsts();
}

// Bu satir ilk calisista veritabanini ve ornek verileri hazirlar.
using (var kapsam = app.Services.CreateScope())
{
    // Bu satir hazirlayici servisini alip veritabanini kurar.
    var hazirlayici = kapsam.ServiceProvider.GetRequiredService<VeritabaniHazirlayici>();
    hazirlayici.Hazirla();
}

// Bu satir statik dosya hizmetini etkinlestirir.
app.UseStaticFiles();

// Bu satir istek yonlendirme altyapisini etkinlestirir.
app.UseRouting();

// Bu satir kimlik dogrulamayi etkinlestirir.
app.UseAuthentication();

// Bu satir yetkilendirmeyi etkinlestirir.
app.UseAuthorization();

// Bu satir varsayilan MVC rota desenini tanimlar.
app.MapControllerRoute(
    name: "varsayilan",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Bu satir uygulamayi calistirir.
app.Run();
