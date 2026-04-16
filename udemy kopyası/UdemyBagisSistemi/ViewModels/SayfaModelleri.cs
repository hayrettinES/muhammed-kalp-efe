// Bu dosya Razor gorunumlerinde kullanilan sayfa modellerini toplar.
using System.ComponentModel.DataAnnotations;
using UdemyBagisSistemi.Models;

namespace UdemyBagisSistemi.ViewModels;

// Bu model anasayfa verilerini tasir.
public class AnasayfaViewModel
{
    // Bu alan yayinlanmis kurs listesini tutar.
    public List<KursKart> YayinliKurslar { get; set; } = [];

    // Bu alan havuzda kalan toplam tutari tutar.
    public decimal HavuzBakiyesi { get; set; }

    // Bu alan toplam kategori sayisini tutar.
    public int ToplamKategoriSayisi { get; set; }

    // Bu alan toplam kurs sayisini tutar.
    public int ToplamKursSayisi { get; set; }

    // Bu alan toplam bagis sayisini tutar.
    public int ToplamBagisSayisi { get; set; }
}

// Bu model giris formunu tasir.
public class GirisViewModel
{
    // Bu alan eposta bilgisini tutar.
    [Required(ErrorMessage = "Eposta zorunludur.")]
    public string Eposta { get; set; } = string.Empty;

    // Bu alan sifre bilgisini tutar.
    [Required(ErrorMessage = "Sifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;
}

// Bu model kayit formunu tasir.
public class KayitViewModel
{
    // Bu alan ad soyad bilgisini tutar.
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    public string AdSoyad { get; set; } = string.Empty;

    // Bu alan eposta bilgisini tutar.
    [Required(ErrorMessage = "Eposta zorunludur.")]
    public string Eposta { get; set; } = string.Empty;

    // Bu alan sifre bilgisini tutar.
    [Required(ErrorMessage = "Sifre zorunludur.")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;

    // Bu alan kayit olacak kullanicinin rolunu tutar.
    [Required(ErrorMessage = "Rol secimi zorunludur.")]
    public string Rol { get; set; } = "Ogrenci";

    // Bu alan ogrenci icin egitim seviyesini tutar.
    public string EgitimSeviyesi { get; set; } = string.Empty;

    // Bu alan ogrenci icin ilgi alanlarini virgul ayracli olarak tutar.
    public string IlgiAlanlari { get; set; } = string.Empty;

    // Bu alan egitmen icin unvan / meslek bilgisini tutar.
    public string Unvan { get; set; } = string.Empty;

    // Bu alan egitmen icin biyografi / hakkinda metnini tutar.
    public string Hakkinda { get; set; } = string.Empty;

    // Bu alan egitmen icin deneyim yilini tutar.
    public int DeneyimYili { get; set; }

    // Bu alan egitmen icin uzmanlik alanlarini virgul ayracli olarak tutar.
    public string UzmanlikAlanlari { get; set; } = string.Empty;

    // Bu alan ogrencinin hedefini tutar.
    public string Hedef { get; set; } = string.Empty;

    // Bu alan ogrenciyi sisteme yonlendiren kaynagi tutar.
    public string Yonlendiren { get; set; } = string.Empty;

    // Bu alan egitmenin linkedin baglantisini tutar.
    public string LinkedinProfili { get; set; } = string.Empty;

    // Bu alan egitmenin kurs format tercihini tutar.
    public string KursFormati { get; set; } = string.Empty;

    // Bu alan egitmenin fiyatlandirma tercihini tutar.
    public string FiyatlandirmaTercihi { get; set; } = string.Empty;
}

// Bu model admin panel verilerini tasir.
public class AdminPanelViewModel
{
    // Bu alan havuz bakiyesini tutar.
    public decimal HavuzBakiyesi { get; set; }

    // Bu alan tum kategorileri tutar.
    public List<Kategori> Kategoriler { get; set; } = [];

    // Bu alan tum kullanicilari tutar.
    public List<Kullanici> Kullanicilar { get; set; } = [];

    // Bu alan tum kurs kartlarini tutar.
    public List<KursKart> Kurslar { get; set; } = [];

    // Bu alan duzenleme icin secilen kategoriyi tutar.
    public Kategori? DuzenlenenKategori { get; set; }

    // Bu alan toplam yorum sayisini tutar.
    public int ToplamYorumSayisi { get; set; }
}

// Bu model egitmen panel verilerini tasir.
public class EgitmenPanelViewModel
{
    // Bu alan kategori secim listesi icin kullanilir.
    public List<Kategori> Kategoriler { get; set; } = [];

    // Bu alan egitmenin mevcut kurslarini tutar.
    public List<KursKart> Kurslar { get; set; } = [];

    // Bu alan formda duzenlenecek kursu tutar.
    public Kurs? DuzenlenenKurs { get; set; }

    // Bu alan secili kursun bolumlerini tutar.
    public List<KursBolumu> Bolumler { get; set; } = [];

    // Bu alan egitmenin son basarili eylemlerini (kurs acma, yeni ogrenci gelmesi vb) tutar.
    public List<AktiviteOgesi> SonAktiviteler { get; set; } = [];

    // ── Dashboard istatistikleri ──

    // Egitmene ait kurslara toplam kayitli ogrenci sayisi (tekrarlanmayan).
    public int ToplamOgrenciSayisi { get; set; }

    // Bu ay kayit olan ogrenci sayisi.
    public int BuAyKatilanOgrenciSayisi { get; set; }

    // Egitmenin kurslarindan elde ettigi toplam gelir.
    public decimal ToplamGelir { get; set; }

    // Egitmene ait kurslarin aldigi toplam yorum sayisi (profil goruntu proxy'si).
    public int ToplamYorumSayisi { get; set; }

    // Her kurs icin kayitli ogrencilerin ilerleme listesi.
    public List<EgitmenKursOgrencileri> KursOgrencileri { get; set; } = [];

    // Egitmenin mevcut kisisel ve profesyonel profil ayarlarini tutar.
    public Kullanici Profil { get; set; } = new();
}

// Bu model ogrenci panel verilerini tasir.
public class OgrenciPanelViewModel
{
    // Bu alan havuz bakiyesini tutar.
    public decimal HavuzBakiyesi { get; set; }

    // Bu alan ogrencinin kendi bakiyesini tutar.
    public decimal OgrenciBakiyesi { get; set; }

    // Bu alan satin alinabilecek kurslari tutar.
    public List<KursKart> Kurslar { get; set; } = [];

    // Bu alan ogrencinin aldigi kurslari tutar.
    public List<KursKart> AldigiKurslar { get; set; } = [];

    // Bu alan kurs bazli ilerleme ozetlerini tutar.
    public List<KursIlerlemeOzeti> Ilerlemeler { get; set; } = [];

    // Bu alan ogrencinin kisisel profil bilgilerini tutar.
    public Kullanici Profil { get; set; } = new();
}

// Bu model kurs detay ekranini tasir.
public class KursDetayViewModel
{
    // Bu alan secili kurs kartini tutar.
    public KursKart KursKart { get; set; } = new();

    // Bu alan kursun bolumlerini tutar.
    public List<KursBolumu> Bolumler { get; set; } = [];

    // Bu alan kursa yazilan yorumlari tutar.
    public List<KursYorumu> Yorumlar { get; set; } = [];

    // Bu alan kullanicinin kursa sahip olup olmadigini belirtir.
    public bool SahipMi { get; set; }

    // Bu alan kullanicinin bu kursa yorum yapma hakki olup olmadigini belirtir.
    public bool YorumYapabilirMi { get; set; }

    // Bu alan kurs ilerleme ozetini tutar.
    public KursIlerlemeOzeti? IlerlemeOzeti { get; set; }

    // Bu alan giris yapan kullanicinin kimligini tutar.
    public int? AktifKullaniciId { get; set; }
}

// Bu model kullanicinin profil duzenleme ekranini tasir.
public class ProfilViewModel
{
    // Bu alan kullanici kimligini tutar.
    public int Id { get; set; }

    // Bu alan ad soyadi tutar.
    public string AdSoyad { get; set; } = string.Empty;

    // Bu alan epostayi tutar.
    public string Eposta { get; set; } = string.Empty;

    // Bu alan unvani tutar.
    public string Unvan { get; set; } = string.Empty;

    // Bu alan hakkinda metnini tutar.
    public string Hakkinda { get; set; } = string.Empty;

    // Bu alan mevcut profil foto yolunu tutar.
    public string ProfilFotoUrl { get; set; } = string.Empty;

    // Bu alan kullanicinin rolunu tutar.
    public string Rol { get; set; } = string.Empty;
}

// Bu model herkese acik bagis formunu tasir.
public class BagisSayfasiViewModel
{
    // Bu alan anlik havuz bakiyesini tutar.
    public decimal HavuzBakiyesi { get; set; }

    // Bu alan toplam bagis sayisini tutar.
    public int ToplamBagisSayisi { get; set; }
}

// Bu model profil tamamlama ekranini tasir.
public class ProfilTamamlaViewModel
{
    // Bu alan kullanicinin rolunu tutar (Ogrenci veya Egitmen).
    public string Rol { get; set; } = string.Empty;

    // ── Öğrenci alanları ──

    // Bu alan ogrencinin egitim seviyesini tutar.
    public string EgitimSeviyesi { get; set; } = string.Empty;

    // Bu alan ogrencinin secili ilgi alanlarini virgul ayracli tutar.
    public string IlgiAlanlari { get; set; } = string.Empty;

    // Bu alan ogrencinin secili kariyer hedefini tutar.
    public string Hedef { get; set; } = string.Empty;

    // Bu alan ogrenciyi sisteme kimin yonlendirdigini tutar.
    public string Yonlendiren { get; set; } = string.Empty;

    // ── Eğitmen alanları ──

    // Bu alan egitmenin unvan bilgisini tutar.
    public string Unvan { get; set; } = string.Empty;

    // Bu alan egitmenin biyografisini tutar.
    public string Hakkinda { get; set; } = string.Empty;

    // Bu alan egitmenin deneyim yilini tutar.
    public int DeneyimYili { get; set; }

    // Bu alan egitmenin uzmanlik alanlarini virgul ayracli tutar.
    public string UzmanlikAlanlari { get; set; } = string.Empty;

    // Bu alan egitmenin linkedin baglantisini tutar.
    public string LinkedinProfili { get; set; } = string.Empty;

    // Bu alan egitmenin kurs formati tercihini tutar.
    public string KursFormati { get; set; } = string.Empty;

    // Bu alan egitmenin fiyatlandirma tercihini tutar.
    public string FiyatlandirmaTercihi { get; set; } = string.Empty;
}

// Bu model öğrencinin kurs videolarını izlediği SPA ekranı için verileri taşır.
public class OgrenciKursIzlemeViewModel
{
    // İzlenilen kurs bilgisi.
    public Kurs Kurs { get; set; } = new();

    // Kursu oluşturan eğitmenin görünen adı.
    public string EgitmenAdi { get; set; } = string.Empty;

    // Kursa ait tüm bölümler (videolar).
    public List<KursBolumu> Bolumler { get; set; } = [];

    // Öğrencinin bu kursa ait hangi bölümleri tamamladığı vb. durumu.
    public List<BolumIlerlemesi> Ilerlemeler { get; set; } = [];
}
