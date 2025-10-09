using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Services;
using System.Threading.Tasks;

namespace PawVerseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Message cannot be empty"));
            }

            try
            {
                var response = await _chatbotService.SendMessageAsync(request.Message);
                return Ok(ApiResponse<ChatbotResponse>.SuccessResponse(
                    new ChatbotResponse { Response = response }, 
                    "Success"));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending message to chatbot");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while communicating with the chatbot"));
            }
        }
    }

    public class ChatMessageRequest
    {
        public string Message { get; set; }
    }

    public class ChatbotResponse
    {
        public string Response { get; set; }
    }
}
