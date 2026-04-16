using System.Net;
using System.Net.Mail;

namespace UdemyBagisSistemi.Servisler;

public class SmtpEpostaServisi : IEpostaServisi
{
    private readonly IConfiguration _yapilandirma;

    public SmtpEpostaServisi(IConfiguration yapilandirma)
    {
        _yapilandirma = yapilandirma;
    }

    public async Task EpostaGonderAsync(string kime, string konu, string icerik)
    {
        var sunucu = _yapilandirma["SmtpAyarlari:Sunucu"];
        var port = int.TryParse(_yapilandirma["SmtpAyarlari:Port"], out var p) ? p : 587;
        var kullaniciAdi = _yapilandirma["SmtpAyarlari:KullaniciAdi"];
        var sifre = _yapilandirma["SmtpAyarlari:Sifre"];
        var gonderen = _yapilandirma["SmtpAyarlari:Gonderen"];

        // Ayarlar yoksa veya bossa hata vermek yerine sadece uyar ve gec (gelistirme kolayligi icin)
        if (string.IsNullOrWhiteSpace(sunucu) || string.IsNullOrWhiteSpace(kullaniciAdi))
        {
            Console.WriteLine($"[DIKKAT] SMTP Ayarlari eksik. Su E-posta gonderilemedi:\nKime: {kime}\nKonu: {konu}\nIcerik: {icerik}");
            return;
        }

        using var islemci = new SmtpClient(sunucu, port)
        {
            Credentials = new NetworkCredential(kullaniciAdi, sifre),
            EnableSsl = true,
        };

        var mesaj = new MailMessage
        {
            From = new MailAddress(gonderen ?? kullaniciAdi, "EduVerse Onay Sistemi"),
            Subject = konu,
            Body = icerik,
            IsBodyHtml = true
        };

        mesaj.To.Add(kime);

        await islemci.SendMailAsync(mesaj);
    }
}
