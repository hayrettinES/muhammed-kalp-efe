// Bu dosya kullanici sifrelerini guvenli sekilde ozetlemek icin kullanilir.
using System.Security.Cryptography;

namespace UdemyBagisSistemi.Servisler;

// Bu servis sifre olusturma ve dogrulama islemlerini yapar.
public class SifrelemeServisi
{
    // Bu sabit uretilen tuz uzunlugunu belirtir.
    private const int TuzUzunlugu = 16;

    // Bu sabit anahtar turetme iterasyon sayisini belirtir.
    private const int IterasyonSayisi = 10_000;

    // Bu metod duz metin sifreyi tuzlu bir ozet degere cevirir.
    public string HashOlustur(string sifre)
    {
        // Bu blok rastgele bir tuz uretir.
        var tuz = RandomNumberGenerator.GetBytes(TuzUzunlugu);

        // Bu blok PBKDF2 ile guvenli bir ozet deger uretir.
        var anahtar = Rfc2898DeriveBytes.Pbkdf2(
            sifre,
            tuz,
            IterasyonSayisi,
            HashAlgorithmName.SHA256,
            32);

        // Bu satir veritabaninda saklanacak birlestirilmis degeri dondurur.
        return $"{Convert.ToBase64String(tuz)}:{Convert.ToBase64String(anahtar)}";
    }

    // Bu metod verilen sifrenin kayitli ozetle eslesip eslesmedigini kontrol eder.
    public bool Dogrula(string sifre, string kayitliHash)
    {
        // Bu blok kayitli hash degerini iki parcaya ayirir.
        var parcaliDeger = kayitliHash.Split(':', StringSplitOptions.RemoveEmptyEntries);

        // Bu satir beklenen format bozuksa dogrudan basarisiz doner.
        if (parcaliDeger.Length != 2)
        {
            return false;
        }

        // Bu blok kayitli tuz ve hash degerlerini byte dizisine cevirir.
        var tuz = Convert.FromBase64String(parcaliDeger[0]);
        var beklenenHash = Convert.FromBase64String(parcaliDeger[1]);

        // Bu blok ayni algoritma ile tekrar hash uretir.
        var hesaplananHash = Rfc2898DeriveBytes.Pbkdf2(
            sifre,
            tuz,
            IterasyonSayisi,
            HashAlgorithmName.SHA256,
            32);

        // Bu satir iki hash degerini guvenli sekilde karsilastirir.
        return CryptographicOperations.FixedTimeEquals(hesaplananHash, beklenenHash);
    }
}
