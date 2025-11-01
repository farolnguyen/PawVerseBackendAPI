using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PawVerseAPI.Helpers;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.Auth;
using PawVerseAPI.Models.Entities;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtHelper jwtHelper,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Email đã được sử dụng"));
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Đăng ký thất bại", errors));
            }

            // Assign default role "User"
            await _userManager.AddToRoleAsync(user, "User");

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtHelper.GenerateJwtToken(user, roles);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = _jwtHelper.GetTokenExpiryTime(),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Avatar = user.Avatar,
                    Roles = roles.ToList()
                }
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "Đăng ký thành công"));
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Email hoặc mật khẩu không đúng"));
            }

            // Check password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Email hoặc mật khẩu không đúng"));
            }

            // Generate JWT token
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtHelper.GenerateJwtToken(user, roles);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = _jwtHelper.GetTokenExpiryTime(),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Avatar = user.Avatar,
                    Roles = roles.ToList()
                }
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "Đăng nhập thành công"));
        }

        /// <summary>
        /// Lấy thông tin người dùng hiện tại
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserProfileDto>.ErrorResponse("Unauthorized"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.ErrorResponse("Không tìm thấy người dùng"));
            }

            var profile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                DiaChi = user.DiaChi,
                GioiTinh = user.GioiTinh,
                NgaySinh = user.NgaySinh,
                Avatar = user.Avatar
            };

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile));
        }

        /// <summary>
        /// Cập nhật thông tin người dùng
        /// </summary>
        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<UserProfileDto>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserProfileDto>.ErrorResponse("Unauthorized"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserProfileDto>.ErrorResponse("Không tìm thấy người dùng"));
            }

            // Update user properties
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.DiaChi = request.DiaChi;
            user.GioiTinh = request.GioiTinh;
            user.NgaySinh = request.NgaySinh;
            user.NgayCapNhat = DateTime.Now;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<UserProfileDto>.ErrorResponse("Cập nhật thất bại", errors));
            }

            var profile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                DiaChi = user.DiaChi,
                GioiTinh = user.GioiTinh,
                NgaySinh = user.NgaySinh,
                Avatar = user.Avatar
            };

            return Ok(ApiResponse<UserProfileDto>.SuccessResponse(profile, "Cập nhật thành công"));
        }

        /// <summary>
        /// Upload ảnh đại diện
        /// </summary>
        [Authorize]
        [HttpPost("profile/upload-avatar")]
        public async Task<ActionResult<ApiResponse<string>>> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Vui lòng chọn ảnh"));
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)"));
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Kích thước ảnh không được vượt quá 5MB"));
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Unauthorized"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<string>.ErrorResponse("Không tìm thấy người dùng"));
            }

            try
            {
                // Create profiles directory if not exists
                var profilesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "profiles");
                if (!Directory.Exists(profilesPath))
                {
                    Directory.CreateDirectory(profilesPath);
                }

                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.Avatar))
                {
                    var oldAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", user.Avatar);
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(profilesPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update user avatar path
                user.Avatar = $"profiles/{fileName}";
                user.NgayCapNhat = DateTime.Now;
                await _userManager.UpdateAsync(user);

                return Ok(ApiResponse<string>.SuccessResponse(user.Avatar, "Upload ảnh thành công"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Lỗi khi upload ảnh"));
            }
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        [Authorize]
        [HttpPut("change-password")]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy người dùng"));
            }

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Đổi mật khẩu thất bại", errors));
            }

            // Update security stamp
            await _userManager.UpdateSecurityStampAsync(user);

            return Ok(ApiResponse<object>.SuccessResponse(null, "Đổi mật khẩu thành công"));
        }

        /// <summary>
        /// Quên mật khẩu - Verify user
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy tài khoản với email này"));
            }

            // Verify phone number matches
            if (user.PhoneNumber != request.PhoneNumber)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Số điện thoại không khớp với tài khoản"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Xác thực thành công. Vui lòng nhập mật khẩu mới"));
        }

        /// <summary>
        /// Reset mật khẩu
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy tài khoản"));
            }

            // Remove old password and set new one
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Đặt lại mật khẩu thất bại", errors));
            }

            // Update security stamp
            await _userManager.UpdateSecurityStampAsync(user);

            return Ok(ApiResponse<object>.SuccessResponse(null, "Đặt lại mật khẩu thành công"));
        }

        /// <summary>
        /// Refresh JWT token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var principal = _jwtHelper.GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Token không hợp lệ"));
            }

            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Token không hợp lệ"));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<LoginResponse>.ErrorResponse("Không tìm thấy người dùng"));
            }

            // Generate new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var newToken = _jwtHelper.GenerateJwtToken(user, roles);
            var newRefreshToken = _jwtHelper.GenerateRefreshToken();

            var response = new LoginResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = _jwtHelper.GetTokenExpiryTime(),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Avatar = user.Avatar,
                    Roles = roles.ToList()
                }
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "Refresh token thành công"));
        }

        /// <summary>
        /// GitHub OAuth callback - Exchange code for token
        /// </summary>
        [HttpPost("github-callback")]
        public async Task<ActionResult<ApiResponse<object>>> GitHubCallback([FromBody] GitHubCallbackRequest request)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var tokenRequest = new
                {
                    client_id = _configuration["Authentication:GitHub:ClientId"],
                    client_secret = _configuration["Authentication:GitHub:ClientSecret"],
                    code = request.Code
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(tokenRequest),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync("https://github.com/login/oauth/access_token", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Failed to exchange GitHub code for token"));
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonDocument.Parse(json);
                var root = data.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse($"GitHub OAuth error: {error.GetString()}"));
                }

                if (!root.TryGetProperty("access_token", out var accessToken))
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("No access token returned from GitHub"));
                }

                return Ok(ApiResponse<object>.SuccessResponse(new { access_token = accessToken.GetString() }, "Token retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error: {ex.Message}"));
            }
        }

        /// <summary>
        /// External login với Google/GitHub (sử dụng ID Token từ frontend)
        /// </summary>
        [HttpPost("external-login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> ExternalLogin([FromBody] ExternalLoginRequest request)
        {
            try
            {
                // Verify token với provider (Google hoặc GitHub)
                ExternalLoginInfoDto? loginInfo = null;

                if (request.Provider.ToLower() == "google")
                {
                    loginInfo = await VerifyGoogleToken(request.IdToken);
                }
                else if (request.Provider.ToLower() == "github")
                {
                    loginInfo = await VerifyGitHubToken(request.AccessToken ?? request.IdToken);
                }

                if (loginInfo == null)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Không thể xác thực với " + request.Provider));
                }

                // Tìm hoặc tạo user
                var user = await _userManager.FindByEmailAsync(loginInfo.Email);
                
                if (user == null)
                {
                    // Tạo user mới
                    user = new ApplicationUser
                    {
                        UserName = loginInfo.Email,
                        Email = loginInfo.Email,
                        FullName = loginInfo.Name,
                        Avatar = loginInfo.Avatar,
                        EmailConfirmed = true,
                        NgayTao = DateTime.Now
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Không thể tạo tài khoản"));
                    }

                    // Add role User mặc định
                    await _userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    // Update avatar nếu có
                    if (!string.IsNullOrEmpty(loginInfo.Avatar) && string.IsNullOrEmpty(user.Avatar))
                    {
                        user.Avatar = loginInfo.Avatar;
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Check if account is locked
                if (await _userManager.IsLockedOutAsync(user))
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Tài khoản đã bị khóa"));
                }

                // Add external login nếu chưa có
                var existingLogins = await _userManager.GetLoginsAsync(user);
                if (!existingLogins.Any(l => l.LoginProvider == request.Provider))
                {
                    var loginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(
                        request.Provider,
                        loginInfo.ProviderId,
                        request.Provider
                    ));
                }

                // Generate JWT token
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtHelper.GenerateJwtToken(user, roles);
                var refreshToken = _jwtHelper.GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
                await _userManager.UpdateAsync(user);

                var response = new LoginResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = _jwtHelper.GetTokenExpiryTime(),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        Avatar = user.Avatar,
                        Roles = roles.ToList()
                    }
                };

                return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "Đăng nhập thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("Lỗi hệ thống: " + ex.Message));
            }
        }

        /// <summary>
        /// Verify Google ID Token
        /// </summary>
        private async Task<ExternalLoginInfoDto?> VerifyGoogleToken(string accessToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                
                // Use userinfo endpoint instead of tokeninfo (works with access tokens)
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonDocument.Parse(json);
                var root = data.RootElement;

                return new ExternalLoginInfoDto
                {
                    Email = root.GetProperty("email").GetString() ?? "",
                    Name = root.GetProperty("name").GetString() ?? "",
                    Avatar = root.TryGetProperty("picture", out var pic) ? pic.GetString() : null,
                    Provider = "Google",
                    ProviderId = root.GetProperty("sub").GetString() ?? ""
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verify GitHub Access Token
        /// </summary>
        private async Task<ExternalLoginInfoDto?> VerifyGitHubToken(string accessToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "PawVerse");

                var response = await httpClient.GetAsync("https://api.github.com/user");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonDocument.Parse(json);
                var root = data.RootElement;

                // Get email from separate endpoint if not available
                string? email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                
                if (string.IsNullOrEmpty(email))
                {
                    var emailResponse = await httpClient.GetAsync("https://api.github.com/user/emails");
                    if (emailResponse.IsSuccessStatusCode)
                    {
                        var emailJson = await emailResponse.Content.ReadAsStringAsync();
                        var emailData = System.Text.Json.JsonDocument.Parse(emailJson);
                        var emails = emailData.RootElement.EnumerateArray();
                        
                        foreach (var emailItem in emails)
                        {
                            if (emailItem.TryGetProperty("primary", out var isPrimary) && isPrimary.GetBoolean())
                            {
                                email = emailItem.GetProperty("email").GetString();
                                break;
                            }
                        }
                        
                        // Fallback to first email
                        if (string.IsNullOrEmpty(email))
                        {
                            email = emailData.RootElement[0].GetProperty("email").GetString();
                        }
                    }
                }

                return new ExternalLoginInfoDto
                {
                    Email = email ?? "",
                    Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? root.GetProperty("login").GetString() ?? "" : root.GetProperty("login").GetString() ?? "",
                    Avatar = root.TryGetProperty("avatar_url", out var avatar) ? avatar.GetString() : null,
                    Provider = "GitHub",
                    ProviderId = root.GetProperty("id").GetInt32().ToString()
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
