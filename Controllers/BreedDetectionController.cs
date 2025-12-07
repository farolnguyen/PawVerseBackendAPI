using Microsoft.AspNetCore.Mvc;
using PawVerseAPI.Models.DTOs;
using PawVerseAPI.Models.DTOs.BreedDetection;
using PawVerseAPI.Services.BreedDetection;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/breed-detection")]
    public class BreedDetectionController : ControllerBase
    {
        private readonly IBreedDetectionService _breedDetectionService;
        private readonly ILogger<BreedDetectionController> _logger;
        
        public BreedDetectionController(
            IBreedDetectionService breedDetectionService,
            ILogger<BreedDetectionController> logger)
        {
            _breedDetectionService = breedDetectionService;
            _logger = logger;
        }
        
        /// <summary>
        /// Detect pet breed from uploaded image
        /// </summary>
        /// <param name="request">Multipart form data with image file</param>
        /// <returns>Detected breed and recommended products</returns>
        [HttpPost("detect")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<DetectBreedResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<DetectBreedResponse>), 400)]
        public async Task<ActionResult<ApiResponse<DetectBreedResponse>>> DetectBreed(
            [FromForm] DetectBreedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<DetectBreedResponse>.ErrorResponse(
                        "Invalid request", errors));
                }
                
                // Apply default values if not provided
                var animalType = string.IsNullOrWhiteSpace(request.AnimalType) ? "dog" : request.AnimalType;
                var maxProducts = request.MaxProducts ?? 20;
                
                var result = await _breedDetectionService.DetectBreedAsync(
                    request.Image,
                    animalType,
                    maxProducts
                );
                
                if (result.Success)
                {
                    return Ok(ApiResponse<DetectBreedResponse>.SuccessResponse(
                        result,
                        result.Message ?? "Breed detected successfully"
                    ));
                }
                else
                {
                    return BadRequest(ApiResponse<DetectBreedResponse>.ErrorResponse(
                        result.Error ?? "Detection failed"
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in breed detection");
                return StatusCode(500, ApiResponse<DetectBreedResponse>.ErrorResponse(
                    "An error occurred while processing your request",
                    new List<string> { ex.Message }
                ));
            }
        }
        
        /// <summary>
        /// Get recommended products for a specific breed
        /// </summary>
        /// <param name="breedName">Breed name</param>
        /// <param name="animalType">Animal type (dog/cat)</param>
        /// <param name="maxProducts">Maximum number of products to return</param>
        [HttpGet("products")]
        [ProducesResponseType(typeof(ApiResponse<List<BreedProductDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<BreedProductDto>>>> GetProductsByBreed(
            [FromQuery] string breedName,
            [FromQuery] string animalType = "dog",
            [FromQuery] int maxProducts = 20)
        {
            try
            {
                var products = await _breedDetectionService.GetProductsByBreedAsync(
                    breedName, 
                    animalType, 
                    maxProducts);
                
                var message = products.Item2 
                    ? $"Tìm thấy {products.Item1.Count} sản phẩm cho {breedName}"
                    : $"Chưa có sản phẩm đặc thù cho {breedName}. Hiển thị sản phẩm phù hợp cho {(animalType == "cat" ? "mèo" : "chó")}.";
                
                return Ok(ApiResponse<List<BreedProductDto>>.SuccessResponse(
                    products.Item1,
                    message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for breed: {BreedName}", breedName);
                return StatusCode(500, ApiResponse<List<BreedProductDto>>.ErrorResponse(
                    "An error occurred while fetching products"
                ));
            }
        }
        
        /// <summary>
        /// Check if breed detection service is ready
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public ActionResult<ApiResponse<object>> GetStatus()
        {
            var isReady = _breedDetectionService.IsReady();
            
            return Ok(ApiResponse<object>.SuccessResponse(
                new
                {
                    ready = isReady,
                    message = isReady
                        ? "Breed detection service is ready"
                        : "Breed detection service is initializing..."
                }
            ));
        }
    }
}
