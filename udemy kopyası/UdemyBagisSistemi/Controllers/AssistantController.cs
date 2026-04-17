using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using UdemyBagisSistemi.Servisler;
using UdemyBagisSistemi.ViewModels;

namespace UdemyBagisSistemi.Controllers;

[Route("Assistant")]
public class AssistantController : Controller
{
    private readonly IAiAssistantServisi _assistantServisi;

    public AssistantController(IAiAssistantServisi assistantServisi)
    {
        _assistantServisi = assistantServisi;
    }

    [HttpPost("Chat")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { error = "İstek gövdesi boş." });
        }

        var message = request.Message?.Trim() ?? string.Empty;
        if (message.Length == 0)
        {
            return BadRequest(new { error = "Mesaj boş olamaz." });
        }

        // Çok uzun mesajlarda maliyeti/response süresini kontrol etmek için kaba limit.
        if (message.Length > 2000)
        {
            return BadRequest(new { error = "Mesaj çok uzun. Lütfen daha kısa yazın." });
        }

        // Basit temizlik: kontrol karakterlerini kırpmak.
        message = Regex.Replace(message, @"\p{C}+", " ");

        try
        {
            var reply = await _assistantServisi.YanitUretAsync(
                message,
                request.History ?? [],
                cancellationToken);

            return Ok(new AssistantChatResponseDto { Reply = reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

