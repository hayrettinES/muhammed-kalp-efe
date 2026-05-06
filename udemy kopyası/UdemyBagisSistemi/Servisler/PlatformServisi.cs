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

    // Bu alan eposta servisini tutar.
    private readonly IEpostaServisi _epostaServisi;

    // Bu kurucu servis bagimliliklarini alir.
    public PlatformServisi(SqliteKomutServisi sqlite, SifrelemeServisi sifreleme, IEpostaServisi epostaServisi)
    {
        // Bu satir sqlite bagimliligini saklar.
        _sqlite = sqlite;

        // Bu satir sifreleme bagimliligini saklar.
        _sifreleme = sifreleme;

        // Bu satir eposta bagimliligini saklar.
        _epostaServisi = epostaServisi;
    }

    // Bu metod anasayfa icin gerekli ozet verileri getirir.
    public AnasayfaViewModel AnasayfaVerisiniGetir()
    {
        var buAy = DateTime.Now.ToString("yyyy-MM");
        var toplamGelir = DecimalGetir("SELECT COALESCE(SUM(OdenenTutar), 0) AS Toplam FROM KursKayitlari;");
        var egitmenSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Kullanicilar WHERE Rol = 'Egitmen';");
        var ortalamaAylikKazanc = egitmenSayisi > 0 ? toplamGelir / egitmenSayisi : 0;

        var sonBagis = _sqlite.SorguCalistir("SELECT BagisciAdSoyad, Tutar FROM Bagislar WHERE OnaylandiMi = 1 ORDER BY Id DESC LIMIT 1;").FirstOrDefault();
        var sonBagisciAdi = sonBagis?.GetValueOrDefault("BagisciAdSoyad") ?? string.Empty;
        var sonBagisTutari = decimal.TryParse(sonBagis?.GetValueOrDefault("Tutar"), NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0m;

        // Bu satir model nesnesini doldurulmak uzere hazirlar.
        return new AnasayfaViewModel
        {
            HavuzBakiyesi = HavuzBakiyesiGetir(),
            ToplamKategoriSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Kategoriler;"),
            ToplamKursSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Kurslar WHERE YayinlandiMi = 1;"),
            ToplamBagisSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Bagislar WHERE OnaylandiMi = 1;"),
            YayinliKurslar = YayinliKurslariGetir(),
            BuAyKatilanOgrenciSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kullanicilar WHERE Rol = 'Ogrenci' AND KayitTarihi LIKE '{buAy}%';"),
            AktifOgrenciSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Kullanicilar WHERE Rol = 'Ogrenci';"),
            UzmanEgitmenSayisi = egitmenSayisi,
            TamamlananKursSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM KursKayitlari;"),
            OrtalamaAylikKazanc = ortalamaAylikKazanc,
            SonBagisciAdi = sonBagisciAdi,
            SonBagisTutari = sonBagisTutari,
            Kategoriler = KategorileriGetir()
        };
    }

    // Bu metod herkese acik bagis sayfasi verisini getirir.
    public BagisSayfasiViewModel BagisSayfasiVerisiniGetir()
    {
        // Bu satir bagis sayfasi ozet bilgisini dondurur.
        return new BagisSayfasiViewModel
        {
            HavuzBakiyesi = HavuzBakiyesiGetir(),
            ToplamBagisSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Bagislar WHERE OnaylandiMi = 1;")
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

    // Bu metod egitmen profiline ozgu tum alanlari gunceller. Sifre alani bos degilse sifreyi de gunceller.
    public void EgitmenProfilGuncelle(int kullaniciId, string adSoyad, string eposta, string unvan, string hakkinda, string profilFotoUrl, int deneyimYili, string uzmanlikAlanlari, string linkedinProfili, string kursFormati, string fiyatlandirmaTercihi, string? yeniSifreHash = null)
    {
        // Bu satir ayni epostanin baska kullanicida olup olmadigini kontrol eder.
        var cakisanKayit = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kullanicilar WHERE Eposta = '{_sqlite.MetinGuvenli(eposta)}' AND Id <> {kullaniciId};");
        if (cakisanKayit > 0)
            throw new InvalidOperationException("Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");

        var sifreGuncellemesi = string.IsNullOrWhiteSpace(yeniSifreHash) ? "" : $", SifreHash = '{_sqlite.MetinGuvenli(yeniSifreHash)}'";

        _sqlite.KomutCalistir($"""
            UPDATE Kullanicilar
            SET AdSoyad = '{_sqlite.MetinGuvenli(adSoyad)}',
                Eposta = '{_sqlite.MetinGuvenli(eposta)}'{sifreGuncellemesi},
                Unvan = '{_sqlite.MetinGuvenli(unvan)}',
                Hakkinda = '{_sqlite.MetinGuvenli(hakkinda)}',
                ProfilFotoUrl = '{_sqlite.MetinGuvenli(profilFotoUrl)}',
                DeneyimYili = {deneyimYili},
                UzmanlikAlanlari = '{_sqlite.MetinGuvenli(uzmanlikAlanlari)}',
                LinkedinProfili = '{_sqlite.MetinGuvenli(linkedinProfili)}',
                KursFormati = '{_sqlite.MetinGuvenli(kursFormati)}',
                FiyatlandirmaTercihi = '{_sqlite.MetinGuvenli(fiyatlandirmaTercihi)}'
            WHERE Id = {kullaniciId};
            """);
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
        if (!_sifreleme.Dogrula(sifre, kullanici.SifreHash))
            return null;

        return kullanici;
    }

    // Bu metod yeni ogrenci veya egitmen kaydi olusturur.
    public async Task<(bool Basarili, string Mesaj)> KayitOlAsync(KayitViewModel model)
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
        var unvan = string.IsNullOrWhiteSpace(model.Unvan)
            ? (model.Rol == "Egitmen" ? "Yeni Egitmen" : "Yeni Ogrenci")
            : model.Unvan;

        // Bu satir onay kodunu olusturur.
        var onayKodu = Guid.NewGuid().ToString("N");

        // Bu satir yeni kullaniciyi veritabanina ekler. (EpostaOnaylandiMi varsayilan 0)
        _sqlite.KomutCalistir($"""
            INSERT INTO Kullanicilar (AdSoyad, Eposta, SifreHash, Rol, Unvan, Hakkinda, ProfilFotoUrl,
                                     Bakiye, EpostaOnaylandiMi, EpostaOnayKodu, KayitTarihi,
                                     EgitimSeviyesi, IlgiAlanlari, DeneyimYili, UzmanlikAlanlari,
                                     Hedef, Yonlendiren, LinkedinProfili, KursFormati, FiyatlandirmaTercihi)
            VALUES (
                '{_sqlite.MetinGuvenli(model.AdSoyad)}',
                '{_sqlite.MetinGuvenli(model.Eposta)}',
                '{_sqlite.MetinGuvenli(sifreHash)}',
                '{_sqlite.MetinGuvenli(model.Rol)}',
                '{_sqlite.MetinGuvenli(unvan)}',
                '{_sqlite.MetinGuvenli(model.Hakkinda)}',
                '',
                0,
                0,
                '{_sqlite.MetinGuvenli(onayKodu)}',
                '{tarih}',
                '{_sqlite.MetinGuvenli(model.EgitimSeviyesi)}',
                '{_sqlite.MetinGuvenli(model.IlgiAlanlari)}',
                {model.DeneyimYili},
                '{_sqlite.MetinGuvenli(model.UzmanlikAlanlari)}',
                '{_sqlite.MetinGuvenli(model.Hedef)}',
                '{_sqlite.MetinGuvenli(model.Yonlendiren)}',
                '{_sqlite.MetinGuvenli(model.LinkedinProfili)}',
                '{_sqlite.MetinGuvenli(model.KursFormati)}',
                '{_sqlite.MetinGuvenli(model.FiyatlandirmaTercihi)}'
            );
            """);

        // Bu satir e-posta gonderir. TODO: Burada Host bilgisini alabilecegimiz IHttpContextAccessor eklenebilir, gecici olarak localhost:5003 kullanilacak.
        var dogrulamaUrl = $"http://localhost:5003/Hesap/Dogrula?eposta={Uri.EscapeDataString(model.Eposta)}&kod={Uri.EscapeDataString(onayKodu)}";
        var mesaj = $@"
            <div style='font-family: sans-serif;'>
                <h2>EduVerse'e Hos Geldiniz!</h2>
                <p>Üyeliğinizi tamamlamak için lütfen aşağıdaki linke tıklayarak e-posta adresinizi onaylayın:</p>
                <p><a href='{dogrulamaUrl}' style='display:inline-block;padding:10px 20px;background:#3dffc0;color:#1e1e1e;text-decoration:none;border-radius:6px;font-weight:bold;'>Hesabımı Doğrula</a></p>
                <p>Eğer buton çalışmıyorsa aşağıdaki linki tarayıcınıza yapıştırın:<br/>{dogrulamaUrl}</p>
            </div>";

        await _epostaServisi.EpostaGonderAsync(model.Eposta, "EduVerse - E-posta Doğrulama", mesaj);

        // Bu satir basarili sonuc dondurur.
        return (true, "Kayıt başarılı. Lütfen e-posta adresinize gönderilen doğrulama linkine tıklayınız.");
    }

    public (bool Basarili, string Mesaj) EpostaDogrula(string eposta, string kod)
    {
        var kullanici = KullaniciGetir(eposta);
        if (kullanici == null)
            return (false, "Kullanıcı bulunamadı.");

        if (kullanici.EpostaOnaylandiMi)
            return (true, "E-posta zaten onaylanmış.");

        if (kullanici.EpostaOnayKodu != kod)
            return (false, "Doğrulama kodu geçersiz.");

        _sqlite.KomutCalistir($"""
            UPDATE Kullanicilar
            SET EpostaOnaylandiMi = 1, EpostaOnayKodu = ''
            WHERE Id = {kullanici.Id};
            """);

        return (true, "E-posta adresiniz başarıyla onaylandı. Şimdi giriş yapabilirsiniz.");
    }

    public void EpostaDogrulaByGoogle(string eposta)
    {
        var kullanici = KullaniciGetir(eposta);
        if (kullanici != null)
        {
            _sqlite.KomutCalistir($"UPDATE Kullanicilar SET EpostaOnaylandiMi = 1, EpostaOnayKodu = '' WHERE Id = {kullanici.Id};");
        }
    }

    // Bu metod kullanicinin profil bilgilerinin eksik olup olmadigini kontrol eder.
    public bool ProfilEksikMi(Kullanici kullanici)
    {
        // Bu satir ogrenci icin zorunlu alanlari kontrol eder.
        if (kullanici.Rol == "Ogrenci")
        {
            return string.IsNullOrWhiteSpace(kullanici.EgitimSeviyesi)
                || string.IsNullOrWhiteSpace(kullanici.IlgiAlanlari)
                || string.IsNullOrWhiteSpace(kullanici.Hedef);
        }

        // Bu satir egitmen icin zorunlu alanlari kontrol eder.
        if (kullanici.Rol == "Egitmen")
        {
            return string.IsNullOrWhiteSpace(kullanici.Unvan)
                || kullanici.Unvan == "Yeni Egitmen"
                || string.IsNullOrWhiteSpace(kullanici.UzmanlikAlanlari)
                || kullanici.DeneyimYili == 0
                || string.IsNullOrWhiteSpace(kullanici.KursFormati)
                || string.IsNullOrWhiteSpace(kullanici.FiyatlandirmaTercihi);
        }

        // Bu satir admin ve diger roller icin profil tamamlamaya gerek olmadigi anlaminaddir.
        return false;
    }

    // Bu metod kullanicinin eksik profil bilgilerini tamamlar.
    public void ProfilTamamla(int kullaniciId, ProfilTamamlaViewModel model)
    {
        // Bu satir ogrenci profilini tamamlar.
        if (model.Rol == "Ogrenci")
        {
            _sqlite.KomutCalistir($"""
                UPDATE Kullanicilar
                SET EgitimSeviyesi = '{_sqlite.MetinGuvenli(model.EgitimSeviyesi)}',
                    IlgiAlanlari   = '{_sqlite.MetinGuvenli(model.IlgiAlanlari)}',
                    Hedef          = '{_sqlite.MetinGuvenli(model.Hedef)}',
                    Yonlendiren    = '{_sqlite.MetinGuvenli(model.Yonlendiren)}'
                WHERE Id = {kullaniciId};
                """);
        }
        // Bu satir egitmen profilini tamamlar.
        else if (model.Rol == "Egitmen")
        {
            _sqlite.KomutCalistir($"""
                UPDATE Kullanicilar
                SET Unvan            = '{_sqlite.MetinGuvenli(model.Unvan)}',
                    Hakkinda         = '{_sqlite.MetinGuvenli(model.Hakkinda)}',
                    DeneyimYili      = {model.DeneyimYili},
                    UzmanlikAlanlari = '{_sqlite.MetinGuvenli(model.UzmanlikAlanlari)}',
                    LinkedinProfili  = '{_sqlite.MetinGuvenli(model.LinkedinProfili)}',
                    KursFormati      = '{_sqlite.MetinGuvenli(model.KursFormati)}',
                    FiyatlandirmaTercihi = '{_sqlite.MetinGuvenli(model.FiyatlandirmaTercihi)}'
                WHERE Id = {kullaniciId};
                """);
        }
    }

    // Bu metod admin paneli verilerini toplar.
    public AdminPanelViewModel AdminPanelVerisiniGetir(int? kategoriId, string arama = "")
    {
        var buAy = DateTime.Now.ToString("yyyy-MM");

        // Toplam gelir
        var toplamGelir = DecimalGetir("SELECT COALESCE(SUM(OdenenTutar), 0) AS Toplam FROM KursKayitlari;");

        // Bağış istatistikleri
        var toplamBagisSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM Bagislar WHERE OnaylandiMi = 1;");
        var toplamBagisTutari = DecimalGetir("SELECT COALESCE(SUM(Tutar), 0) AS Toplam FROM Bagislar WHERE OnaylandiMi = 1;");

        // Bu ay kayıt sayısı
        var buAyKayitSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kullanicilar WHERE KayitTarihi LIKE '{buAy}%';");

        // Son bağışlar
        var bagisSatirlari = _sqlite.SorguCalistir("SELECT * FROM Bagislar ORDER BY Id DESC LIMIT 20;");
        var bagislar = bagisSatirlari.Select(s => new Bagis
        {
            Id = IntGetir(s, "Id"),
            BagisciAdSoyad = s.GetValueOrDefault("BagisciAdSoyad") ?? "",
            Tutar = decimal.TryParse(s.GetValueOrDefault("Tutar"), NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0,
            Tarih = s.GetValueOrDefault("Tarih") ?? "",
            OnaylandiMi = IntGetir(s, "OnaylandiMi") == 1
        }).ToList();

        return new AdminPanelViewModel
        {
            HavuzBakiyesi = HavuzBakiyesiGetir(),
            Kategoriler = KategorileriGetir(),
            Kullanicilar = TumKullanicilariGetir(),
            Kurslar = TumKursKartlariniGetir().Where(k => string.IsNullOrWhiteSpace(arama) || k.Kurs.Baslik.ToLower().Contains(arama.ToLower()) || k.Kurs.Aciklama.ToLower().Contains(arama.ToLower())).ToList(),
            DuzenlenenKategori = kategoriId.HasValue ? KategoriGetir(kategoriId.Value) : null,
            ToplamYorumSayisi = SayiGetir("SELECT COUNT(*) AS Toplam FROM KursYorumlari;"),
            ToplamGelir = toplamGelir,
            ToplamBagisSayisi = toplamBagisSayisi,
            ToplamBagisTutari = toplamBagisTutari,
            BuAyKayitSayisi = buAyKayitSayisi,
            Bagislar = bagislar
        };
    }

    // Admin: Kullanıcı silme
    public void KullaniciSil(int kullaniciId)
    {
        // Admin kendini silemesin
        var kullanici = _sqlite.SorguCalistir($"SELECT Rol FROM Kullanicilar WHERE Id = {kullaniciId};");
        if (kullanici.Count == 0) throw new InvalidOperationException("Kullanıcı bulunamadı.");
        if (kullanici[0].GetValueOrDefault("Rol") == "Admin") throw new InvalidOperationException("Admin hesabı silinemez.");

        // İlişkili verileri temizle
        _sqlite.KomutCalistir($"DELETE FROM SepetOgesi WHERE KullaniciId = {kullaniciId};");
        _sqlite.KomutCalistir($"DELETE FROM KursYorumlari WHERE OgrenciId = {kullaniciId};");
        _sqlite.KomutCalistir($"DELETE FROM BolumIlerlemeleri WHERE OgrenciId = {kullaniciId};");
        _sqlite.KomutCalistir($"DELETE FROM KursKayitlari WHERE OgrenciId = {kullaniciId};");
        _sqlite.KomutCalistir($"DELETE FROM Kullanicilar WHERE Id = {kullaniciId};");
    }

    // Admin: Kurs yayın durumu toggle
    public void KursYayinDurumDegistir(int kursId)
    {
        var kurs = _sqlite.SorguCalistir($"SELECT YayinlandiMi FROM Kurslar WHERE Id = {kursId};");
        if (kurs.Count == 0) throw new InvalidOperationException("Kurs bulunamadı.");

        var mevcutDurum = kurs[0].GetValueOrDefault("YayinlandiMi") == "1";
        var yeniDurum = mevcutDurum ? 0 : 1;
        _sqlite.KomutCalistir($"UPDATE Kurslar SET YayinlandiMi = {yeniDurum} WHERE Id = {kursId};");
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

        // Egitmene ait kurslara kayitli toplam tekil ogrenci sayisi.
        var toplamOgrenci = SayiGetir($@"
            SELECT COUNT(DISTINCT kk.OgrenciId) AS Toplam
            FROM KursKayitlari kk
            JOIN Kurslar k ON k.Id = kk.KursId
            WHERE k.EgitmenId = {egitmenId};");

        // Bu ay kayit olan ogrenci sayisi.
        var buAy = DateTime.Now.ToString("yyyy-MM");
        var buAyKatilan = SayiGetir($@"
            SELECT COUNT(DISTINCT kk.OgrenciId) AS Toplam
            FROM KursKayitlari kk
            JOIN Kurslar k ON k.Id = kk.KursId
            WHERE k.EgitmenId = {egitmenId}
              AND kk.Tarih LIKE '{buAy}%';");

        // Egitmenin kurslarindan kazandigi toplam gelir.
        var toplamGelir = DecimalGetir($@"
            SELECT COALESCE(SUM(kk.OdenenTutar), 0) AS Toplam
            FROM KursKayitlari kk
            JOIN Kurslar k ON k.Id = kk.KursId
            WHERE k.EgitmenId = {egitmenId};");

        // Egitmene ait kurslarin aldigi toplam yorum sayisi.
        var toplamYorum = SayiGetir($@"
            SELECT COUNT(*) AS Toplam
            FROM KursYorumlari ky
            JOIN Kurslar k ON k.Id = ky.KursId
            WHERE k.EgitmenId = {egitmenId};");

        // Bu satir egitmen modeli olusturur.
        return new EgitmenPanelViewModel
        {
            Kategoriler          = KategorileriGetir(),
            Kurslar              = EgitmenKurslariniGetir(egitmenId),
            DuzenlenenKurs       = seciliKurs,
            Bolumler             = seciliKurs is null ? [] : KursBolumleriniGetir(seciliKurs.Id),
            SonAktiviteler       = EgitmenAktiviteleriGetir(egitmenId),
            ToplamOgrenciSayisi  = toplamOgrenci,
            BuAyKatilanOgrenciSayisi = buAyKatilan,
            ToplamGelir          = toplamGelir,
            ToplamYorumSayisi    = toplamYorum,
            KursOgrencileri      = EgitmenKursOgrencileriniGetir(egitmenId),
            Profil               = KullaniciGetir(egitmenId) ?? new Kullanici()
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
                INSERT INTO Kurslar (EgitmenId, KategoriId, Baslik, Aciklama, VideoUrl, OnizlemeVideoUrl, DokumanUrl, ThumbnailUrl, Fiyat, YayinlandiMi, OlusturmaTarihi)
                VALUES (
                    {kurs.EgitmenId},
                    {kurs.KategoriId},
                    '{_sqlite.MetinGuvenli(kurs.Baslik)}',
                    '{_sqlite.MetinGuvenli(kurs.Aciklama)}',
                    '{_sqlite.MetinGuvenli(kurs.OnizlemeVideoUrl)}',
                    '{_sqlite.MetinGuvenli(kurs.OnizlemeVideoUrl)}',
                    '{_sqlite.MetinGuvenli(kurs.DokumanUrl)}',
                    '{_sqlite.MetinGuvenli(kurs.ThumbnailUrl)}',
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
                ThumbnailUrl = '{_sqlite.MetinGuvenli(kurs.ThumbnailUrl)}',
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

    // Bu metod mevcut bir kursu bolumunu gunceller.
    public void BolumGuncelle(int bolumId, int egitmenId, string baslik, string aciklama, string? yeniVideoUrl, string? yeniDokumanUrl)
    {
        // Bolumun egitmene ait oldugunu dogrula.
        var sahipMi = SayiGetir($@"
            SELECT COUNT(*) AS Toplam
            FROM KursBolumleri kb
            JOIN Kurslar k ON k.Id = kb.KursId
            WHERE kb.Id = {bolumId} AND k.EgitmenId = {egitmenId};");

        if (sahipMi == 0)
            throw new InvalidOperationException("Bu bölüm size ait değil veya bulunamadı.");

        // Video guncelleme: yeni dosya geldiyse kullan, gelmezse eskiyi koru.
        var videoSatiri = string.IsNullOrWhiteSpace(yeniVideoUrl) ? "" : $"VideoUrl = '{_sqlite.MetinGuvenli(yeniVideoUrl)}',";
        var dokumanSatiri = string.IsNullOrWhiteSpace(yeniDokumanUrl) ? "" : $"DokumanUrl = '{_sqlite.MetinGuvenli(yeniDokumanUrl)}',";

        _sqlite.KomutCalistir($"""
            UPDATE KursBolumleri
            SET Baslik     = '{_sqlite.MetinGuvenli(baslik)}',
                Aciklama   = '{_sqlite.MetinGuvenli(aciklama)}',
                {videoSatiri}
                {dokumanSatiri}
                SiraNo     = SiraNo
            WHERE Id = {bolumId};
            """);
    }

    // Bu metod bir kursu bolumunu siler.
    public void BolumSil(int bolumId, int egitmenId)
    {
        // Bolumun egitmene ait oldugunu dogrula.
        var sahipMi = SayiGetir($@"
            SELECT COUNT(*) AS Toplam
            FROM KursBolumleri kb
            JOIN Kurslar k ON k.Id = kb.KursId
            WHERE kb.Id = {bolumId} AND k.EgitmenId = {egitmenId};");

        if (sahipMi == 0)
            throw new InvalidOperationException("Bu bölüm size ait değil veya bulunamadı.");

        _sqlite.KomutCalistir($"DELETE FROM KursBolumleri WHERE Id = {bolumId};");
    }

    // Bu metod kursu tamamen siler (bolumler dahil).
    public void KursSil(int kursId, int egitmenId)
    {
        // Kursun egitmene ait oldugunu dogrula.
        var kurs = KursGetir(kursId, egitmenId);
        if (kurs is null)
            throw new InvalidOperationException("Kurs bulunamadı veya size ait değil.");

        _sqlite.KomutCalistir($"DELETE FROM KursBolumleri WHERE KursId = {kursId};");
        _sqlite.KomutCalistir($"DELETE FROM Kurslar WHERE Id = {kursId} AND EgitmenId = {egitmenId};");
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
            Ilerlemeler = aldigiKurslar.Select(x => KursIlerlemeOzetiGetir(x.Kurs.Id, ogrenciId)).ToList(),
            Profil = KullaniciGetir(ogrenciId) ?? new Kullanici()
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

    // Bu metod ögrenci paneline özel tam ekran eğitim izleme detaylarını hazırlayıp gönderir.
    public OgrenciKursIzlemeViewModel OgrenciKursIzlemeVerisiniGetir(int kursId, int ogrenciId)
    {
        // 1. Kursun varlığını ve öğrenciye aidiyetini kontrol et.
        var sahiplik = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursKayitlari WHERE OgrenciId = {ogrenciId} AND KursId = {kursId};");
        if (sahiplik == 0)
        {
            throw new InvalidOperationException("Bu kursa sahip değilsiniz veya bulunamadı.");
        }

        // 2. Kurs bilgilerini getir.
        var kursData = _sqlite.SorguCalistir($"SELECT * FROM Kurslar WHERE Id = {kursId}").FirstOrDefault();
        if (kursData is null) throw new InvalidOperationException("Kurs bulunamadı.");
        
        var egitmenAdi = _sqlite.SorguCalistir($"SELECT AdSoyad FROM Kullanicilar WHERE Id = {Convert.ToInt32(kursData["EgitmenId"])}").FirstOrDefault()?["AdSoyad"]?.ToString() ?? "Bilinmiyor";

        // 3. Kurs bölümlerini getir
        var bolumlerCikti = _sqlite.SorguCalistir($"SELECT * FROM KursBolumleri WHERE KursId = {kursId} ORDER BY SiraNo ASC");
        var bolumler = new List<KursBolumu>();
        foreach (var b in bolumlerCikti)
        {
            bolumler.Add(new KursBolumu
            {
                Id = Convert.ToInt32(b["Id"]),
                KursId = Convert.ToInt32(b["KursId"]),
                SiraNo = Convert.ToInt32(b["SiraNo"]),
                Baslik = b["Baslik"].ToString() ?? "",
                Aciklama = b["Aciklama"].ToString() ?? "",
                VideoUrl = b["VideoUrl"].ToString() ?? "",
                DokumanUrl = b["DokumanUrl"].ToString() ?? "",
                OlusturmaTarihi = b["OlusturmaTarihi"].ToString() ?? ""
            });
        }

        // 4. Öğrencinin bölüm ilerlemelerini getir
        var ilerlemelerCikti = _sqlite.SorguCalistir($"SELECT * FROM BolumIlerlemeleri WHERE KursId = {kursId} AND OgrenciId = {ogrenciId}");
        var ilerlemeler = new List<BolumIlerlemesi>();
        foreach (var i in ilerlemelerCikti)
        {
            ilerlemeler.Add(new BolumIlerlemesi
            {
                Id = Convert.ToInt32(i["Id"]),
                OgrenciId = Convert.ToInt32(i["OgrenciId"]),
                KursId = Convert.ToInt32(i["KursId"]),
                KursBolumuId = Convert.ToInt32(i["KursBolumuId"]),
                TamamlandiMi = Convert.ToInt32(i["TamamlandiMi"]) == 1,
                SonIzlemeTarihi = i["SonIzlemeTarihi"].ToString() ?? ""
            });
        }

        // 5. Bölüm yorumlarını getir
        var bolumYorumlariCikti = _sqlite.SorguCalistir($@"
            SELECT byo.*, k.AdSoyad as KullaniciAdi 
            FROM BolumYorumlari byo 
            JOIN Kullanicilar k ON byo.KullaniciId = k.Id 
            WHERE byo.KursBolumuId IN (SELECT Id FROM KursBolumleri WHERE KursId = {kursId})
            ORDER BY byo.Id DESC");
            
        var bolumYorumlari = new List<BolumYorumu>();
        foreach (var byo in bolumYorumlariCikti)
        {
            bolumYorumlari.Add(new BolumYorumu
            {
                Id = Convert.ToInt32(byo["Id"]),
                KursBolumuId = Convert.ToInt32(byo["KursBolumuId"]),
                KullaniciId = Convert.ToInt32(byo["KullaniciId"]),
                Yorum = byo["Yorum"].ToString() ?? "",
                Tarih = byo["Tarih"].ToString() ?? "",
                KullaniciAdi = byo["KullaniciAdi"].ToString() ?? "Bilinmiyor"
            });
        }

        return new OgrenciKursIzlemeViewModel
        {
            Kurs = new Kurs
            {
                Id = Convert.ToInt32(kursData["Id"]),
                Baslik = kursData["Baslik"].ToString() ?? "",
                Aciklama = kursData["Aciklama"].ToString() ?? "",
                OnizlemeVideoUrl = kursData["OnizlemeVideoUrl"].ToString() ?? ""
            },
            EgitmenAdi = egitmenAdi,
            Bolumler = bolumler,
            Ilerlemeler = ilerlemeler,
            BolumYorumlari = bolumYorumlari
        };
    }

    // Bu metod spesifik bir videoya (bölüme) yorum ekler.
    public void BolumYorumEkle(int kursBolumuId, int kullaniciId, string yorum)
    {
        var guvenliYorum = _sqlite.MetinGuvenli(yorum);
        _sqlite.KomutCalistir($"""
            INSERT INTO BolumYorumlari (KursBolumuId, KullaniciId, Yorum, Tarih, ParentId)
            VALUES ({kursBolumuId}, {kullaniciId}, '{guvenliYorum}', '{DateTime.Now:yyyy-MM-dd HH:mm}', 0);
        """);
    }

    // Bu metod bir yoruma yanıt ekler (eğitmen veya öğrenci).
    public void BolumYorumYanitEkle(int parentYorumId, int kullaniciId, string yanit)
    {
        // Önce parent yorumun bölüm ID'sini bul.
        var parentRow = _sqlite.SorguCalistir($"SELECT KursBolumuId FROM BolumYorumlari WHERE Id = {parentYorumId};").FirstOrDefault();
        if (parentRow is null)
            throw new InvalidOperationException("Yanıt verilecek yorum bulunamadı.");

        var kursBolumuId = Convert.ToInt32(parentRow["KursBolumuId"]);
        var guvenliYanit = _sqlite.MetinGuvenli(yanit);
        _sqlite.KomutCalistir($"""
            INSERT INTO BolumYorumlari (KursBolumuId, KullaniciId, Yorum, Tarih, ParentId)
            VALUES ({kursBolumuId}, {kullaniciId}, '{guvenliYanit}', '{DateTime.Now:yyyy-MM-dd HH:mm}', {parentYorumId});
        """);
    }

    // Bu metod belirli bir bölüme ait yorumları yanıtlarıyla birlikte hiyerarşik olarak döndürür.
    public List<BolumYorumu> BolumYorumlariniGetir(int kursBolumuId)
    {
        var sonuclar = _sqlite.SorguCalistir($@"
            SELECT byo.*, k.AdSoyad as KullaniciAdi, k.Rol as KullaniciRol
            FROM BolumYorumlari byo
            JOIN Kullanicilar k ON byo.KullaniciId = k.Id
            WHERE byo.KursBolumuId = {kursBolumuId}
            ORDER BY byo.Id ASC");

        var tumYorumlar = new List<BolumYorumu>();
        foreach (var row in sonuclar)
        {
            tumYorumlar.Add(new BolumYorumu
            {
                Id = Convert.ToInt32(row["Id"]),
                KursBolumuId = Convert.ToInt32(row["KursBolumuId"]),
                KullaniciId = Convert.ToInt32(row["KullaniciId"]),
                Yorum = row["Yorum"].ToString() ?? "",
                Tarih = row["Tarih"].ToString() ?? "",
                ParentId = int.TryParse(row.GetValueOrDefault("ParentId"), out var pid) ? pid : 0,
                KullaniciAdi = row["KullaniciAdi"].ToString() ?? "Bilinmiyor",
                KullaniciRol = row.GetValueOrDefault("KullaniciRol") ?? "Ogrenci"
            });
        }

        // Hiyerarşik yapıya dönüştür: kök yorumlar + yanıtları
        var kokYorumlar = tumYorumlar.Where(y => y.ParentId == 0).ToList();
        foreach (var kok in kokYorumlar)
        {
            kok.Yanitlar = tumYorumlar.Where(y => y.ParentId == kok.Id).OrderBy(y => y.Id).ToList();
        }
        return kokYorumlar;
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
            INSERT INTO Bagislar (BagisciAdSoyad, Tutar, Tarih, OnaylandiMi)
            VALUES (
                '{_sqlite.MetinGuvenli(bagisciAdSoyad)}',
                {_sqlite.SayiGuvenli(tutar)},
                '{DateTime.Now:yyyy-MM-dd HH:mm:ss}',
                0
            );
            """);
    }

    // Bu metod bekleyen bagisi onaylar.
    public void BagisOnayla(int bagisId)
    {
        _sqlite.KomutCalistir($"UPDATE Bagislar SET OnaylandiMi = 1 WHERE Id = {bagisId};");
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
    public string KursSatinAl(int kursId, int ogrenciId, bool askidanMi = false)
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

        string odemeYontemi;

        if (askidanMi)
        {
            if (havuzBakiyesi < kurs.Fiyat)
                throw new InvalidOperationException("Bağış havuzunda bu kursu karşılayacak yeterli bakiye bulunmuyor.");
            
            odemeYontemi = "Havuz";
        }
        else
        {
            if (ogrenciBakiyesi < kurs.Fiyat)
                throw new InvalidOperationException("Öğrenci bakiyeniz yetersiz.");
            
            odemeYontemi = "Bakiye";
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

        // Eğitmene ödeme yatırılır
        _sqlite.KomutCalistir($"""
            UPDATE Kullanicilar
            SET Bakiye = COALESCE(Bakiye, 0) + {_sqlite.SayiGuvenli(kurs.Fiyat)}
            WHERE Id = {kurs.EgitmenId};
            """);

        // Bu satir kullaniciya sonuc mesajini doner.
        return odemeYontemi == "Havuz"
            ? "Kurs ödemesi bağış havuzundan karşılandı."
            : "Kurs ücreti bakiyenizden düşüldü.";
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
        var bagisToplami = DecimalGetir("SELECT COALESCE(SUM(Tutar), 0) AS Toplam FROM Bagislar WHERE OnaylandiMi = 1;");

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
                k.ThumbnailUrl,
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
            ThumbnailUrl = satir.GetValueOrDefault("ThumbnailUrl") ?? string.Empty,
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
            EpostaOnaylandiMi = satir.GetValueOrDefault("EpostaOnaylandiMi") == "1",
            EpostaOnayKodu = satir.GetValueOrDefault("EpostaOnayKodu") ?? string.Empty,
            KayitTarihi = satir.GetValueOrDefault("KayitTarihi") ?? string.Empty,
            EgitimSeviyesi = satir.GetValueOrDefault("EgitimSeviyesi") ?? string.Empty,
            IlgiAlanlari = satir.GetValueOrDefault("IlgiAlanlari") ?? string.Empty,
            DeneyimYili = IntGetir(satir, "DeneyimYili"),
            UzmanlikAlanlari = satir.GetValueOrDefault("UzmanlikAlanlari") ?? string.Empty,
            Hedef = satir.GetValueOrDefault("Hedef") ?? string.Empty,
            Yonlendiren = satir.GetValueOrDefault("Yonlendiren") ?? string.Empty,
            LinkedinProfili = satir.GetValueOrDefault("LinkedinProfili") ?? string.Empty,
            KursFormati = satir.GetValueOrDefault("KursFormati") ?? string.Empty,
            FiyatlandirmaTercihi = satir.GetValueOrDefault("FiyatlandirmaTercihi") ?? string.Empty
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

    // Bu metod egitmenin gerceklestirdigi ve egitmeni ilgilendiren son etkinlikleri getirir.
    public List<AktiviteOgesi> EgitmenAktiviteleriGetir(int egitmenId)
    {
        var aktiviteler = new List<AktiviteOgesi>();

        // 1. Yayina alinan kurslar (Kurslar Tablosu)
        var yayindakiler = _sqlite.SorguCalistir($@"
            SELECT Baslik, OlusturmaTarihi 
            FROM Kurslar 
            WHERE EgitmenId = {egitmenId} AND YayinlandiMi = 1;
        ");
        foreach(var satir in yayindakiler)
        {
            var tarih = satir.GetValueOrDefault("OlusturmaTarihi") ?? string.Empty;
            if(!string.IsNullOrEmpty(tarih))
            {
                aktiviteler.Add(new AktiviteOgesi {
                    Metin = $"<strong>{satir.GetValueOrDefault("Baslik")}</strong> isimli eğitim setiniz yayınlandı ve öğrencilere açıldı.",
                    Tarih = tarih,
                    IkonRengi = "" // default
                });
            }
        }

        // 2. Kurslara kayit olan yeni ogrenciler (KursKayitlari Tablosu)
        // EgitmenId'ye ait kurslarin id'lerinden kayitlara ulasilir.
        var kayitlar = _sqlite.SorguCalistir($@"
            SELECT k.Baslik, kk.Tarih 
            FROM KursKayitlari kk
            JOIN Kurslar k ON k.Id = kk.KursId
            WHERE k.EgitmenId = {egitmenId};
        ");
        foreach(var satir in kayitlar)
        {
            var tarih = satir.GetValueOrDefault("Tarih") ?? string.Empty;
            if(!string.IsNullOrEmpty(tarih))
            {
                aktiviteler.Add(new AktiviteOgesi {
                    Metin = $"<strong>{satir.GetValueOrDefault("Baslik")}</strong> eğitiminize yeni bir öğrenci katılım sağladı.",
                    Tarih = tarih,
                    IkonRengi = "copper" 
                });
            }
        }

        // 3. Profil Tanimlamasi (Kullanicilar Tablosu)
        var egitmenKaydi = _sqlite.SorguCalistir($@"
            SELECT KayitTarihi FROM Kullanicilar WHERE Id = {egitmenId};
        ").FirstOrDefault();
        if(egitmenKaydi != null && egitmenKaydi.TryGetValue("KayitTarihi", out var eTarih) && !string.IsNullOrEmpty(eTarih))
        {
            aktiviteler.Add(new AktiviteOgesi {
                Metin = "Eğitmen paneliniz aktifleşti ve profiliniz başarıyla onaylandı.",
                Tarih = eTarih,
                IkonRengi = ""
            });
        }

        // Listeyi tarihe gore sondan basa dogru sirala ve sadece son 15'ini al.
        return aktiviteler.OrderByDescending(a => a.Tarih).Take(15).ToList();
    }

    // Bu metod egitmene ait tum kursları ve o kursa kayitli ogrencilerin ilerleme ve temel bilgilerini getirir.
    public List<EgitmenKursOgrencileri> EgitmenKursOgrencileriniGetir(int egitmenId)
    {
        var egitmeninKurslari = _sqlite.SorguCalistir($@"
            SELECT Id, Baslik, YayinlandiMi
            FROM Kurslar
            WHERE EgitmenId = {egitmenId};
        ");

        var liste = new List<EgitmenKursOgrencileri>();

        foreach (var kursSatir in egitmeninKurslari)
        {
            var kursId = int.TryParse(kursSatir.GetValueOrDefault("Id"), out var id) ? id : 0;
            var toplamBolum = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursBolumleri WHERE KursId = {kursId};");

            var ogrenciSatirlari = _sqlite.SorguCalistir($@"
                SELECT 
                    ku.Id AS OgrenciId, 
                    ku.AdSoyad, 
                    ku.Eposta, 
                    kk.Tarih AS KayitTarihi
                FROM KursKayitlari kk
                JOIN Kullanicilar ku ON ku.Id = kk.OgrenciId
                WHERE kk.KursId = {kursId};
            ");

            var ogrenciListesi = new List<KursOgrencisi>();

            foreach (var os in ogrenciSatirlari)
            {
                var oId = int.TryParse(os.GetValueOrDefault("OgrenciId"), out var oid) ? oid : 0;
                var tamamlananCount = SayiGetir($@"
                    SELECT COUNT(*) AS Toplam FROM BolumIlerlemeleri 
                    WHERE OgrenciId = {oId} AND KursId = {kursId} AND TamamlandiMi = 1;
                ");

                ogrenciListesi.Add(new KursOgrencisi
                {
                    OgrenciId = oId,
                    AdSoyad = os.GetValueOrDefault("AdSoyad") ?? "Bilinmiyor",
                    Eposta = os.GetValueOrDefault("Eposta") ?? "Bilinmiyor",
                    KayitTarihi = os.GetValueOrDefault("KayitTarihi") ?? "",
                    TamamlananBolum = tamamlananCount,
                    ToplamBolum = toplamBolum
                });
            }

            liste.Add(new EgitmenKursOgrencileri
            {
                KursId = kursId,
                KursBaslik = kursSatir.GetValueOrDefault("Baslik") ?? "Isimsiz Egitim",
                YayinlandiMi = kursSatir.GetValueOrDefault("YayinlandiMi") == "1",
                Ogrenciler = ogrenciListesi.OrderByDescending(x => x.IlerlemeYuzdesi).ToList()
            });
        }

        return liste.OrderByDescending(x => x.Ogrenciler.Count).ToList();
    }

    // ── AGENT TOOL METOTLARI ──────────────────────────────────────

    // Bu metod agent'ın platformdaki kursları aramasını sağlar.
    public string AgentKursAra(string aramaMetni)
    {
        var guvenliMetin = _sqlite.MetinGuvenli(aramaMetni);
        var satirlar = _sqlite.SorguCalistir($"""
            SELECT k.Id, k.Baslik, k.Aciklama, k.Fiyat, k.YayinlandiMi,
                   kat.Ad AS KategoriAdi,
                   ku.AdSoyad AS EgitmenAdi,
                   (SELECT COUNT(*) FROM KursBolumleri WHERE KursId = k.Id) AS BolumSayisi,
                   (SELECT COUNT(*) FROM KursKayitlari WHERE KursId = k.Id) AS KayitliOgrenci,
                   (SELECT ROUND(AVG(Puan),1) FROM KursYorumlari WHERE KursId = k.Id) AS OrtPuan
            FROM Kurslar k
            LEFT JOIN Kategoriler kat ON kat.Id = k.KategoriId
            LEFT JOIN Kullanicilar ku ON ku.Id = k.EgitmenId
            WHERE k.YayinlandiMi = 1
              AND (k.Baslik LIKE '%{guvenliMetin}%' OR k.Aciklama LIKE '%{guvenliMetin}%' OR kat.Ad LIKE '%{guvenliMetin}%')
            ORDER BY k.Id DESC
            LIMIT 10;
            """);

        if (satirlar.Count == 0)
            return "Aramayla eşleşen yayınlanmış kurs bulunamadı.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Bulunan kurslar ({satirlar.Count} adet):");
        foreach (var s in satirlar)
        {
            var fiyat = s.GetValueOrDefault("Fiyat") ?? "0";
            var puan = s.GetValueOrDefault("OrtPuan") ?? "-";
            sb.AppendLine($"- [ID:{s.GetValueOrDefault("Id")}] \"{s.GetValueOrDefault("Baslik")}\" | Kategori: {s.GetValueOrDefault("KategoriAdi")} | Eğitmen: {s.GetValueOrDefault("EgitmenAdi")} | Fiyat: {fiyat}₺ | {s.GetValueOrDefault("BolumSayisi")} bölüm | {s.GetValueOrDefault("KayitliOgrenci")} öğrenci | Puan: {puan}");
        }
        return sb.ToString();
    }

    // Bu metod agent'ın tüm kategorileri listelemesini sağlar.
    public string AgentKategorileriListele()
    {
        var kategoriler = KategorileriGetir();
        if (kategoriler.Count == 0)
            return "Sistemde henüz kategori yok.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Platformdaki kategoriler ({kategoriler.Count} adet):");
        foreach (var k in kategoriler)
        {
            // Her kategoride kaç yayınlı kurs var?
            var kursSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kurslar WHERE KategoriId = {k.Id} AND YayinlandiMi = 1;");
            sb.AppendLine($"- {k.Ad} ({kursSayisi} kurs)");
        }
        return sb.ToString();
    }

    // Bu metod agent'ın giriş yapmış kullanıcının profil bilgilerini özetlemesini sağlar.
    public string AgentKullaniciBilgisiGetir(int kullaniciId)
    {
        var kullanici = KullaniciGetir(kullaniciId);
        if (kullanici is null)
            return "Kullanıcı bilgisi bulunamadı.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Kullanıcı Bilgileri:");
        sb.AppendLine($"- Ad: {kullanici.AdSoyad}");
        sb.AppendLine($"- Rol: {kullanici.Rol}");

        if (kullanici.Rol == "Ogrenci")
        {
            sb.AppendLine($"- Eğitim Seviyesi: {(string.IsNullOrWhiteSpace(kullanici.EgitimSeviyesi) ? "Belirtilmemiş" : kullanici.EgitimSeviyesi)}");
            sb.AppendLine($"- İlgi Alanları: {(string.IsNullOrWhiteSpace(kullanici.IlgiAlanlari) ? "Belirtilmemiş" : kullanici.IlgiAlanlari)}");
            sb.AppendLine($"- Hedef: {(string.IsNullOrWhiteSpace(kullanici.Hedef) ? "Belirtilmemiş" : kullanici.Hedef)}");

            var alinanKursSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursKayitlari WHERE OgrenciId = {kullaniciId};");
            sb.AppendLine($"- Aldığı Kurs Sayısı: {alinanKursSayisi}");

            if (alinanKursSayisi > 0)
            {
                var alinanKurslar = _sqlite.SorguCalistir($"""
                    SELECT k.Baslik
                    FROM KursKayitlari kk
                    JOIN Kurslar k ON k.Id = kk.KursId
                    WHERE kk.OgrenciId = {kullaniciId}
                    ORDER BY kk.Tarih DESC
                    LIMIT 5;
                    """);
                sb.AppendLine($"- Son Aldığı Kurslar: {string.Join(", ", alinanKurslar.Select(x => x.GetValueOrDefault("Baslik")))}");
            }
        }
        else if (kullanici.Rol == "Egitmen")
        {
            sb.AppendLine($"- Uzmanlık Alanları: {(string.IsNullOrWhiteSpace(kullanici.UzmanlikAlanlari) ? "Belirtilmemiş" : kullanici.UzmanlikAlanlari)}");
            sb.AppendLine($"- Deneyim: {kullanici.DeneyimYili} yıl");
            sb.AppendLine($"- Kurs Formatı: {(string.IsNullOrWhiteSpace(kullanici.KursFormati) ? "Belirtilmemiş" : kullanici.KursFormati)}");

            var kursSayisi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kurslar WHERE EgitmenId = {kullaniciId};");
            sb.AppendLine($"- Oluşturduğu Kurs Sayısı: {kursSayisi}");
        }

        return sb.ToString();
    }

    // Bu metod agent'ın tüm yayınlı kursları listelemesini sağlar (roadmap için).
    public string AgentTumKurslariListele()
    {
        var satirlar = _sqlite.SorguCalistir("""
            SELECT k.Id, k.Baslik, k.Fiyat,
                   kat.Ad AS KategoriAdi,
                   ku.AdSoyad AS EgitmenAdi,
                   (SELECT COUNT(*) FROM KursBolumleri WHERE KursId = k.Id) AS BolumSayisi
            FROM Kurslar k
            LEFT JOIN Kategoriler kat ON kat.Id = k.KategoriId
            LEFT JOIN Kullanicilar ku ON ku.Id = k.EgitmenId
            WHERE k.YayinlandiMi = 1
            ORDER BY k.Id DESC
            LIMIT 20;
            """);

        if (satirlar.Count == 0)
            return "Platformda henüz yayınlanmış kurs yok.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Platformdaki yayınlı kurslar ({satirlar.Count} adet):");
        foreach (var s in satirlar)
        {
            sb.AppendLine($"- [ID:{s.GetValueOrDefault("Id")}] \"{s.GetValueOrDefault("Baslik")}\" | {s.GetValueOrDefault("KategoriAdi")} | {s.GetValueOrDefault("EgitmenAdi")} | {s.GetValueOrDefault("Fiyat")}₺ | {s.GetValueOrDefault("BolumSayisi")} bölüm");
        }
        return sb.ToString();
    }

    // ── SEPET İŞLEMLERİ ───────────────────────────────────────────

    public void SepeteEkle(int kullaniciId, int kursId, string odemeYontemi = "Bakiye")
    {
        // Kurs alınmış mı kontrolü
        var alindiMi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM KursKayitlari WHERE OgrenciId = {kullaniciId} AND KursId = {kursId};");
        if (alindiMi > 0)
        {
            throw new InvalidOperationException("Bu kursu zaten satın aldınız.");
        }

        // Sepette var mı kontrolü
        var sepetteMi = SayiGetir($"SELECT COUNT(*) AS Toplam FROM SepetOgesi WHERE KullaniciId = {kullaniciId} AND KursId = {kursId};");
        if (sepetteMi > 0)
        {
            throw new InvalidOperationException("Bu kurs zaten sepetinizde.");
        }

        // Kendi kursunu mu alıyor kontrolü
        var kendiKursuMu = SayiGetir($"SELECT COUNT(*) AS Toplam FROM Kurslar WHERE Id = {kursId} AND EgitmenId = {kullaniciId};");
        if (kendiKursuMu > 0)
        {
            throw new InvalidOperationException("Kendi oluşturduğunuz kursu sepete ekleyemezsiniz.");
        }

        var guvenliOdeme = odemeYontemi == "Havuz" ? "Havuz" : "Bakiye";
        _sqlite.KomutCalistir($"""
            INSERT INTO SepetOgesi (KullaniciId, KursId, EklenmeTarihi, OdemeYontemi)
            VALUES ({kullaniciId}, {kursId}, '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', '{guvenliOdeme}');
            """);
    }

    public void SepettenCikar(int kullaniciId, int kursId)
    {
        _sqlite.KomutCalistir($"DELETE FROM SepetOgesi WHERE KullaniciId = {kullaniciId} AND KursId = {kursId};");
    }

    public int SepetUrunSayisi(int kullaniciId)
    {
        return SayiGetir($"SELECT COUNT(*) AS Toplam FROM SepetOgesi WHERE KullaniciId = {kullaniciId};");
    }

    public SepetViewModel SepetiGetir(int kullaniciId)
    {
        var kurslar = _sqlite.SorguCalistir($"""
            SELECT k.Id, k.Baslik, k.Aciklama, k.Fiyat, k.YayinlandiMi, k.DokumanUrl,
                   kat.Ad AS KategoriAdi,
                   ku.AdSoyad AS EgitmenAdi
            FROM SepetOgesi s
            INNER JOIN Kurslar k ON k.Id = s.KursId
            LEFT JOIN Kategoriler kat ON kat.Id = k.KategoriId
            LEFT JOIN Kullanicilar ku ON ku.Id = k.EgitmenId
            WHERE s.KullaniciId = {kullaniciId}
            ORDER BY s.Id DESC;
            """)
            .Select(s => new KursKart
            {
                Kurs = new Kurs
                {
                    Id = IntGetir(s, "Id"),
                    Baslik = s.GetValueOrDefault("Baslik") ?? "",
                    Aciklama = s.GetValueOrDefault("Aciklama") ?? "",
                    Fiyat = decimal.TryParse(s.GetValueOrDefault("Fiyat"), NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : 0,
                    YayinlandiMi = s.GetValueOrDefault("YayinlandiMi") == "1",
                    DokumanUrl = s.GetValueOrDefault("DokumanUrl") ?? ""
                },
                EgitmenAdi = s.GetValueOrDefault("EgitmenAdi") ?? "",
                KategoriAdi = s.GetValueOrDefault("KategoriAdi") ?? ""
            })
            .ToList();

        return new SepetViewModel
        {
            SepettekiKurslar = kurslar
        };
    }

    public string SepetiSatinAl(int kullaniciId)
    {
        var sepet = SepetiGetir(kullaniciId);
        if (sepet.SepettekiKurslar.Count == 0)
        {
            throw new InvalidOperationException("Sepetiniz boş.");
        }

        int basariliSayisi = 0;
        int basarisizSayisi = 0;

        foreach (var kurs in sepet.SepettekiKurslar)
        {
            try
            {
                KursSatinAl(kurs.Kurs.Id, kullaniciId);
                // Satın alma başarılı olursa sepetten çıkar
                SepettenCikar(kullaniciId, kurs.Kurs.Id);
                basariliSayisi++;
            }
            catch
            {
                basarisizSayisi++;
            }
        }

        if (basarisizSayisi == 0)
            return $"{basariliSayisi} kurs başarıyla satın alındı.";
        else if (basariliSayisi == 0)
            throw new InvalidOperationException("Bakiye veya havuz yetersiz olduğu için hiçbir kurs satın alınamadı.");
        else
            return $"{basariliSayisi} kurs başarıyla satın alındı, ancak {basarisizSayisi} kurs için bakiye yetersiz kaldı (sepette bırakıldı).";
    }

    public string AgentSepeteEkle(int kullaniciId, int kursId)
    {
        try
        {
            SepeteEkle(kullaniciId, kursId);
            return "Kurs başarıyla sepete eklendi!";
        }
        catch (Exception ex)
        {
            return $"Kurs sepete eklenemedi: {ex.Message}";
        }
    }

    // ── AI SOHBET YÖNETİMİ ───────────────────────────────────────

    // Bu metod havuzdaki en az kullanılan aktif API anahtarını döndürür (round-robin).
    public string AiApiAnahtariGetir()
    {
        var satir = _sqlite.SorguCalistir("""
            SELECT ApiKey FROM AiApiAnahtarlari
            WHERE Aktif = 1
            ORDER BY KullanmaSayisi ASC
            LIMIT 1;
            """).FirstOrDefault();

        if (satir is null)
            throw new InvalidOperationException("AI API anahtarı bulunamadı.");

        var key = satir.GetValueOrDefault("ApiKey") ?? string.Empty;

        // Kullanma sayısını artır
        _sqlite.KomutCalistir($"UPDATE AiApiAnahtarlari SET KullanmaSayisi = KullanmaSayisi + 1 WHERE ApiKey = '{_sqlite.MetinGuvenli(key)}';");

        return key;
    }

    // Bu metod kullanıcı için yeni bir sohbet oturumu oluşturur.
    public int AiSohbetOlustur(int kullaniciId, string baslik = "Yeni Sohbet")
    {
        var tarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _sqlite.KomutCalistir($"""
            INSERT INTO AiSohbetleri (KullaniciId, Baslik, OlusturmaTarihi, SonGuncellemeTarihi)
            VALUES ({kullaniciId}, '{_sqlite.MetinGuvenli(baslik)}', '{tarih}', '{tarih}');
            """);

        return SayiGetir("SELECT Id AS Toplam FROM AiSohbetleri ORDER BY Id DESC LIMIT 1;");
    }

    // Bu metod kullanıcının tüm sohbet oturumlarını listeler.
    public List<Dictionary<string, string?>> AiSohbetleriGetir(int kullaniciId)
    {
        return _sqlite.SorguCalistir($"""
            SELECT Id, Baslik, OlusturmaTarihi, SonGuncellemeTarihi
            FROM AiSohbetleri
            WHERE KullaniciId = {kullaniciId}
            ORDER BY SonGuncellemeTarihi DESC;
            """);
    }

    // Bu metod sohbet başlığını günceller.
    public void AiSohbetBasligiGuncelle(int sohbetId, int kullaniciId, string yeniBaslik)
    {
        _sqlite.KomutCalistir($"""
            UPDATE AiSohbetleri
            SET Baslik = '{_sqlite.MetinGuvenli(yeniBaslik)}',
                SonGuncellemeTarihi = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            WHERE Id = {sohbetId} AND KullaniciId = {kullaniciId};
            """);
    }

    // Bu metod sohbet oturumunu siler.
    public void AiSohbetSil(int sohbetId, int kullaniciId)
    {
        // Önce mesajları sil
        _sqlite.KomutCalistir($"DELETE FROM AiMesajlari WHERE SohbetId = {sohbetId};");
        // Sonra sohbeti sil
        _sqlite.KomutCalistir($"DELETE FROM AiSohbetleri WHERE Id = {sohbetId} AND KullaniciId = {kullaniciId};");
    }

    // Bu metod sohbete mesaj ekler.
    public void AiMesajEkle(int sohbetId, string rol, string icerik)
    {
        var tarih = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _sqlite.KomutCalistir($"""
            INSERT INTO AiMesajlari (SohbetId, Rol, Icerik, Tarih)
            VALUES ({sohbetId}, '{_sqlite.MetinGuvenli(rol)}', '{_sqlite.MetinGuvenli(icerik)}', '{tarih}');
            """);

        // Sohbetin son güncelleme tarihini güncelle
        _sqlite.KomutCalistir($"UPDATE AiSohbetleri SET SonGuncellemeTarihi = '{tarih}' WHERE Id = {sohbetId};");
    }

    // Bu metod bir sohbetin tüm mesajlarını getirir.
    public List<Dictionary<string, string?>> AiMesajlariGetir(int sohbetId)
    {
        return _sqlite.SorguCalistir($"""
            SELECT Id, Rol, Icerik, Tarih
            FROM AiMesajlari
            WHERE SohbetId = {sohbetId}
            ORDER BY Id ASC;
            """);
    }

    // Bu metod sohbetin sahibini doğrular.
    public bool AiSohbetSahibiMi(int sohbetId, int kullaniciId)
    {
        return SayiGetir($"SELECT COUNT(*) AS Toplam FROM AiSohbetleri WHERE Id = {sohbetId} AND KullaniciId = {kullaniciId};") > 0;
    }

    // Bu metod ilk mesajdan otomatik başlık üretir (ilk 50 karakter).
    public void AiSohbetBasligiOtomatikAyarla(int sohbetId, int kullaniciId, string ilkMesaj)
    {
        var baslik = ilkMesaj.Length > 50 ? ilkMesaj[..50] + "..." : ilkMesaj;
        baslik = baslik.Replace("\n", " ").Replace("\r", " ").Trim();
        AiSohbetBasligiGuncelle(sohbetId, kullaniciId, baslik);
    }
}
