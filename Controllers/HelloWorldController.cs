namespace HelloWorldPlusApi.Controllers;
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/[controller]")]
public class HelloWorldController : ControllerBase
{
    // This controller handles requests to the /api/helloworld endpoint
    //GET: /api/helloworld
    [HttpGet]
    public IActionResult GetHello()
    {
         return Ok("Hello World!");
    }
    //GET: /api/helloworld/{name}
    [HttpGet("{name}")]
    public IActionResult GetHelloName(string name)
    {
        //Required and length validation
        if(string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 32)
        {
            return BadRequest("Name must be between 2 and 32 characters long.");
        }
        //Character restriction only allow letters and spaces
        if(!name.All(char.IsLetter))
            return BadRequest("Name can only contain letters and spaces.");
        //Return greeting
        return Ok($"Hello {name}!");
    }
    //GET /api/status
    [HttpGet("/api/status")]
    public IActionResult GetStatus()
    {
        var status = new
        {
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        };
        return Ok(status);
    }
}
