using System.ComponentModel.DataAnnotations;

namespace PawVerseAPI.Models.DTOs.Auth
{
    public class ExternalLoginRequest
    {
        [Required]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string IdToken { get; set; } = string.Empty;

        public string? AccessToken { get; set; }
    }

    public class GitHubCallbackRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class ExternalLoginInfoDto
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
    }
}
