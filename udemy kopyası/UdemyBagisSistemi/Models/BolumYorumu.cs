namespace UdemyBagisSistemi.Models;

// Bu model öğrencilerin bölümlere (videolara) yaptığı özel yorumları temsil eder.
public class BolumYorumu
{
    // Yorumun benzersiz kimliği
    public int Id { get; set; }

    // Yorumun yapıldığı bölümün kimliği
    public int KursBolumuId { get; set; }

    // Yorumu yapan kullanıcının kimliği
    public int KullaniciId { get; set; }

    // Yorum içeriği
    public string Yorum { get; set; } = string.Empty;

    // Yorum tarihi
    public string Tarih { get; set; } = string.Empty;

    // Eğer bu bir yanıtsa, ana yorumun Id'si. 0 ise kök yorumdur.
    public int ParentId { get; set; }

    // Veritabanında olmayan, sadece arayüzde göstermek için kullanılan alan
    public string KullaniciAdi { get; set; } = string.Empty;

    // Kullanıcının rolü (Egitmen, Ogrenci vb.) — arayüzde rozet göstermek için
    public string KullaniciRol { get; set; } = string.Empty;

    // Bu yoruma gelen yanıtlar (iç içe yapı)
    public List<BolumYorumu> Yanitlar { get; set; } = [];
}
