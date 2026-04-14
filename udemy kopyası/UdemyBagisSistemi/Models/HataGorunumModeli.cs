// Bu dosya hata ekraninda kullanilan modeli tanimlar.
namespace UdemyBagisSistemi.Models;

// Bu model hata durumunda istek kimligini ekrana tasir.
public class HataGorunumModeli
{
    // Bu alan istek kimligini tutar.
    public string? IstekKimligi { get; set; }

    // Bu alan istek kimligi varsa ekranda gosterim karari verir.
    public bool IstekKimligiGoster => !string.IsNullOrWhiteSpace(IstekKimligi);
}
