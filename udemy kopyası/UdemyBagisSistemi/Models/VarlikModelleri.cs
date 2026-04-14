// Bu dosya uygulamada kullanilan temel veri modellerini tutar.
namespace UdemyBagisSistemi.Models;

// Bu model sistemdeki kullanici bilgisini temsil eder.
public class Kullanici
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan kullanicinin gorunen adini tutar.
    public string AdSoyad { get; set; } = string.Empty;

    // Bu alan giris icin kullanilan eposta bilgisini tutar.
    public string Eposta { get; set; } = string.Empty;

    // Bu alan sifrenin guvenli ozet degerini tutar.
    public string SifreHash { get; set; } = string.Empty;

    // Bu alan kullanicinin rolunu tutar.
    public string Rol { get; set; } = string.Empty;

    // Bu alan kullanicinin kisa unvanini tutar.
    public string Unvan { get; set; } = string.Empty;

    // Bu alan kullanicinin hakkinda yazisini tutar.
    public string Hakkinda { get; set; } = string.Empty;

    // Bu alan kullanicinin profil fotografi yolunu tutar.
    public string ProfilFotoUrl { get; set; } = string.Empty;

    // Bu alan kullanicinin sistem icindeki mevcut bakiyesini tutar.
    public decimal Bakiye { get; set; }

    // Bu alan kullanicinin sisteme kaydoldugu zamani tutar.
    public string KayitTarihi { get; set; } = string.Empty;
}

// Bu model kurs kategorisini temsil eder.
public class Kategori
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan kategorinin adini tutar.
    public string Ad { get; set; } = string.Empty;

    // Bu alan kategorinin aciklamasini tutar.
    public string Aciklama { get; set; } = string.Empty;
}

// Bu model egitmenin ekledigi kursu temsil eder.
public class Kurs
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan kursu olusturan egitmenin kimligini tutar.
    public int EgitmenId { get; set; }

    // Bu alan secilen kategori kimligini tutar.
    public int KategoriId { get; set; }

    // Bu alan kurs basligini tutar.
    public string Baslik { get; set; } = string.Empty;

    // Bu alan kurs aciklamasini tutar.
    public string Aciklama { get; set; } = string.Empty;

    // Bu alan satin alma oncesi gosterilecek onizleme videosunu tutar.
    public string OnizlemeVideoUrl { get; set; } = string.Empty;

    // Bu alan eski veritabani uyumlulugu icin ayni onizleme bilgisini de tutar.
    public string VideoUrl { get; set; } = string.Empty;

    // Bu alan kurs icin genel dokuman dosyasini tutar.
    public string DokumanUrl { get; set; } = string.Empty;

    // Bu alan kursun fiyatini tutar.
    public decimal Fiyat { get; set; }

    // Bu alan kursun yayin durumunu tutar.
    public bool YayinlandiMi { get; set; }

    // Bu alan kursun olusturulma zamanini tutar.
    public string OlusturmaTarihi { get; set; } = string.Empty;
}

// Bu model kurs icindeki her bir video bolumunu temsil eder.
public class KursBolumu
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan bolumun ait oldugu kursu tutar.
    public int KursId { get; set; }

    // Bu alan bolum sira bilgisini tutar.
    public int SiraNo { get; set; }

    // Bu alan bolum basligini tutar.
    public string Baslik { get; set; } = string.Empty;

    // Bu alan bolum aciklamasini tutar.
    public string Aciklama { get; set; } = string.Empty;

    // Bu alan bolum videosunun kaydedildigi yolu tutar.
    public string VideoUrl { get; set; } = string.Empty;

    // Bu alan bolum videosuna ait dokuman yolunu tutar.
    public string DokumanUrl { get; set; } = string.Empty;

    // Bu alan bolumun olusturulma zamanini tutar.
    public string OlusturmaTarihi { get; set; } = string.Empty;
}

// Bu model sisteme yapilan bagisi temsil eder.
public class Bagis
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan bagis yapan kisinin adini tutar.
    public string BagisciAdSoyad { get; set; } = string.Empty;

    // Bu alan bagis tutarini tutar.
    public decimal Tutar { get; set; }

    // Bu alan bagisin yapildigi tarihi tutar.
    public string Tarih { get; set; } = string.Empty;
}

// Bu model ogrencinin kurs kaydini temsil eder.
public class KursKaydi
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan kursu alan ogrenciyi tutar.
    public int OgrenciId { get; set; }

    // Bu alan alinmis kursu tutar.
    public int KursId { get; set; }

    // Bu alan kayit sirasinda odemesi gereken tutari tutar.
    public decimal OdenenTutar { get; set; }

    // Bu alan odemenin havuzdan mi ogrenciden mi geldiyini belirtir.
    public string OdemeYontemi { get; set; } = string.Empty;

    // Bu alan satin alma tarihini tutar.
    public string Tarih { get; set; } = string.Empty;
}

// Bu model liste ekranlarinda zenginlestirilmis kurs bilgisini tasir.
public class KursKart
{
    // Bu alan kurs bilgisini tutar.
    public Kurs Kurs { get; set; } = new();

    // Bu alan egitmenin gorunen adini tutar.
    public string EgitmenAdi { get; set; } = string.Empty;

    // Bu alan kategorinin gorunen adini tutar.
    public string KategoriAdi { get; set; } = string.Empty;

    // Bu alan ogrencinin bu kursa sahip olup olmadigini belirtir.
    public bool SahipMi { get; set; }

    // Bu alan kursun ortalama puanini tutar.
    public decimal OrtalamaPuan { get; set; }

    // Bu alan kursa ait yorum sayisini tutar.
    public int YorumSayisi { get; set; }

    // Bu alan kursun toplam video bolumu sayisini tutar.
    public int BolumSayisi { get; set; }
}

// Bu model ogrencinin kurs icin yaptigi yorumu temsil eder.
public class KursYorumu
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan ilgili kursu tutar.
    public int KursId { get; set; }

    // Bu alan yorumu yapan ogrenciyi tutar.
    public int OgrenciId { get; set; }

    // Bu alan puan bilgisini tutar.
    public int Puan { get; set; }

    // Bu alan yorum metnini tutar.
    public string Yorum { get; set; } = string.Empty;

    // Bu alan yorum tarihini tutar.
    public string Tarih { get; set; } = string.Empty;

    // Bu alan yorumu yapan ogrencinin gorunen adini tutar.
    public string OgrenciAdi { get; set; } = string.Empty;

    // Bu alan yoruma verilen yanitlari tutar.
    public List<YorumYaniti> Yanitlar { get; set; } = [];
}

// Bu model yorumlara verilen yanitlari temsil eder.
public class YorumYaniti
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan bagli olunan yorumu tutar.
    public int KursYorumuId { get; set; }

    // Bu alan yaniti yapan kullaniciyi tutar.
    public int KullaniciId { get; set; }

    // Bu alan yanit metnini tutar.
    public string Yanit { get; set; } = string.Empty;

    // Bu alan yanit tarihini tutar.
    public string Tarih { get; set; } = string.Empty;

    // Bu alan yaniti yapan kisinin gorunen adini tutar.
    public string KullaniciAdi { get; set; } = string.Empty;

    // Bu alan yaniti yapan kisinin rolunu tutar.
    public string KullaniciRol { get; set; } = string.Empty;
}

// Bu model ogrencinin bir bolumdeki ilerlemesini tutar.
public class BolumIlerlemesi
{
    // Bu alan veritabanindaki birincil anahtardir.
    public int Id { get; set; }

    // Bu alan ilgili ogrenciyi tutar.
    public int OgrenciId { get; set; }

    // Bu alan ilgili kursu tutar.
    public int KursId { get; set; }

    // Bu alan ilgili bolumu tutar.
    public int KursBolumuId { get; set; }

    // Bu alan bolumun tamamlanip tamamlanmadigini belirtir.
    public bool TamamlandiMi { get; set; }

    // Bu alan ogrencinin en son bu bolume ne zaman girdigini tutar.
    public string SonIzlemeTarihi { get; set; } = string.Empty;
}

// Bu model kullanicinin kurs ilerleme ozetini tutar.
public class KursIlerlemeOzeti
{
    // Bu alan ilgili kursu tutar.
    public int KursId { get; set; }

    // Bu alan tamamlanma yuzdesini tutar.
    public decimal TamamlanmaYuzdesi { get; set; }

    // Bu alan tamamlanan bolum sayisini tutar.
    public int TamamlananBolumSayisi { get; set; }

    // Bu alan toplam bolum sayisini tutar.
    public int ToplamBolumSayisi { get; set; }

    // Bu alan en son kalinan bolum basligini tutar.
    public string SonKalinanBolumBasligi { get; set; } = string.Empty;

    // Bu alan son kalinan bolum kimligini tutar.
    public int? SonKalinanBolumId { get; set; }
}
