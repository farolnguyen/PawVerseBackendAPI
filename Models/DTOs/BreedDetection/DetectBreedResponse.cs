namespace PawVerseAPI.Models.DTOs.BreedDetection
{
    public class DetectBreedResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
        
        // Main result (top breed)
        public string? Breed { get; set; }
        public float Confidence { get; set; }
        public string? AnimalType { get; set; }
        
        // Top K breed candidates
        public List<BreedCandidate>? TopBreeds { get; set; }
        
        // Recommended products for selected breed
        public List<BreedProductDto>? RecommendedProducts { get; set; }
        
        // Metadata
        public BreedDetectionMetadata? Metadata { get; set; }
    }
    
    public class BreedCandidate
    {
        public string Breed { get; set; } = string.Empty;
        public float Score { get; set; }
        public int Rank { get; set; }
    }
    
    public class BreedDetectionMetadata
    {
        [System.Text.Json.Serialization.JsonPropertyName("animal_detected")]
        public bool AnimalDetected { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("detection_confidence")]
        public float DetectionConfidence { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("bounding_box")]
        public int[]? BoundingBox { get; set; }  // [x1, y1, x2, y2]
        
        [System.Text.Json.Serialization.JsonPropertyName("processing_time_ms")]
        public int ProcessingTimeMs { get; set; }
    }
}
