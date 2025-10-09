using Microsoft.AspNetCore.Mvc;

namespace PawVerseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "PawVerse API is running successfully!",
                timestamp = DateTime.Now,
                version = "1.0.0"
            });
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.Now
            });
        }
    }
}
