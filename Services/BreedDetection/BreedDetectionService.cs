using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PawVerseAPI.Data;
using PawVerseAPI.Models.DTOs.BreedDetection;

namespace PawVerseAPI.Services.BreedDetection
{
    public class BreedDetectionService : IBreedDetectionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BreedDetectionService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private readonly string _uploadPath;
        private readonly string _projectRoot;
        private readonly int _timeoutSeconds;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _lock = new(1, 1); // Serialize requests
        
        public BreedDetectionService(
            IConfiguration configuration,
            ILogger<BreedDetectionService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
            
            // Load config
            _pythonPath = _configuration["BreedDetection:PythonPath"] 
                ?? throw new InvalidOperationException("Python path not configured");
            
            // Find project root (go up from bin/Debug/net8.0 to project root)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Navigate up: bin/Debug/net8.0 -> bin/Debug -> bin -> project root
            var parent1 = Directory.GetParent(baseDir); // bin/Debug
            var parent2 = parent1?.Parent; // bin
            var parent3 = parent2?.Parent; // project root
            
            _projectRoot = parent3?.FullName 
                ?? throw new InvalidOperationException($"Cannot find project root from: {baseDir}");
            
            // Get script path - use absolute if provided, otherwise combine with project root
            var scriptPathConfig = _configuration["BreedDetection:ScriptPath"] ?? "Python\\breed_detection.py";
            _scriptPath = Path.IsPathRooted(scriptPathConfig) 
                ? scriptPathConfig 
                : Path.Combine(_projectRoot, scriptPathConfig);
            
            // Get upload path - use absolute if provided, otherwise combine with project root
            var uploadPathConfig = _configuration["BreedDetection:UploadPath"] ?? "wwwroot\\uploads\\breed_detection";
            _uploadPath = Path.IsPathRooted(uploadPathConfig) 
                ? uploadPathConfig 
                : Path.Combine(_projectRoot, uploadPathConfig);
            _timeoutSeconds = int.Parse(_configuration["BreedDetection:ProcessTimeoutSeconds"] ?? "60");
            
            // Log paths for debugging
            _logger.LogInformation("Project Root: {ProjectRoot}", _projectRoot);
            _logger.LogInformation("Script Path: {ScriptPath}", _scriptPath);
            _logger.LogInformation("Python Path: {PythonPath}", _pythonPath);
            
            // Verify script exists
            if (!File.Exists(_scriptPath))
            {
                throw new InvalidOperationException($"Python script not found at: {_scriptPath}");
            }
            
            // Create upload directory
            Directory.CreateDirectory(_uploadPath);
        }
        
        public async Task<bool> InitializeModelsAsync()
        {
            try
            {
                _logger.LogInformation("Initializing breed detection models...");
                
                var result = await ExecutePythonAsync("--init-only", timeout: 120000); // 2 min
                
                if (result.Success)
                {
                    _isInitialized = true;
                    _logger.LogInformation("Breed detection models initialized successfully");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to initialize models: {Error}", result.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing breed detection models");
                return false;
            }
        }
        
        public bool IsReady() => _isInitialized;
        
        public async Task<(List<BreedProductDto>, bool)> GetProductsByBreedAsync(
            string breedName, 
            string animalType, 
            int maxProducts = 20)
        {
            return await SearchProductsAsync(breedName, animalType, maxProducts);
        }
        
        public async Task<DetectBreedResponse> DetectBreedAsync(
            IFormFile imageFile, 
            string animalType, 
            int maxProducts = 20)
        {
            if (!_isInitialized)
            {
                return new DetectBreedResponse
                {
                    Success = false,
                    Error = "Breed detection service is not ready. Please try again later."
                };
            }
            
            // Serialize requests to avoid GPU OOM
            await _lock.WaitAsync();
            
            try
            {
                // Step 1: Validate image
                var validation = ValidateImage(imageFile);
                if (!validation.IsValid)
                {
                    return new DetectBreedResponse
                    {
                        Success = false,
                        Error = validation.Error
                    };
                }
                
                // Step 2: Save image temporarily
                var imagePath = await SaveImageAsync(imageFile);
                
                try
                {
                    // Step 3: Run Python detection
                    var pythonResult = await ExecutePythonAsync(
                        $"--image \"{imagePath}\" --type {animalType}",
                        timeout: _timeoutSeconds * 1000
                    );
                    
                    if (!pythonResult.Success)
                    {
                        return new DetectBreedResponse
                        {
                            Success = false,
                            Error = pythonResult.Error,
                            Message = pythonResult.Message
                        };
                    }
                    
                    // Step 4: Search products
                    var (products, foundSpecific) = await SearchProductsAsync(
                        pythonResult.Breed!, 
                        pythonResult.AnimalType ?? animalType, 
                        maxProducts);
                    
                    // Step 5: Build response message
                    var animalName = (pythonResult.AnimalType ?? animalType).ToLower() == "cat" ? "mèo" : "chó";
                    var message = foundSpecific
                        ? $"Đã phát hiện giống {animalName}: {pythonResult.Breed}"
                        : $"Đã phát hiện giống {animalName}: {pythonResult.Breed}. Chưa có sản phẩm đặc thù cho giống này, dưới đây là một số sản phẩm phù hợp cho {animalName}.";
                    
                    // Step 6: Map TopBreeds
                    var topBreeds = pythonResult.TopBreeds?.Select(b => new BreedCandidate
                    {
                        Breed = b.Breed,
                        Score = b.Score,
                        Rank = b.Rank
                    }).ToList();
                    
                    // Step 7: Build response
                    return new DetectBreedResponse
                    {
                        Success = true,
                        Message = message,
                        Breed = pythonResult.Breed,
                        Confidence = pythonResult.Confidence,
                        AnimalType = pythonResult.AnimalType,
                        TopBreeds = topBreeds,
                        RecommendedProducts = products,
                        Metadata = pythonResult.Metadata
                    };
                }
                finally
                {
                    // Cleanup temp file
                    CleanupFile(imagePath);
                }
            }
            finally
            {
                _lock.Release();
            }
        }
        
        private (bool IsValid, string? Error) ValidateImage(IFormFile file)
        {
            // Size check
            var maxSizeMB = int.Parse(_configuration["BreedDetection:MaxImageSizeMB"] ?? "10");
            if (file.Length > maxSizeMB * 1024 * 1024)
            {
                return (false, $"Image size exceeds {maxSizeMB}MB limit");
            }
            
            // Extension check
            var ext = Path.GetExtension(file.FileName).ToLower();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExts.Contains(ext))
            {
                return (false, "Only JPG, JPEG, PNG images are allowed");
            }
            
            return (true, null);
        }
        
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_uploadPath, fileName);
            
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            
            return filePath;
        }
        
        private async Task<PythonResult> ExecutePythonAsync(string arguments, int timeout = 30000)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_scriptPath}\" {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _projectRoot
            };
            
            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            var completed = await Task.Run(() => process.WaitForExit(timeout));
            
            if (!completed)
            {
                try { process.Kill(); } catch { }
                return new PythonResult
                {
                    Success = false,
                    Error = $"Python process timed out after {timeout}ms"
                };
            }
            
            var output = outputBuilder.ToString().Trim();
            var errors = errorBuilder.ToString().Trim();
            
            // Log stderr (contains model loading info or errors)
            if (!string.IsNullOrEmpty(errors))
            {
                _logger.LogWarning("Python stderr: {Errors}", errors);
            }
            
            // Check if output is empty
            if (string.IsNullOrEmpty(output))
            {
                _logger.LogError("Python script returned empty output. Stderr: {Errors}", errors);
                return new PythonResult
                {
                    Success = false,
                    Error = $"Python script returned no output. Check logs for details. Stderr: {errors}"
                };
            }
            
            // Parse JSON output
            try
            {
                _logger.LogDebug("Python stdout: {Output}", output);
                var result = JsonSerializer.Deserialize<PythonResult>(output, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return result ?? new PythonResult { Success = false, Error = "Empty response from Python" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Python output: {Output}", output);
                return new PythonResult
                {
                    Success = false,
                    Error = $"Invalid Python response: {ex.Message}"
                };
            }
        }
        
        private async Task<(List<BreedProductDto> Products, bool FoundSpecific)> SearchProductsAsync(
            string breedName, 
            string animalType, 
            int maxProducts)
        {
            // Create a scope to get DbContext (Singleton can't inject Scoped service directly)
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Step 1: Try to find products matching breed name
            var searchTerm = breedName.ToLower();
            
            var breedProducts = await context.SanPhams
                .Include(p => p.IdDanhMucNavigation)
                .Include(p => p.IdThuongHieuNavigation)
                .Where(p => p.TrangThai == "Còn hàng" &&
                           (p.TenSanPham.ToLower().Contains(searchTerm) ||
                            p.MoTa.ToLower().Contains(searchTerm)))
                .OrderByDescending(p => p.SoLuongDaBan)
                .Take(maxProducts)
                .Select(p => new BreedProductDto
                {
                    IdSanPham = p.IdSanPham,
                    TenSanPham = p.TenSanPham,
                    TenDanhMuc = p.IdDanhMucNavigation.TenDanhMuc,
                    TenThuongHieu = p.IdThuongHieuNavigation.TenThuongHieu,
                    GiaHienThi = p.GiaKhuyenMai ?? p.GiaBan,
                    GiaKhuyenMai = p.GiaKhuyenMai,
                    HinhAnh = p.HinhAnh,
                    CoKhuyenMai = p.GiaKhuyenMai.HasValue && p.GiaKhuyenMai < p.GiaBan,
                    PhanTramGiam = (p.GiaKhuyenMai.HasValue && p.GiaKhuyenMai < p.GiaBan)
                        ? (int)Math.Round((p.GiaBan - p.GiaKhuyenMai.Value) / p.GiaBan * 100)
                        : null
                })
                .ToListAsync();
            
            // Step 2: If found breed-specific products, return them
            if (breedProducts.Any())
            {
                _logger.LogInformation("Found {Count} breed-specific products for '{BreedName}'", breedProducts.Count, breedName);
                return (breedProducts, true);
            }
            
            _logger.LogInformation("No breed-specific products found for '{BreedName}', searching fallback", breedName);
            
            // Step 3: No breed-specific products found, get random products for animal type
            var animalKeyword = animalType.ToLower() == "cat" ? "mèo" : "chó";
            
            // Try to find products matching animal keyword
            var randomProducts = await context.SanPhams
                .Include(p => p.IdDanhMucNavigation)
                .Include(p => p.IdThuongHieuNavigation)
                .Where(p => p.TrangThai == "Còn hàng" &&
                           (p.TenSanPham.ToLower().Contains(animalKeyword) ||
                            p.MoTa.ToLower().Contains(animalKeyword) ||
                            p.IdDanhMucNavigation.TenDanhMuc.ToLower().Contains(animalKeyword)))
                .OrderBy(p => Guid.NewGuid()) // Random order
                .Take(maxProducts)
                .Select(p => new BreedProductDto
                {
                    IdSanPham = p.IdSanPham,
                    TenSanPham = p.TenSanPham,
                    TenDanhMuc = p.IdDanhMucNavigation.TenDanhMuc,
                    TenThuongHieu = p.IdThuongHieuNavigation.TenThuongHieu,
                    GiaHienThi = p.GiaKhuyenMai ?? p.GiaBan,
                    GiaKhuyenMai = p.GiaKhuyenMai,
                    HinhAnh = p.HinhAnh,
                    CoKhuyenMai = p.GiaKhuyenMai.HasValue && p.GiaKhuyenMai < p.GiaBan,
                    PhanTramGiam = (p.GiaKhuyenMai.HasValue && p.GiaKhuyenMai < p.GiaBan)
                        ? (int)Math.Round((p.GiaBan - p.GiaKhuyenMai.Value) / p.GiaBan * 100)
                        : null
                })
                .ToListAsync();
            
            // Step 4: If still no products (database might not have animal keyword), get ANY random products
            if (!randomProducts.Any())
            {
                _logger.LogInformation("No products found with keyword '{AnimalKeyword}', getting any random products", animalKeyword);
                
                randomProducts = await context.SanPhams
                    .Include(p => p.IdDanhMucNavigation)
                    .Include(p => p.IdThuongHieuNavigation)
                    .Where(p => p.TrangThai == "Còn hàng")
                    .OrderBy(p => Guid.NewGuid()) // Random order
                    .Take(maxProducts)
                    .Select(p => new BreedProductDto
                    {
                        IdSanPham = p.IdSanPham,
                        TenSanPham = p.TenSanPham,
                        TenDanhMuc = p.IdDanhMucNavigation.TenDanhMuc,
                        TenThuongHieu = p.IdThuongHieuNavigation.TenThuongHieu,
                        GiaHienThi = p.GiaKhuyenMai ?? p.GiaBan,
                        GiaKhuyenMai = p.GiaKhuyenMai,
                        HinhAnh = p.HinhAnh,
                        CoKhuyenMai = p.GiaKhuyenMai.HasValue && p.GiaKhuyenMai < p.GiaBan,
                        PhanTramGiam = (p.GiaKhuyenMai.HasValue && p.GiaKhuyenMai < p.GiaBan)
                            ? (int)Math.Round((p.GiaBan - p.GiaKhuyenMai.Value) / p.GiaBan * 100)
                            : null
                    })
                    .ToListAsync();
                    
                _logger.LogInformation("Fallback products found: {Count}", randomProducts.Count);
            }
            
            return (randomProducts, false);
        }
        
        private void CleanupFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp file: {FilePath}", filePath);
            }
        }
        
        // Internal class for Python response parsing
        private class PythonResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("success")]
            public bool Success { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("error")]
            public string? Error { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("message")]
            public string? Message { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("breed")]
            public string? Breed { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("animal_type")]
            public string? AnimalType { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("confidence")]
            public float Confidence { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("top_breeds")]
            public List<PythonBreedCandidate>? TopBreeds { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("metadata")]
            public BreedDetectionMetadata? Metadata { get; set; }
        }
        
        private class PythonBreedCandidate
        {
            [System.Text.Json.Serialization.JsonPropertyName("breed")]
            public string Breed { get; set; } = string.Empty;
            
            [System.Text.Json.Serialization.JsonPropertyName("score")]
            public float Score { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("rank")]
            public int Rank { get; set; }
        }
    }
}
