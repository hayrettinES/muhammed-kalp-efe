// Bu dosya veritabanini olusturur ve ilk verileri ekler.
using UdemyBagisSistemi.Models;

namespace UdemyBagisSistemi.Servisler;

// Bu servis uygulama baslarken gerekli tablolarin hazir olmasini saglar.
public class VeritabaniHazirlayici
{
    // Bu alan sqlite servisine erisim saglar.
    private readonly SqliteKomutServisi _sqlite;

    // Bu alan sifreleme servisine erisim saglar.
    private readonly SifrelemeServisi _sifreleme;

    // Bu kurucu servis bagimliliklarini alir.
    public VeritabaniHazirlayici(SqliteKomutServisi sqlite, SifrelemeServisi sifreleme)
    {
        // Bu satir sqlite bagimliligini saklar.
        _sqlite = sqlite;

        // Bu satir sifreleme bagimliligini saklar.
        _sifreleme = sifreleme;
    }

    // Bu metod tum tablo ve ornek verileri olusturur.
    public void Hazirla()
    {
        // Bu SQL blogu sistem tablolarini olusturur.
        _sqlite.KomutCalistir("""
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Kullanicilar (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AdSoyad TEXT NOT NULL,
                Eposta TEXT NOT NULL UNIQUE,
                SifreHash TEXT NOT NULL,
                Rol TEXT NOT NULL,
                Unvan TEXT NOT NULL DEFAULT '',
                Hakkinda TEXT NOT NULL DEFAULT '',
                ProfilFotoUrl TEXT NOT NULL DEFAULT '',
                Bakiye REAL NOT NULL DEFAULT 0,
                EpostaOnaylandiMi INTEGER NOT NULL DEFAULT 0,
                EpostaOnayKodu TEXT NOT NULL DEFAULT '',
                KayitTarihi TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Kategoriler (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Ad TEXT NOT NULL,
                Aciklama TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Kurslar (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EgitmenId INTEGER NOT NULL,
                KategoriId INTEGER NOT NULL,
                Baslik TEXT NOT NULL,
                Aciklama TEXT NOT NULL,
                VideoUrl TEXT NOT NULL DEFAULT '',
                DokumanUrl TEXT NOT NULL,
                ThumbnailUrl TEXT NOT NULL DEFAULT '',
                Fiyat REAL NOT NULL,
                YayinlandiMi INTEGER NOT NULL DEFAULT 0,
                OlusturmaTarihi TEXT NOT NULL,
                FOREIGN KEY (EgitmenId) REFERENCES Kullanicilar(Id),
                FOREIGN KEY (KategoriId) REFERENCES Kategoriler(Id)
            );

            CREATE TABLE IF NOT EXISTS KursBolumleri (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                KursId INTEGER NOT NULL,
                SiraNo INTEGER NOT NULL,
                Baslik TEXT NOT NULL,
                Aciklama TEXT NOT NULL,
                VideoUrl TEXT NOT NULL,
                DokumanUrl TEXT NOT NULL DEFAULT '',
                OlusturmaTarihi TEXT NOT NULL,
                FOREIGN KEY (KursId) REFERENCES Kurslar(Id)
            );

            CREATE TABLE IF NOT EXISTS Bagislar (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BagisciAdSoyad TEXT NOT NULL,
                Tutar REAL NOT NULL,
                Tarih TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS KursKayitlari (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OgrenciId INTEGER NOT NULL,
                KursId INTEGER NOT NULL,
                OdenenTutar REAL NOT NULL,
                OdemeYontemi TEXT NOT NULL,
                Tarih TEXT NOT NULL,
                FOREIGN KEY (OgrenciId) REFERENCES Kullanicilar(Id),
                FOREIGN KEY (KursId) REFERENCES Kurslar(Id),
                UNIQUE (OgrenciId, KursId)
            );

            CREATE TABLE IF NOT EXISTS SepetOgesi (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                KullaniciId INTEGER NOT NULL,
                KursId INTEGER NOT NULL,
                EklenmeTarihi TEXT NOT NULL,
                OdemeYontemi TEXT NOT NULL DEFAULT 'Bakiye',
                FOREIGN KEY (KullaniciId) REFERENCES Kullanicilar(Id),
                FOREIGN KEY (KursId) REFERENCES Kurslar(Id),
                UNIQUE (KullaniciId, KursId)
            );

            CREATE TABLE IF NOT EXISTS KursYorumlari (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                KursId INTEGER NOT NULL,
                OgrenciId INTEGER NOT NULL,
                Puan INTEGER NOT NULL,
                Yorum TEXT NOT NULL,
                Tarih TEXT NOT NULL,
                FOREIGN KEY (KursId) REFERENCES Kurslar(Id),
                FOREIGN KEY (OgrenciId) REFERENCES Kullanicilar(Id),
                UNIQUE (KursId, OgrenciId)
            );

            CREATE TABLE IF NOT EXISTS YorumYanitlari (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                KursYorumuId INTEGER NOT NULL,
                KullaniciId INTEGER NOT NULL,
                Yanit TEXT NOT NULL,
                Tarih TEXT NOT NULL,
                FOREIGN KEY (KursYorumuId) REFERENCES KursYorumlari(Id),
                FOREIGN KEY (KullaniciId) REFERENCES Kullanicilar(Id)
            );

            CREATE TABLE IF NOT EXISTS BolumYorumlari (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                KursBolumuId INTEGER NOT NULL,
                KullaniciId INTEGER NOT NULL,
                Yorum TEXT NOT NULL,
                Tarih TEXT NOT NULL,
                FOREIGN KEY (KursBolumuId) REFERENCES KursBolumleri(Id),
                FOREIGN KEY (KullaniciId) REFERENCES Kullanicilar(Id)
            );

            CREATE TABLE IF NOT EXISTS BolumIlerlemeleri (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OgrenciId INTEGER NOT NULL,
                KursId INTEGER NOT NULL,
                KursBolumuId INTEGER NOT NULL,
                TamamlandiMi INTEGER NOT NULL DEFAULT 0,
                SonIzlemeTarihi TEXT NOT NULL,
                FOREIGN KEY (OgrenciId) REFERENCES Kullanicilar(Id),
                FOREIGN KEY (KursId) REFERENCES Kurslar(Id),
                FOREIGN KEY (KursBolumuId) REFERENCES KursBolumleri(Id),
                UNIQUE (OgrenciId, KursBolumuId)
            );

            CREATE TABLE IF NOT EXISTS AiApiAnahtarlari (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ApiKey TEXT NOT NULL UNIQUE,
                Aktif INTEGER NOT NULL DEFAULT 1,
                KullanmaSayisi INTEGER NOT NULL DEFAULT 0,
                EklenmeTarihi TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS AiSohbetleri (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                KullaniciId INTEGER NOT NULL,
                Baslik TEXT NOT NULL DEFAULT 'Yeni Sohbet',
                OlusturmaTarihi TEXT NOT NULL,
                SonGuncellemeTarihi TEXT NOT NULL,
                FOREIGN KEY (KullaniciId) REFERENCES Kullanicilar(Id)
            );

            CREATE TABLE IF NOT EXISTS AiMesajlari (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SohbetId INTEGER NOT NULL,
                Rol TEXT NOT NULL,
                Icerik TEXT NOT NULL,
                Tarih TEXT NOT NULL,
                FOREIGN KEY (SohbetId) REFERENCES AiSohbetleri(Id)
            );
            """);

        // Bu satir eski veritabani varsa gerekli yeni kolonlari ekler.
        KolonYoksaEkle("Kullanicilar", "Unvan", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "Hakkinda", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "ProfilFotoUrl", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "Bakiye", "REAL NOT NULL DEFAULT 0");
        KolonYoksaEkle("Kullanicilar", "EpostaOnaylandiMi", "INTEGER NOT NULL DEFAULT 0");
        KolonYoksaEkle("Kullanicilar", "EpostaOnayKodu", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "EgitimSeviyesi", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "IlgiAlanlari", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "Hedef", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "Yonlendiren", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "DeneyimYili", "INTEGER NOT NULL DEFAULT 0");
        KolonYoksaEkle("Kullanicilar", "UzmanlikAlanlari", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "LinkedinProfili", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kullanicilar", "KursFormati", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kurslar", "VideoUrl", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kurslar", "OnizlemeVideoUrl", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Kurslar", "ThumbnailUrl", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("KursBolumleri", "DokumanUrl", "TEXT NOT NULL DEFAULT ''");
        KolonYoksaEkle("Bagislar", "OnaylandiMi", "INTEGER NOT NULL DEFAULT 0");
        KolonYoksaEkle("BolumYorumlari", "ParentId", "INTEGER NOT NULL DEFAULT 0");
        KolonYoksaEkle("SepetOgesi", "OdemeYontemi", "TEXT NOT NULL DEFAULT 'Bakiye'");

        // Bu metod ornek kullanicilari ekler.
        OrnekKullanicilariEkle();

        // Bu metod ornek kategorileri ekler.
        OrnekKategorileriEkle();

        // Bu metod ornek kurslari ekler.
        OrnekKurslariEkle();

        // Bu metod API key havuzunu doldurur.
        ApiKeyHavuzunuDoldur();
    }

    // Bu metod demo kullanicilarini sadece bir kez ekler.
    private void OrnekKullanicilariEkle()
    {
        // Bu satir mevcut admin var mi diye kontrol eder.
        var kayit = _sqlite.SorguCalistir("SELECT COUNT(*) AS Toplam FROM Kullanicilar;").FirstOrDefault();
        var toplam = int.TryParse(kayit?["Toplam"], out var sayi) ? sayi : 0;

        // Bu satir kullanicilar zaten varsa tekrar eklemeyi durdurur.
        if (toplam > 0)
        {
            return;
        }

        // Bu satir bugunun tarih bilgisini hazirlar.
        var tarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Bu blok varsayilan kullanicilari ekler.
        KullaniciEkle("Sistem Yonetici", "admin@udemybagis.com", "Admin123!", "Admin", tarih);
        KullaniciEkle("Ornek Egitmen", "egitmen@udemybagis.com", "Egitmen123!", "Egitmen", tarih);
        KullaniciEkle("Ornek Ogrenci", "ogrenci@udemybagis.com", "Ogrenci123!", "Ogrenci", tarih);
    }

    // Bu metod kullanici ekleme tekrarini azaltir.
    private void KullaniciEkle(string adSoyad, string eposta, string sifre, string rol, string tarih)
    {
        // Bu satir sifre hash degerini olusturur.
        var sifreHash = _sifreleme.HashOlustur(sifre);

        // Bu satir guvenli ekleme SQL'ini calistirir.
        _sqlite.KomutCalistir($"""
            INSERT INTO Kullanicilar (AdSoyad, Eposta, SifreHash, Rol, Unvan, Hakkinda, ProfilFotoUrl, Bakiye, EpostaOnaylandiMi, EpostaOnayKodu, KayitTarihi)
            VALUES (
                '{_sqlite.MetinGuvenli(adSoyad)}',
                '{_sqlite.MetinGuvenli(eposta)}',
                '{_sqlite.MetinGuvenli(sifreHash)}',
                '{_sqlite.MetinGuvenli(rol)}',
                '{_sqlite.MetinGuvenli(rol == "Egitmen" ? "Egitmen" : rol == "Admin" ? "Platform Yoneticisi" : "Ogrenci")}',
                '',
                '',
                0,
                1,
                '',
                '{tarih}'
            );
            """);
    }

    // Bu metod sistemdeki temel kategorileri varligina gore kontrol edip ekler.
    private void OrnekKategorileriEkle()
    {
        var kategoriler = new[]
        {
            "Yazılım Geliştirme", "Web Geliştirme", "Mobil Uygulama Geliştirme", 
            "Veri Bilimi ve Yapay Zeka", "Siber Güvenlik", "Ağ ve Sistem Yönetimi", 
            "Grafik Tasarım", "UI/UX Tasarım", "Video Düzenleme ve Montaj", 
            "Fotoğrafçılık", "Dijital Pazarlama", "Sosyal Medya Yönetimi", 
            "SEO", "E-Ticaret", "İşletme", "Girişimcilik", "Finans ve Muhasebe", 
            "Ofis Programları", "Kişisel Gelişim", "İletişim Becerileri", 
            "Liderlik ve Yönetim", "Yabancı Dil", "Sınav Hazırlık", "Müzik", 
            "Sağlık ve Yaşam Tarzı"
        };

        foreach (var k in kategoriler)
        {
            var guvenliAd = _sqlite.MetinGuvenli(k);
            var varMi = _sqlite.SorguCalistir($"SELECT COUNT(*) AS Toplam FROM Kategoriler WHERE Ad = '{guvenliAd}';").FirstOrDefault();
            
            if (int.TryParse(varMi?["Toplam"], out var sayi) && sayi == 0)
            {
                _sqlite.KomutCalistir($"INSERT INTO Kategoriler (Ad, Aciklama) VALUES ('{guvenliAd}', '{guvenliAd} eğitimleri');");
            }
        }
    }

    // Bu metod ornek kurslari ilk acilis deneyimi icin ekler.
    private void OrnekKurslariEkle()
    {
        // Bu satir kurs sayisini kontrol eder.
        var kayit = _sqlite.SorguCalistir("SELECT COUNT(*) AS Toplam FROM Kurslar;").FirstOrDefault();
        var toplam = int.TryParse(kayit?["Toplam"], out var sayi) ? sayi : 0;

        // Bu satir kurslar varsa tekrar eklemeyi durdurur.
        if (toplam > 0)
        {
            return;
        }

        // Bu satir ornek egitmenin kimligini bulur.
        var egitmenId = _sqlite.SorguCalistir("SELECT Id FROM Kullanicilar WHERE Rol = 'Egitmen' LIMIT 1;").FirstOrDefault()?["Id"] ?? "0";

        // Bu satir ilk kategorinin kimligini bulur.
        var kategoriId = _sqlite.SorguCalistir("SELECT Id FROM Kategoriler ORDER BY Id LIMIT 1;").FirstOrDefault()?["Id"] ?? "1";

        // Bu satir bugunun tarih bilgisini hazirlar.
        var tarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Bu blok ilk ornek kursu ekler.
        _sqlite.KomutCalistir($"""
            INSERT INTO Kurslar (EgitmenId, KategoriId, Baslik, Aciklama, VideoUrl, OnizlemeVideoUrl, DokumanUrl, ThumbnailUrl, Fiyat, YayinlandiMi, OlusturmaTarihi)
            VALUES (
                {egitmenId},
                {kategoriId},
                'ASP.NET Core MVC Sifirdan',
                'ASP.NET Core MVC ile katmanli bir kurs platformu gelistirme egitimi.',
                'https://samplelib.com/lib/preview/mp4/sample-5s.mp4',
                'https://samplelib.com/lib/preview/mp4/sample-5s.mp4',
                'https://example.com/ornek-dokuman.pdf',
                'https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&q=80&w=800',
                450,
                1,
                '{tarih}'
            );
            """);

        // Bu satir eklenen kursun kimligini bulur.
        var kursId = _sqlite.SorguCalistir("SELECT Id FROM Kurslar ORDER BY Id DESC LIMIT 1;").FirstOrDefault()?["Id"] ?? "0";

        // Bu blok ornek bolumleri ekler.
        _sqlite.KomutCalistir($"""
            INSERT INTO KursBolumleri (KursId, SiraNo, Baslik, Aciklama, VideoUrl, DokumanUrl, OlusturmaTarihi)
            VALUES
            ({kursId}, 1, 'Kuruluma Giris', 'Projeyi sifirdan olusturma ve temel mimariyi kurma.', 'https://samplelib.com/lib/preview/mp4/sample-5s.mp4', 'https://example.com/kurulum-notlari.pdf', '{tarih}'),
            ({kursId}, 2, 'MVC Yapisi', 'Controller, View ve servis katmanlarini ayirma.', 'https://samplelib.com/lib/preview/mp4/sample-10s.mp4', 'https://example.com/mvc-notlari.pdf', '{tarih}');
            """);
    }

    // Bu metod tabloya yeni kolon eklenmesi gerekiyorsa ekleme yapar.
    private void KolonYoksaEkle(string tabloAdi, string kolonAdi, string kolonTanimi)
    {
        // Bu satir tablo kolon bilgilerini okur.
        var kolonlar = _sqlite.SorguCalistir($"PRAGMA table_info({tabloAdi});");

        // Bu satir kolon zaten varsa tekrar eklemeyi durdurur.
        if (kolonlar.Any(x => string.Equals(x.GetValueOrDefault("name"), kolonAdi, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        // Bu satir yeni kolonu tabloya ekler.
        _sqlite.KomutCalistir($"ALTER TABLE {tabloAdi} ADD COLUMN {kolonAdi} {kolonTanimi};");
    }

    // Bu metod API key havuzuna seed verileri ekler.
    private void ApiKeyHavuzunuDoldur()
    {
        var anahtarlar = new[]
        {
            "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",
            "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",
            "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",
            "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",
            "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",
            "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"
        };

        var tarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        foreach (var key in anahtarlar)
        {
            var guvenliKey = _sqlite.MetinGuvenli(key);
            var varMi = _sqlite.SorguCalistir($"SELECT COUNT(*) AS Toplam FROM AiApiAnahtarlari WHERE ApiKey = '{guvenliKey}';").FirstOrDefault();
            if (int.TryParse(varMi?["Toplam"], out var sayi) && sayi == 0)
            {
                _sqlite.KomutCalistir($"INSERT INTO AiApiAnahtarlari (ApiKey, Aktif, KullanmaSayisi, EklenmeTarihi) VALUES ('{guvenliKey}', 1, 0, '{tarih}');");
            }
        }
    }
}
