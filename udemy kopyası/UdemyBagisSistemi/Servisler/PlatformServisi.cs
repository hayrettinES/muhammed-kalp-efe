// Bu dosya is kurallarini tek noktada toplar.
using System.Globalization;
using UdemyBagisSistemi.Models;
using UdemyBagisSistemi.ViewModels;

namespace UdemyBagisSistemi.Servisler;

// Bu servis uygulamanin ana is kurallarini yonetir.
public class PlatformServisi
{
    // Bu alan sqlite servisini tutar.
    private readonly SqliteKomutServisi _sqlite;

    // Bu alan sifreleme servisini tutar.
    private readonly SifrelemeServisi _sifreleme;

    // Bu kurucu servis bagimliliklarini alir.
    public PlatformServisi(SqliteKomutServisi sqlite, SifrelemeServisi sifreleme)
    {
        // Bu satir sqlite bagimliligini saklar.
        _sqlite = sqlite;

        // Bu satir sifreleme bagimliligini saklar.
        _sifreleme = sifreleme;
    }

    // Bu metod anasayfa icin gerekli ozet verileri getirir.
    public AnasayfaViewModel AnasayfaVerisiniGetir()
    {
        // Bu satir model nesnesini doldurulmak uzere hazirlar.
        return new AnasayfaViewModel
        {
            HavuzBakiyesi = HavuzBakiyesiGetir(),
            ToplamKategoriSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Kategoriler;"),
            ToplamKursSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Kurslar WHERE YayinlandiMi = 1;"),
            ToplamBagisSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Bagislar;"),
            YayinliKurslar = YayinliKurslariGetir()
        };
    }

    // Bu metod herkese acik bagis sayfasi verisini getirir.
    public BagisSayfasiViewModel BagisSayfasiVerisiniGetir()
    {
        // Bu satir bagis sayfasi ozet bilgisini dondurur.
        return new BagisSayfasiViewModel
        {
            HavuzBakiyesi = HavuzBakiyesiGetir(),
            ToplamBagisSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Bagislar;")
        };
    }

    // Bu metod eposta ile kullanici kaydini bulur.
    public Kullanici? KullaniciGetir(string eposta)
    {
        // Bu satir guvenli eposta metnini hazirlar.
        var guvenliEposta = _sqlite.MetinGuvenli(eposta);

        // Bu satir kullanici sorgusunu calistirir.
        var satir = _sqlite.SorguCalistir($"SELECT * FROM Kullanicilar WHERE Eposta = '{guvenliEposta}' LIMIT 1;").FirstOrDefault();

        // Bu satir sonuc yoksa null dondurur.
        return satir is null ? null : KullaniciDonustur(satir);
    }

    // Bu metod kullaniciyi kimlige gore getirir.
    public Kullanici? KullaniciGetir(int kullaniciId)
    {
        // Bu satir kullanici sorgusunu calistirir.
        var satir = _sqlite.SorguCalistir($"SELECT * FROM Kullanicilar WHERE Id = {kullaniciId} LIMIT 1;").FirstOrDefault();

        // Bu satir sonuc yoksa null dondurur.
        return satir is null ? null : KullaniciDonustur(satir);
    }

    // Bu metod profil ekran modelini getirir.
    public ProfilViewModel ProfilGetir(int kullaniciId)
    {
        // Bu satir kullaniciyi bulur.
        var kullanici = KullaniciGetir(kullaniciId) ?? throw new InvalidOperationException("Kullanici bulunamadi.");

        // Bu satir profil modelini dondurur.
        return new ProfilViewModel
        {
            Id = kullanici.Id,
            AdSoyad = kullanici.AdSoyad,
            Eposta = kullanici.Eposta,
            Unvan = kullanici.Unvan,
            Hakkinda = kullanici.Hakkinda,
            ProfilFotoUrl = kullanici.ProfilFotoUrl,
            Rol = kullanici.Rol
        };
    }

    // Bu metod profil gunceller.
    public void ProfilGuncelle(int kullaniciId, string adSoyad, string eposta, string unvan, string hakkinda, string profilFotoUrl)
    {
        // Bu satir ayni epostanin baska kullanicida olup olmadigini kontrol eder.
        var cakisanKayit = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kullanicilar WHERE Eposta = '{_sqlite.MetinGuvenli(eposta)}' AND Id <> {kullaniciId};");

        // Bu satir eposta cakismasi varsa hata verir.
        if (cakisanKayit > 0)
        {
            throw new InvalidOperationException("Bu eposta baska bir kullanici tarafindan kullaniliyor.");
        }

        // Bu satir profil bilgilerini gunceller.
        _sqlite.KomutCalistir($"""
            UPDATE Kullanicilar
            SET AdSoyad = '{_sqlite.MetinGuvenli(adSoyad)}',
                Eposta = '{_sqlite.MetinGuvenli(eposta)}',
                Unvan = '{_sqlite.MetinGuvenli(unvan)}',
                Hakkinda = '{_sqlite.MetinGuvenli(hakkinda)}',
                ProfilFotoUrl = '{_sqlite.MetinGuvenli(profilFotoUrl)}'
            WHERE Id = {kullaniciId};
            """);
    }

    // Bu metod kimlik dogrulama yapar.
    public Kullanici? GirisYap(string eposta, string sifre)
    {
        // Bu satir epostaya gore kullaniciyi bulur.
        var kullanici = KullaniciGetir(eposta);

        // Bu satir kullanici yoksa dogrudan basarisiz doner.
        if (kullanici is null)
        {
            return null;
        }

        // Bu satir sifre eslesmiyorsa basarisiz doner.
        return _sifreleme.Dogrula(sifre, kullanici.SifreHash) ? kullanici : null;
    }

    // Bu metod yeni ogrenci veya egitmen kaydi olusturur.
    public (bool Basarili, string Mesaj) KayitOl(KayitViewModel model)
    {
        // Bu satir daha once ayni eposta var mi diye kontrol eder.
        if (KullaniciGetir(model.Eposta) is not null)
        {
            return (false, "Bu eposta ile kayitli bir kullanici zaten var.");
        }

        // Bu satir admin rolunu disaridan kapatir.
        if (model.Rol == "Admin")
        {
            return (false, "Admin kaydi sadece sistem tarafindan olusturulur.");
        }

        // Bu satir kayit tarihini hazirlar.
        var tarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Bu satir sifre hash degerini olusturur.
        var sifreHash = _sifreleme.HashOlustur(model.Sifre);

        // Bu satir rol tabanli varsayilan unvani belirler.
        var unvan = model.Rol == "Egitmen" ? "Yeni Egitmen" : "Yeni Ogrenci";

        // Bu satir yeni kullaniciyi veritabanina ekler.
        _sqlite.KomutCalistir($"""
            INSERT INTO Kullanicilar (AdSoyad, Eposta, SifreHash, Rol, Unvan, Hakkinda, ProfilFotoUrl, KayitTarihi)
            VALUES (
                '{_sqlite.MetinGuvenli(model.AdSoyad)}',
                '{_sqlite.MetinGuvenli(model.Eposta)}',
                '{_sqlite.MetinGuvenli(sifreHash)}',
                '{_sqlite.MetinGuvenli(model.Rol)}',
                '{_sqlite.MetinGuvenli(unvan)}',
                '',
                '',
                '{tarih}'
            );
            """);

        // Bu satir basarili sonuc dondurur.
        return (true, "Kayit basarili. Simdi giris yapabilirsin.");
    }

    // Bu metod admin paneli verilerini toplar.
    public AdminPanelViewModel AdminPanelVerisiniGetir(int? kategoriId)
    {
        // Bu satir admin modeli olusturur.
        return new AdminPanelViewModel
        {
            HavuzBakiyesi = HavuzBakiyesiGetir(),
            Kategoriler = KategorileriGetir(),
            Kullanicilar = TumKullanicilariGetir(),
            Kurslar = TumKursKartlariniGetir(),
            DuzenlenenKategori = kategoriId.HasValue ? KategoriGetir(kategoriId.Value) : null,
            ToplamYorumSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM KursYorumlari;")
        };
    }

    // Bu metod kategori ekler.
    public void KategoriEkle(string ad, string aciklama)
    {
        // Bu satir yeni kategori kaydini olusturur.
        _sqlite.KomutCalistir($"""
            INSERT INTO Kategoriler (Ad, Aciklama)
            VALUES ('{_sqlite.MetinGuvenli(ad)}', '{_sqlite.MetinGuvenli(aciklama)}');
            """);
    }

    // Bu metod kategori gunceller.
    public void KategoriGuncelle(int id, string ad, string aciklama)
    {
        // Bu satir secilen kategori kaydini gunceller.
        _sqlite.KomutCalistir($"""
            UPDATE Kategoriler
            SET Ad = '{_sqlite.MetinGuvenli(ad)}',
                Aciklama = '{_sqlite.MetinGuvenli(aciklama)}'
            WHERE Id = {id};
            """);
    }

    // Bu metod kategori siler.
    public void KategoriSil(int id)
    {
        // Bu satir once bu kategoriye bagli kurs var mi diye kontrol eder.
        var bagliKursSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kurslar WHERE KategoriId = {id};");

        // Bu satir bagli kurs varsa silmeyi engeller.
        if (bagliKursSayisi > 0)
        {
            throw new InvalidOperationException("Bu kategoriye bagli kurslar oldugu icin silinemez.");
        }

        // Bu satir guvenli kategori silme islemini yapar.
        _sqlite.KomutCalistir($"DELETE FROM Kategoriler WHERE Id = {id};");
    }

    // Bu metod egitmen paneli verilerini toplar.
    public EgitmenPanelViewModel EgitmenPanelVerisiniGetir(int egitmenId, int? kursId)
    {
        // Bu satir secili kurs nesnesini bulur.
        var seciliKurs = kursId.HasValue ? KursGetir(kursId.Value, egitmenId) : null;

        // Bu satir egitmen modeli olusturur.
        return new EgitmenPanelViewModel
        {
            Kategoriler = KategorileriGetir(),
            Kurslar = EgitmenKurslariniGetir(egitmenId),
            DuzenlenenKurs = seciliKurs,
            Bolumler = seciliKurs is null ? [] : KursBolumleriniGetir(seciliKurs.Id)
        };
    }

    // Bu metod kurs ekleme veya guncelleme yapar.
    public int KursKaydet(Kurs kurs)
    {
        // Bu satir bos baslik kontrolu yapar.
        if (string.IsNullOrWhiteSpace(kurs.Baslik))
        {
            throw new InvalidOperationException("Kurs basligi bos olamaz.");
        }

        // Bu satir en az onizleme beklentisini kontrol eder.
        if (string.IsNullOrWhiteSpace(kurs.OnizlemeVideoUrl))
        {
            throw new InvalidOperationException("Kurs icin bir onizleme videosu yuklenmeli.");
        }

        // Bu satir ekleme veya guncelleme ayrimi yapar.
        if (kurs.Id == 0)
        {
            // Bu satir yeni kurs kaydi olusturur.
            _sqlite.KomutCalistir($"""
                INSERT INTO Kurslar (EgitmenId, KategoriId, Baslik, Aciklama, VideoUrl, OnizlemeVideoUrl, DokumanUrl, Fiyat, YayinlandiMi, OlusturmaTarihi)
                VALUES (
                    {kurs.EgitmenId},
                    {kurs.KategoriId},
                    '{_sqlite.MetinGuvenli(kurs.Baslik)}',
                    '{_sqlite.MetinGuvenli(kurs.Aciklama)}',
                    '{_sqlite.MetinGuvenli(kurs.OnizlemeVideoUrl)}',
                    '{_sqlite.MetinGuvenli(kurs.OnizlemeVideoUrl)}',
                    '{_sqlite.MetinGuvenli(kurs.DokumanUrl)}',
                    {_sqlite.SayiGuvenli(kurs.Fiyat)},
                    {(kurs.YayinlandiMi ? 1 : 0)},
                    '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
                );
                """);

            // Bu satir son eklenen kurs kimligini dondurur.
            return SayiGetir("SELECT Id AS Toplam FROM Kurslar ORDER BY Id DESC LIMIT 1;");
        }

        // Bu satir var olan kurs kaydini gunceller.
        _sqlite.KomutCalistir($"""
            UPDATE Kurslar
            SET KategoriId = {kurs.KategoriId},
                Baslik = '{_sqlite.MetinGuvenli(kurs.Baslik)}',
                Aciklama = '{_sqlite.MetinGuvenli(kurs.Aciklama)}',
                VideoUrl = '{_sqlite.MetinGuvenli(kurs.OnizlemeVideoUrl)}',
                OnizlemeVideoUrl = '{_sqlite.MetinGuvenli(kurs.OnizlemeVideoUrl)}',
                DokumanUrl = '{_sqlite.MetinGuvenli(kurs.DokumanUrl)}',
                Fiyat = {_sqlite.SayiGuvenli(kurs.Fiyat)},
                YayinlandiMi = {(kurs.YayinlandiMi ? 1 : 0)}
            WHERE Id = {kurs.Id} AND EgitmenId = {kurs.EgitmenId};
            """);

        // Bu satir guncellenen kurs kimligini dondurur.
        return kurs.Id;
    }

    // Bu metod kursa yeni video bolumu ekler.
    public void KursBolumuEkle(int kursId, int egitmenId, string baslik, string aciklama, string videoUrl, string dokumanUrl)
    {
        // Bu satir kursun egitmene ait olup olmadigini kontrol eder.
        var kurs = KursGetir(kursId, egitmenId);

        // Bu satir kurs bulunamazsa hata verir.
        if (kurs is null)
        {
            throw new InvalidOperationException("Bolum eklenecek kurs bulunamadi.");
        }

        // Bu satir bolum videosu kontrolu yapar.
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            throw new InvalidOperationException("Bolum videosu secmelisin.");
        }

        // Bu satir yeni sira numarasini belirler.
        var yeniSiraNo = SayiGetir($"SELECT COALESCE(MAX(SiraNo), 0) + 1 AS Toplam FROM KursBolumleri WHERE KursId = {kursId};");

        // Bu satir bolumu veritabanina ekler.
        _sqlite.KomutCalistir($"""
            INSERT INTO KursBolumleri (KursId, SiraNo, Baslik, Aciklama, VideoUrl, DokumanUrl, OlusturmaTarihi)
            VALUES (
                {kursId},
                {yeniSiraNo},
                '{_sqlite.MetinGuvenli(baslik)}',
                '{_sqlite.MetinGuvenli(aciklama)}',
                '{_sqlite.MetinGuvenli(videoUrl)}',
                '{_sqlite.MetinGuvenli(dokumanUrl)}',
                '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            );
            """);
    }

    // Bu metod kurs yayin durumunu tersine cevirir.
    public void KursYayinDurumuDegistir(int kursId, int egitmenId)
    {
        // Bu satir yayina alinacak kursun en az bir bolumu olmasini kontrol eder.
        var bolumSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursBolumleri WHERE KursId = {kursId};");
        var kurs = KursGetir(kursId, egitmenId);

        // Bu satir kurs bulunamazsa hata verir.
        if (kurs is null)
        {
            throw new InvalidOperationException("Kurs bulunamadi.");
        }

        // Bu satir yayina alma sirasinda gerekli minimum icerik kontrolu yapar.
        if (!kurs.YayinlandiMi && bolumSayisi == 0)
        {
            throw new InvalidOperationException("Kursu yayinlamak icin en az bir video bolumu eklemelisin.");
        }

        // Bu satir yayin durumunu tek SQL ile tersine cevirir.
        _sqlite.KomutCalistir($"""
            UPDATE Kurslar
            SET YayinlandiMi = CASE WHEN YayinlandiMi = 1 THEN 0 ELSE 1 END
            WHERE Id = {kursId} AND EgitmenId = {egitmenId};
            """);
    }

    // Bu metod ogrenci paneli verilerini toplar.
    public OgrenciPanelViewModel OgrenciPanelVerisiniGetir(int ogrenciId)
    {
        // Bu satir ogrencinin aldigi kurslari bulur.
        var aldigiKurslar = OgrencininAldigiKurslariGetir(ogrenciId);

        // Bu satir ogrenci modeli olusturur.
        return new OgrenciPanelViewModel
        {
            HavuzBakiyesi = HavuzBakiyesiGetir(),
            OgrenciBakiyesi = OgrenciBakiyesiGetir(ogrenciId),
            Kurslar = OgrenciIcinKurslariGetir(ogrenciId),
            AldigiKurslar = aldigiKurslar,
            Ilerlemeler = aldigiKurslar.Select(x => KursIlerlemeOzetiGetir(x.Kurs.Id, ogrenciId)).ToList()
        };
    }

    // Bu metod kurs detay ekranini hazirlar.
    public KursDetayViewModel KursDetayiGetir(int kursId, int? aktifKullaniciId)
    {
        // Bu satir tek kurs kartini getirir.
        var kursKart = TekKursKartiniGetir(kursId, aktifKullaniciId);

        // Bu satir kurs yoksa hata verir.
        if (kursKart is null || !kursKart.Kurs.YayinlandiMi)
        {
            throw new InvalidOperationException("Kurs bulunamadi.");
        }

        // Bu satir aktif kullanicinin sahiplik durumunu belirler.
        var sahipMi = kursKart.SahipMi;

        // Bu satir aktif kullanici varsa ilerleme ozetini getirir.
        var ilerleme = aktifKullaniciId.HasValue ? KursIlerlemeOzetiGetir(kursId, aktifKullaniciId.Value) : null;

        // Bu satir detay modelini dondurur.
        return new KursDetayViewModel
        {
            KursKart = kursKart,
            Bolumler = sahipMi ? KursBolumleriniGetir(kursId) : [],
            Yorumlar = KursYorumlariniGetir(kursId),
            SahipMi = sahipMi,
            YorumYapabilirMi = sahipMi && aktifKullaniciId.HasValue,
            IlerlemeOzeti = ilerleme,
            AktifKullaniciId = aktifKullaniciId
        };
    }

    // Bu metod sisteme bagis ekler.
    public void BagisYap(string bagisciAdSoyad, decimal tutar)
    {
        // Bu satir sifir veya negatif bagisi engeller.
        if (tutar <= 0)
        {
            throw new InvalidOperationException("Bagis tutari sifirdan buyuk olmalidir.");
        }

        // Bu satir bagisci adi kontrolu yapar.
        if (string.IsNullOrWhiteSpace(bagisciAdSoyad))
        {
            throw new InvalidOperationException("Bagisci adi bos olamaz.");
        }

        // Bu satir bagisi veritabanina ekler.
        _sqlite.KomutCalistir($"""
            INSERT INTO Bagislar (BagisciAdSoyad, Tutar, Tarih)
            VALUES (
                '{_sqlite.MetinGuvenli(bagisciAdSoyad)}',
                {_sqlite.SayiGuvenli(tutar)},
                '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            );
            """);
    }

    // Bu metod ogrencinin kendi hesabina bakiye yuklemesini saglar.
    public void OgrenciBakiyesiYukle(int ogrenciId, decimal tutar)
    {
        // Bu satir yukleme tutarinin gecerli olmasini kontrol eder.
        if (tutar <= 0)
        {
            throw new InvalidOperationException("Yuklenecek tutar sifirdan buyuk olmalidir.");
        }

        // Bu satir ogrenci bakiyesini artirir.
        _sqlite.KomutCalistir($"""
            UPDATE Kullanicilar
            SET Bakiye = COALESCE(Bakiye, 0) + {_sqlite.SayiGuvenli(tutar)}
            WHERE Id = {ogrenciId} AND Rol = 'Ogrenci';
            """);
    }

    // Bu metod ogrencinin kurs almasini saglar.
    public string KursSatinAl(int kursId, int ogrenciId)
    {
        // Bu satir ogrencinin daha once bu kursu alip almadigini kontrol eder.
        var kayitSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursKayitlari WHERE OgrenciId = {ogrenciId} AND KursId = {kursId};");

        // Bu satir ayni kurs ikinci kez alinmak istenirse mesaji dondurur.
        if (kayitSayisi > 0)
        {
            return "Bu kursu zaten almissin.";
        }

        // Bu satir kurs bilgisini getirir.
        var kurs = KursGetir(kursId, null);

        // Bu satir kurs bulunamazsa hata firlatir.
        if (kurs is null || !kurs.YayinlandiMi)
        {
            throw new InvalidOperationException("Kurs bulunamadi veya yayinlanmamis.");
        }

        // Bu satir guncel havuz bakiyesini getirir.
        var havuzBakiyesi = HavuzBakiyesiGetir();

        // Bu satir ogrencinin kendi bakiyesini getirir.
        var ogrenciBakiyesi = OgrenciBakiyesiGetir(ogrenciId);

        // Bu satir odeme yontemini kurala gore belirler.
        var odemeYontemi = havuzBakiyesi >= kurs.Fiyat
            ? "Havuz"
            : ogrenciBakiyesi >= kurs.Fiyat
                ? "Bakiye"
                : string.Empty;

        // Bu satir iki odeme kaynagi da yetersizse hata verir.
        if (string.IsNullOrWhiteSpace(odemeYontemi))
        {
            throw new InvalidOperationException("Ne bagis havuzunda ne de ogrenci bakiyesinde yeterli tutar var.");
        }

        // Bu satir kurs kaydini olusturur.
        _sqlite.KomutCalistir($"""
            INSERT INTO KursKayitlari (OgrenciId, KursId, OdenenTutar, OdemeYontemi, Tarih)
            VALUES (
                {ogrenciId},
                {kurs.Id},
                {_sqlite.SayiGuvenli(kurs.Fiyat)},
                '{odemeYontemi}',
                '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            );
            """);

        // Bu satir odeme ogrenci bakiyesinden geldiyse bakiyeyi dusurur.
        if (odemeYontemi == "Bakiye")
        {
            _sqlite.KomutCalistir($"""
                UPDATE Kullanicilar
                SET Bakiye = Bakiye - {_sqlite.SayiGuvenli(kurs.Fiyat)}
                WHERE Id = {ogrenciId};
                """);
        }

        // Bu satir kullaniciya sonuc mesajini doner.
        return odemeYontemi == "Havuz"
            ? "Kurs odemesi bagis havuzundan karsilandi."
            : "Bagis havuzu yetersiz oldugu icin kurs ucreti ogrenci bakiyesinden dusuldu.";
    }

    // Bu metod ogrencinin mevcut bakiyesini getirir.
    public decimal OgrenciBakiyesiGetir(int ogrenciId)
    {
        // Bu satir kullanicinin bakiye degerini okur.
        var satir = _sqlite.SorguCalistir($"SELECT COALESCE(Bakiye, 0) AS Toplam FROM Kullanicilar WHERE Id = {ogrenciId} LIMIT 1;").FirstOrDefault();

        // Bu satir sonucu decimal olarak dondurur.
        return decimal.TryParse(
            satir?.GetValueOrDefault("Toplam"),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var bakiye)
            ? bakiye
            : 0;
    }

    // Bu metod ogrencinin bolumu tamamlamasini veya son gordugu yer olarak isaretlemesini saglar.
    public void BolumIlerlemesiniKaydet(int kursId, int kursBolumuId, int ogrenciId, bool tamamlandiMi)
    {
        // Bu satir sadece kurs sahibi ogrencinin ilerleme isaretleyebilmesini saglar.
        var sahiplik = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursKayitlari WHERE OgrenciId = {ogrenciId} AND KursId = {kursId};");

        // Bu satir sahiplik yoksa hata verir.
        if (sahiplik == 0)
        {
            throw new InvalidOperationException("Ilerleme kaydetmek icin kursu almis olman gerekir.");
        }

        // Bu satir ilgili ilerleme kaydi var mi diye kontrol eder.
        var mevcut = SayiGetir($"SELECT COUNT(*) AS Toplam FROM BolumIlerlemeleri WHERE OgrenciId = {ogrenciId} AND KursBolumuId = {kursBolumuId};");

        // Bu satir tarih bilgisini hazirlar.
        var tarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Bu satir kayit varsa gunceller.
        if (mevcut > 0)
        {
            _sqlite.KomutCalistir($"""
                UPDATE BolumIlerlemeleri
                SET TamamlandiMi = {(tamamlandiMi ? 1 : 0)},
                    SonIzlemeTarihi = '{tarih}'
                WHERE OgrenciId = {ogrenciId} AND KursBolumuId = {kursBolumuId};
                """);
        }
        else
        {
            _sqlite.KomutCalistir($"""
                INSERT INTO BolumIlerlemeleri (OgrenciId, KursId, KursBolumuId, TamamlandiMi, SonIzlemeTarihi)
                VALUES ({ogrenciId}, {kursId}, {kursBolumuId}, {(tamamlandiMi ? 1 : 0)}, '{tarih}');
                """);
        }
    }

    // Bu metod kurs ilerleme ozetini hesaplar.
    public KursIlerlemeOzeti KursIlerlemeOzetiGetir(int kursId, int ogrenciId)
    {
        // Bu satir toplam bolum sayisini bulur.
        var toplamBolumSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursBolumleri WHERE KursId = {kursId};");

        // Bu satir tamamlanan bolum sayisini bulur.
        var tamamlananBolumSayisi = SayiGetir($"""
            SELECT COUNT(*) AS Toplam
            FROM BolumIlerlemeleri
            WHERE KursId = {kursId} AND OgrenciId = {ogrenciId} AND TamamlandiMi = 1;
            """);

        // Bu satir son kalinan bolumu tarih sirasina gore bulur.
        var sonKayit = _sqlite.SorguCalistir($"""
            SELECT kb.Id, kb.Baslik
            FROM BolumIlerlemeleri bi
            INNER JOIN KursBolumleri kb ON kb.Id = bi.KursBolumuId
            WHERE bi.KursId = {kursId} AND bi.OgrenciId = {ogrenciId}
            ORDER BY bi.SonIzlemeTarihi DESC
            LIMIT 1;
            """).FirstOrDefault();

        // Bu satir yuzde bilgisini hesaplar.
        var yuzde = toplamBolumSayisi == 0 ? 0 : Math.Round((decimal)tamamlananBolumSayisi / toplamBolumSayisi * 100, 1);

        // Bu satir ozet modeli dondurur.
        return new KursIlerlemeOzeti
        {
            KursId = kursId,
            TamamlanmaYuzdesi = yuzde,
            TamamlananBolumSayisi = tamamlananBolumSayisi,
            ToplamBolumSayisi = toplamBolumSayisi,
            SonKalinanBolumId = sonKayit is null ? null : IntGetir(sonKayit, "Id"),
            SonKalinanBolumBasligi = sonKayit?.GetValueOrDefault("Baslik") ?? string.Empty
        };
    }

    // Bu metod ogrencinin aldigi kursa yorum birakmasini saglar.
    public void KursYorumYap(int kursId, int ogrenciId, int puan, string yorum)
    {
        // Bu satir sadece kurs sahibi ogrencinin yorum yapmasina izin verir.
        var sahiplik = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursKayitlari WHERE KursId = {kursId} AND OgrenciId = {ogrenciId};");

        // Bu satir kursa sahip degilse hata verir.
        if (sahiplik == 0)
        {
            throw new InvalidOperationException("Yorum yapabilmek icin once kursu alman gerekir.");
        }

        // Bu satir puan araligini kontrol eder.
        if (puan < 1 || puan > 5)
        {
            throw new InvalidOperationException("Puan 1 ile 5 arasinda olmalidir.");
        }

        // Bu satir daha once yorum var mi diye kontrol eder.
        var mevcut = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursYorumlari WHERE KursId = {kursId} AND OgrenciId = {ogrenciId};");

        // Bu satir guncelleme veya ekleme islemini secerek calistirir.
        if (mevcut > 0)
        {
            _sqlite.KomutCalistir($"""
                UPDATE KursYorumlari
                SET Puan = {puan},
                    Yorum = '{_sqlite.MetinGuvenli(yorum)}',
                    Tarih = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
                WHERE KursId = {kursId} AND OgrenciId = {ogrenciId};
                """);
        }
        else
        {
            _sqlite.KomutCalistir($"""
                INSERT INTO KursYorumlari (KursId, OgrenciId, Puan, Yorum, Tarih)
                VALUES ({kursId}, {ogrenciId}, {puan}, '{_sqlite.MetinGuvenli(yorum)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}');
                """);
        }
    }

    // Bu metod bir yoruma yanit ekler.
    public void YorumYanitiEkle(int kursYorumuId, int kullaniciId, string yanit)
    {
        // Bu satir kullanicinin girilen yaniti bos birakmasini engeller.
        if (string.IsNullOrWhiteSpace(yanit))
        {
            throw new InvalidOperationException("Yanit metni bos olamaz.");
        }

        // Bu satir ilgili yoruma yaniti ekler.
        _sqlite.KomutCalistir($"""
            INSERT INTO YorumYanitlari (KursYorumuId, KullaniciId, Yanit, Tarih)
            VALUES ({kursYorumuId}, {kullaniciId}, '{_sqlite.MetinGuvenli(yanit)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}');
            """);
    }

    // Bu metod kategori listesini getirir.
    public List<Kategori> KategorileriGetir()
    {
        // Bu satir kategori sorgusunu calistirir ve modele cevirir.
        return _sqlite.SorguCalistir("SELECT * FROM Kategoriler ORDER BY Ad;")
            .Select(KategoriDonustur)
            .ToList();
    }

    // Bu metod tek bir kategoriyi getirir.
    public Kategori? KategoriGetir(int id)
    {
        // Bu satir hedef kategoriyi bulur.
        var satir = _sqlite.SorguCalistir($"SELECT * FROM Kategoriler WHERE Id = {id} LIMIT 1;").FirstOrDefault();

        // Bu satir sonuc yoksa null dondurur.
        return satir is null ? null : KategoriDonustur(satir);
    }

    // Bu metod tek kursu getirir ve istenirse egitmen sahipligini kontrol eder.
    public Kurs? KursGetir(int kursId, int? egitmenId)
    {
        // Bu satir gerekli filtreyi olusturur.
        var filtre = egitmenId.HasValue ? $"AND EgitmenId = {egitmenId.Value}" : string.Empty;

        // Bu satir kurs sorgusunu calistirir.
        var satir = _sqlite.SorguCalistir($"SELECT * FROM Kurslar WHERE Id = {kursId} {filtre} LIMIT 1;").FirstOrDefault();

        // Bu satir sonuc yoksa null dondurur.
        return satir is null ? null : KursDonustur(satir);
    }

    // Bu metod toplam havuz bakiyesini hesaplar.
    public decimal HavuzBakiyesiGetir()
    {
        // Bu satir toplam bagis tutarini alir.
        var bagisToplami = DecimalGetir("SELECT COALESCE(SUM(Tutar), 0) AS Toplam FROM Bagislar;");

        // Bu satir havuzdan harcanan tutari alir.
        var harcananToplam = DecimalGetir("SELECT COALESCE(SUM(OdenenTutar), 0) AS Toplam FROM KursKayitlari WHERE OdemeYontemi = 'Havuz';");

        // Bu satir kalan bakiyeyi dondurur.
        return bagisToplami - harcananToplam;
    }

    // Bu metod kursun video bolumlerini getirir.
    public List<KursBolumu> KursBolumleriniGetir(int kursId)
    {
        // Bu satir bolum sorgusunu calistirir.
        return _sqlite.SorguCalistir($"SELECT * FROM KursBolumleri WHERE KursId = {kursId} ORDER BY SiraNo;")
            .Select(KursBolumuDonustur)
            .ToList();
    }

    // Bu metod kursun yorumlarini getirir.
    public List<KursYorumu> KursYorumlariniGetir(int kursId)
    {
        // Bu satir yorumlari ogrenci adi ile birlikte getirir.
        var yorumlar = _sqlite.SorguCalistir($"""
            SELECT ky.*, ku.AdSoyad AS OgrenciAdi
            FROM KursYorumlari ky
            INNER JOIN Kullanicilar ku ON ku.Id = ky.OgrenciId
            WHERE ky.KursId = {kursId}
            ORDER BY ky.Id DESC;
            """)
            .Select(KursYorumuDonustur)
            .ToList();

        // Bu dongu her yorum icin yanitlari ekler.
        foreach (var yorum in yorumlar)
        {
            yorum.Yanitlar = YorumYanitlariniGetir(yorum.Id);
        }

        // Bu satir zenginlestirilmis yorum listesini dondurur.
        return yorumlar;
    }

    // Bu metod bir yorumun yanitlarini getirir.
    public List<YorumYaniti> YorumYanitlariniGetir(int kursYorumuId)
    {
        // Bu satir yanit sorgusunu calistirir.
        return _sqlite.SorguCalistir($"""
            SELECT yy.*, ku.AdSoyad AS KullaniciAdi, ku.Rol AS KullaniciRol
            FROM YorumYanitlari yy
            INNER JOIN Kullanicilar ku ON ku.Id = yy.KullaniciId
            WHERE yy.KursYorumuId = {kursYorumuId}
            ORDER BY yy.Id ASC;
            """)
            .Select(YorumYanitiDonustur)
            .ToList();
    }

    // Bu metod yayinli kurslari genel liste icin getirir.
    private List<KursKart> YayinliKurslariGetir()
    {
        // Bu satir yayinlanmis kurslari ortak sorguyla getirir.
        return KursKartSorgusu("WHERE k.YayinlandiMi = 1");
    }

    // Bu metod admin icin tum kurs kartlarini getirir.
    private List<KursKart> TumKursKartlariniGetir()
    {
        // Bu satir tum kurslari ortak sorguyla getirir.
        return KursKartSorgusu(string.Empty);
    }

    // Bu metod egitmenin kendi kurslarini getirir.
    private List<KursKart> EgitmenKurslariniGetir(int egitmenId)
    {
        // Bu satir egitmene ait kurslari ortak sorguyla getirir.
        return KursKartSorgusu($"WHERE k.EgitmenId = {egitmenId}");
    }

    // Bu metod ogrencinin gorecegi tum yayinli kurslari getirir.
    private List<KursKart> OgrenciIcinKurslariGetir(int ogrenciId)
    {
        // Bu satir yayinli kurslari ve sahiplik bilgisini getirir.
        return KursKartSorgusu("WHERE k.YayinlandiMi = 1", ogrenciId);
    }

    // Bu metod ogrencinin aldigi kurslari getirir.
    private List<KursKart> OgrencininAldigiKurslariGetir(int ogrenciId)
    {
        // Bu satir sadece ogrencinin satin aldigi kurslari getirir.
        return KursKartSorgusu($"INNER JOIN KursKayitlari kk ON kk.KursId = k.Id AND kk.OgrenciId = {ogrenciId}", ogrenciId);
    }

    // Bu metod tek kurs karti getirir.
    private KursKart? TekKursKartiniGetir(int kursId, int? aktifKullaniciId)
    {
        // Bu satir ortak sorguyu belirli kurs icin calistirir.
        return KursKartSorgusu($"WHERE k.Id = {kursId}", aktifKullaniciId).FirstOrDefault();
    }

    // Bu metod ortak kurs karti sorgusunu tek noktada toplar.
    private List<KursKart> KursKartSorgusu(string ekBolum, int? aktifKullaniciId = null)
    {
        // Bu satir sahiplik kontrolu icin gerekli alt sorguyu hazirlar.
        var sahiplikSorgusu = aktifKullaniciId.HasValue
            ? $"EXISTS (SELECT 1 FROM KursKayitlari kk2 WHERE kk2.KursId = k.Id AND kk2.OgrenciId = {aktifKullaniciId.Value})"
            : "0";

        // Bu satir ortak kurs sorgusunu calistirir.
        var satirlar = _sqlite.SorguCalistir($"""
            SELECT
                k.Id,
                k.EgitmenId,
                k.KategoriId,
                k.Baslik,
                k.Aciklama,
                k.VideoUrl,
                k.OnizlemeVideoUrl,
                k.DokumanUrl,
                k.Fiyat,
                k.YayinlandiMi,
                k.OlusturmaTarihi,
                ku.AdSoyad AS EgitmenAdi,
                ku.Unvan AS EgitmenUnvan,
                ka.Ad AS KategoriAdi,
                {sahiplikSorgusu} AS SahipMi,
                COALESCE((SELECT AVG(Puan) FROM KursYorumlari ky WHERE ky.KursId = k.Id), 0) AS OrtalamaPuan,
                COALESCE((SELECT COUNT(*) FROM KursYorumlari ky WHERE ky.KursId = k.Id), 0) AS YorumSayisi,
                COALESCE((SELECT COUNT(*) FROM KursBolumleri kb WHERE kb.KursId = k.Id), 0) AS BolumSayisi
            FROM Kurslar k
            INNER JOIN Kullanicilar ku ON ku.Id = k.EgitmenId
            INNER JOIN Kategoriler ka ON ka.Id = k.KategoriId
            {ekBolum}
            ORDER BY k.Id DESC;
            """);

        // Bu satir kayitlari kurs karti listesine cevirir.
        return satirlar.Select(satir => new KursKart
        {
            Kurs = KursDonustur(satir),
            EgitmenAdi = satir.GetValueOrDefault("EgitmenAdi") ?? string.Empty,
            KategoriAdi = satir.GetValueOrDefault("KategoriAdi") ?? string.Empty,
            SahipMi = satir.GetValueOrDefault("SahipMi") == "1",
            OrtalamaPuan = DecimalGetir(satir, "OrtalamaPuan"),
            YorumSayisi = IntGetir(satir, "YorumSayisi"),
            BolumSayisi = IntGetir(satir, "BolumSayisi")
        }).ToList();
    }

    // Bu metod tum kullanicilari getirir.
    private List<Kullanici> TumKullanicilariGetir()
    {
        // Bu satir kullanici sorgusunu calistirir.
        return _sqlite.SorguCalistir("SELECT * FROM Kullanicilar ORDER BY Id DESC;")
            .Select(KullaniciDonustur)
            .ToList();
    }

    // Bu metod kurs satirini modele cevirir.
    private static Kurs KursDonustur(Dictionary<string, string?> satir)
    {
        // Bu satir kurs nesnesini doldurup dondurur.
        return new Kurs
        {
            Id = IntGetir(satir, "Id"),
            EgitmenId = IntGetir(satir, "EgitmenId"),
            KategoriId = IntGetir(satir, "KategoriId"),
            Baslik = satir.GetValueOrDefault("Baslik") ?? string.Empty,
            Aciklama = satir.GetValueOrDefault("Aciklama") ?? string.Empty,
            VideoUrl = satir.GetValueOrDefault("VideoUrl") ?? satir.GetValueOrDefault("OnizlemeVideoUrl") ?? string.Empty,
            OnizlemeVideoUrl = satir.GetValueOrDefault("OnizlemeVideoUrl") ?? satir.GetValueOrDefault("VideoUrl") ?? string.Empty,
            DokumanUrl = satir.GetValueOrDefault("DokumanUrl") ?? string.Empty,
            Fiyat = DecimalGetir(satir, "Fiyat"),
            YayinlandiMi = satir.GetValueOrDefault("YayinlandiMi") == "1" || satir.GetValueOrDefault("YayinlandiMi")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
            OlusturmaTarihi = satir.GetValueOrDefault("OlusturmaTarihi") ?? string.Empty
        };
    }

    // Bu metod kurs bolum satirini modele cevirir.
    private static KursBolumu KursBolumuDonustur(Dictionary<string, string?> satir)
    {
        // Bu satir bolum nesnesini doldurup dondurur.
        return new KursBolumu
        {
            Id = IntGetir(satir, "Id"),
            KursId = IntGetir(satir, "KursId"),
            SiraNo = IntGetir(satir, "SiraNo"),
            Baslik = satir.GetValueOrDefault("Baslik") ?? string.Empty,
            Aciklama = satir.GetValueOrDefault("Aciklama") ?? string.Empty,
            VideoUrl = satir.GetValueOrDefault("VideoUrl") ?? string.Empty,
            DokumanUrl = satir.GetValueOrDefault("DokumanUrl") ?? string.Empty,
            OlusturmaTarihi = satir.GetValueOrDefault("OlusturmaTarihi") ?? string.Empty
        };
    }

    // Bu metod yorum satirini modele cevirir.
    private static KursYorumu KursYorumuDonustur(Dictionary<string, string?> satir)
    {
        // Bu satir yorum nesnesini doldurup dondurur.
        return new KursYorumu
        {
            Id = IntGetir(satir, "Id"),
            KursId = IntGetir(satir, "KursId"),
            OgrenciId = IntGetir(satir, "OgrenciId"),
            Puan = IntGetir(satir, "Puan"),
            Yorum = satir.GetValueOrDefault("Yorum") ?? string.Empty,
            Tarih = satir.GetValueOrDefault("Tarih") ?? string.Empty,
            OgrenciAdi = satir.GetValueOrDefault("OgrenciAdi") ?? string.Empty
        };
    }

    // Bu metod yorum yaniti satirini modele cevirir.
    private static YorumYaniti YorumYanitiDonustur(Dictionary<string, string?> satir)
    {
        // Bu satir yanit nesnesini doldurup dondurur.
        return new YorumYaniti
        {
            Id = IntGetir(satir, "Id"),
            KursYorumuId = IntGetir(satir, "KursYorumuId"),
            KullaniciId = IntGetir(satir, "KullaniciId"),
            Yanit = satir.GetValueOrDefault("Yanit") ?? string.Empty,
            Tarih = satir.GetValueOrDefault("Tarih") ?? string.Empty,
            KullaniciAdi = satir.GetValueOrDefault("KullaniciAdi") ?? string.Empty,
            KullaniciRol = satir.GetValueOrDefault("KullaniciRol") ?? string.Empty
        };
    }

    // Bu metod kategori satirini modele cevirir.
    private static Kategori KategoriDonustur(Dictionary<string, string?> satir)
    {
        // Bu satir kategori nesnesini doldurup dondurur.
        return new Kategori
        {
            Id = IntGetir(satir, "Id"),
            Ad = satir.GetValueOrDefault("Ad") ?? string.Empty,
            Aciklama = satir.GetValueOrDefault("Aciklama") ?? string.Empty
        };
    }

    // Bu metod kullanici satirini modele cevirir.
    private static Kullanici KullaniciDonustur(Dictionary<string, string?> satir)
    {
        // Bu satir kullanici nesnesini doldurup dondurur.
        return new Kullanici
        {
            Id = IntGetir(satir, "Id"),
            AdSoyad = satir.GetValueOrDefault("AdSoyad") ?? string.Empty,
            Eposta = satir.GetValueOrDefault("Eposta") ?? string.Empty,
            SifreHash = satir.GetValueOrDefault("SifreHash") ?? string.Empty,
            Rol = satir.GetValueOrDefault("Rol") ?? string.Empty,
            Unvan = satir.GetValueOrDefault("Unvan") ?? string.Empty,
            Hakkinda = satir.GetValueOrDefault("Hakkinda") ?? string.Empty,
            ProfilFotoUrl = satir.GetValueOrDefault("ProfilFotoUrl") ?? string.Empty,
            Bakiye = DecimalGetir(satir, "Bakiye"),
            KayitTarihi = satir.GetValueOrDefault("KayitTarihi") ?? string.Empty
        };
    }

    // Bu metod tek SQL sayi sonucunu getirir.
    private int SayiGetir(string sql)
    {
        // Bu satir ilk sonucu alir.
        var satir = _sqlite.SorguCalistir(sql).FirstOrDefault();

        // Bu satir sonucu tamsayiya cevirir.
        return int.TryParse(satir?.GetValueOrDefault("Toplam"), out var toplam) ? toplam : 0;
    }

    // Bu metod tek SQL ondalik sonucunu getirir.
    private decimal DecimalGetir(string sql)
    {
        // Bu satir ilk sonucu alir.
        var satir = _sqlite.SorguCalistir(sql).FirstOrDefault();

        // Bu satir sonucu decimal olarak dondurur.
        return decimal.TryParse(
            satir?.GetValueOrDefault("Toplam"),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var toplam)
            ? toplam
            : 0;
    }

    // Bu metod sozlukten tamsayi deger ceker.
    private static int IntGetir(Dictionary<string, string?> satir, string alanAdi)
    {
        // Bu satir int parse sonucu yoksa sifir dondurur.
        return int.TryParse(satir.GetValueOrDefault(alanAdi), out var sonuc) ? sonuc : 0;
    }

    // Bu metod sozlukten decimal deger ceker.
    private static decimal DecimalGetir(Dictionary<string, string?> satir, string alanAdi)
    {
        // Bu satir decimal parse sonucu yoksa sifir dondurur.
        return decimal.TryParse(
            satir.GetValueOrDefault(alanAdi),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var sonuc)
            ? sonuc
            : 0;
    }
}
