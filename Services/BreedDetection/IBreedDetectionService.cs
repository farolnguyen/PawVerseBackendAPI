using PawVerseAPI.Models.DTOs.BreedDetection;

namespace PawVerseAPI.Services.BreedDetection
{
    public interface IBreedDetectionService
    {
        /// <summary>
        /// Detect breed from uploaded image and return recommended products
        /// </summary>
        Task<DetectBreedResponse> DetectBreedAsync(IFormFile imageFile, string animalType, int maxProducts = 20);
        
        /// <summary>
        /// Get products for a specific breed
        /// </summary>
        Task<(List<BreedProductDto>, bool)> GetProductsByBreedAsync(string breedName, string animalType, int maxProducts = 20);
        
        /// <summary>
        /// Initialize Python models (called at startup)
        /// </summary>
        Task<bool> InitializeModelsAsync();
        
        /// <summary>
        /// Check if service is ready
        /// </summary>
        bool IsReady();
    }
}
