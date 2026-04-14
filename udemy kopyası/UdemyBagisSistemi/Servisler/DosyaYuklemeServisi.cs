// Bu dosya bilgisayardan secilen dosyalari uygulama klasorune kaydetmek icin kullanilir.
namespace UdemyBagisSistemi.Servisler;

// Bu servis egitmenin ve ogrencinin yukledigi dosyalari diske kaydeder.
public class DosyaYuklemeServisi
{
    // Bu alan web kok yolunu tutar.
    private readonly IWebHostEnvironment _ortam;

    // Bu kurucu ortam bilgisini alir.
    public DosyaYuklemeServisi(IWebHostEnvironment ortam)
    {
        // Bu satir ortam bagimliligini saklar.
        _ortam = ortam;
    }

    // Bu metod yuklenen dosyayi belirtilen alt klasore kaydeder.
    public async Task<string> DosyaKaydetAsync(IFormFile? dosya, string altKlasor)
    {
        // Bu satir dosya yoksa bos deger dondurur.
        if (dosya is null || dosya.Length == 0)
        {
            return string.Empty;
        }

        // Bu satir hedef klasor yolunu olusturur.
        var hedefKlasor = Path.Combine(_ortam.WebRootPath, "yuklenenler", altKlasor);
        Directory.CreateDirectory(hedefKlasor);

        // Bu satir dosya uzantisini koruyarak benzersiz ad uretir.
        var uzanti = Path.GetExtension(dosya.FileName);
        var benzersizAd = $"{Guid.NewGuid():N}{uzanti}";
        var tamYol = Path.Combine(hedefKlasor, benzersizAd);

        // Bu blok dosyayi diske yazar.
        await using var akim = new FileStream(tamYol, FileMode.Create);
        await dosya.CopyToAsync(akim);

        // Bu satir web tarafinda kullanilacak goreli yolu dondurur.
        return $"/yuklenenler/{altKlasor}/{benzersizAd}";
    }
}
