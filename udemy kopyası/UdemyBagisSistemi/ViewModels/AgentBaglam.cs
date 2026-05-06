namespace UdemyBagisSistemi.ViewModels;

/// <summary>
/// Agent'ın tool calling sırasında kullanıcı bağlamını taşıyan DTO.
/// </summary>
public class AgentBaglam
{
    public int? KullaniciId { get; set; }
    public string? Rol { get; set; }
    public string? AdSoyad { get; set; }
}
