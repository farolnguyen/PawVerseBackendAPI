using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace PawVerseAPI.Models.DTOs.BreedDetection
{
    public class DetectBreedRequest
    {
        [Required(ErrorMessage = "Image file is required")]
        public IFormFile Image { get; set; } = null!;
        
        [RegularExpression("^(dog|cat)$", ErrorMessage = "Animal type must be 'dog' or 'cat'")]
        [DefaultValue("dog")]
        public string? AnimalType { get; set; }
        
        [Range(5, 50, ErrorMessage = "MaxProducts must be between 5 and 50")]
        [DefaultValue(20)]
        public int? MaxProducts { get; set; }
    }
}
