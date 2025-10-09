using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Token là bắt buộc")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Refresh token là bắt buộc")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
