namespace UdemyBagisSistemi.Servisler;

using UdemyBagisSistemi.ViewModels;

public interface IAiAssistantServisi
{
    Task<string> YanitUretAsync(string message, IReadOnlyList<ChatMessageDto> history, CancellationToken cancellationToken = default);
}

