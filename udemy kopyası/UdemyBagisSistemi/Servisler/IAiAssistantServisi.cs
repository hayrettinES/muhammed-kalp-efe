namespace UdemyBagisSistemi.Servisler;

using UdemyBagisSistemi.ViewModels;

public interface IAiAssistantServisi
{
    Task<string> YanitUretAsync(string message, IReadOnlyList<ChatMessageDto> history, AgentBaglam? baglam = null, CancellationToken cancellationToken = default);
}

