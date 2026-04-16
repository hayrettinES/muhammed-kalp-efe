// Bu dosya sqlite3 komut aracini kullanarak SQLite veritabanina erisim saglar.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;


namespace UdemyBagisSistemi.Servisler;

// Bu servis sqlite3 komutu uzerinden SQL calistirir.
public class SqliteKomutServisi
{
    // Bu alan uygulamanin kullandigi veritabani dosya yolunu tutar.
    private readonly string _veritabaniYolu;

    // Bu kurucu servis olusurken veritabani yolunu hazirlar.
    public SqliteKomutServisi(IWebHostEnvironment ortam)
    {
        // Bu blok veritabani klasorunu garanti eder.
        var veriKlasoru = Path.Combine(ortam.ContentRootPath, "Veriler");
        Directory.CreateDirectory(veriKlasoru);

        // Bu satir veritabani dosyasinin kesin yolunu olusturur.
        _veritabaniYolu = Path.Combine(veriKlasoru, "udemy.db");
    }

    // Bu alan disaridan veritabani dosya yolunun okunmasini saglar.
    public string VeritabaniYolu => _veritabaniYolu;

    // Bu metod veri dondurmeyen SQL komutlarini calistirir.
    public void KomutCalistir(string sql)
    {
        // Bu satir sqlite3 araci ile komutu calistirir.
        var sonuc = Calistir(["-batch", _veritabaniYolu, sql]);

        // Bu satir hata varsa istisna firlatir.
        if (sonuc.CikisKodu != 0)
        {
            throw new InvalidOperationException($"SQLite komut hatasi: {sonuc.Hata}");
        }
    }

    // Bu metod sorgu sonucunu JSON formatinda alip satir listesine cevirir.
    public List<Dictionary<string, string?>> SorguCalistir(string sql)
    {
        // Bu satir sqlite3 araci ile json formatli cikis uretir.
        var sonuc = Calistir(["-json", _veritabaniYolu, sql]);

        // Bu satir hata varsa istisna firlatir.
        if (sonuc.CikisKodu != 0)
        {
            throw new InvalidOperationException($"SQLite sorgu hatasi: {sonuc.Hata}");
        }

        // Bu satir bos cikis varsa bos liste dondurur.
        if (string.IsNullOrWhiteSpace(sonuc.StandartCikis))
        {
            return [];
        }

        // Bu satir json cikisini okunabilir yapiya cevirir.
        using var belge = JsonDocument.Parse(sonuc.StandartCikis);
        var liste = new List<Dictionary<string, string?>>();

        // Bu dongu her kaydi sozluk formatina cevirir.
        foreach (var oge in belge.RootElement.EnumerateArray())
        {
            var satir = new Dictionary<string, string?>();

            // Bu dongu kaydin alanlarini gezer.
            foreach (var alan in oge.EnumerateObject())
            {
                // Bu satir alan degerini null veya metin olarak ekler.
                satir[alan.Name] = alan.Value.ValueKind == JsonValueKind.Null
                    ? null
                    : alan.Value.ToString();
            }

            // Bu satir hazir satiri sonuca ekler.
            liste.Add(satir);
        }

        // Bu satir tum sonuclari dondurur.
        return liste;
    }

    // Bu metod girilen metni SQL icin guvenli hale getirir.
    public string MetinGuvenli(string? deger)
    {
        // Bu satir null degerleri bos metne cevirir.
        var hazirDeger = deger ?? string.Empty;

        // Bu satir tek tirnaklari kacirarak SQL metnine uygun hale getirir.
        return hazirDeger.Replace("'", "''");
    }

    // Bu metod ondalik sayiyi her zaman nokta ile yazar.
    public string SayiGuvenli(decimal deger)
    {
        // Bu satir culture farkini engelleyerek sayiyi SQL uyumlu dondurur.
        return deger.ToString(CultureInfo.InvariantCulture);
    }

    // Bu metod sqlite3 komutunu calistirip cikti nesnesi dondurur.
    private static (int CikisKodu, string StandartCikis, string Hata) Calistir(IEnumerable<string> argumanlar)
    {
        // Bu satir isletim sistemine gore sqlite3 yolunu belirler.
        var sqlite3Yolu = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "sqlite3.exe"
            : "/usr/bin/sqlite3";

        // Bu blok process ayarlarini kurar.
        var bilgi = new ProcessStartInfo
        {
            FileName = sqlite3Yolu,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Bu dongu argumanlari process bilgisine ekler.
        foreach (var arguman in argumanlar)
        {
            bilgi.ArgumentList.Add(arguman);
        }

        // Bu blok processi baslatir.
        using var process = Process.Start(bilgi) ?? throw new InvalidOperationException("sqlite3 baslatilamadi.");
        var standartCikis = process.StandardOutput.ReadToEnd();
        var hata = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Bu satir process sonucunu dondurur.
        return (process.ExitCode, standartCikis, hata);
    }
}
