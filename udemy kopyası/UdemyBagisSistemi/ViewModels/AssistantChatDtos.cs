namespace UdemyBagisSistemi.ViewModels;

public class AssistantChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto> History { get; set; } = [];
}

public class AssistantChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
}

public class ChatMessageDto
{
    // Beklenen rol: "user" veya "assistant" (server tarafı system prompt ekler).
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

