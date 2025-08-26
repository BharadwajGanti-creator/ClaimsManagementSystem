namespace Learning_Project.Controllers
{
    using Learning_Project.Interfaces;
    using Learning_Project.Services;
    using Microsoft.AspNetCore.Mvc;
    public class HelloController : ControllerBase
    {
        private readonly INameValidationService _nameValidationService;
        private readonly IErrorResponseService _errorService;
        // Declaration of the constructor
        public HelloController(INameValidationService nameValidationService, IErrorResponseService errorService)
        {
            _nameValidationService = nameValidationService;
            _errorService = errorService;
        }
        /// <summary>
        /// This method returns the required response to the hello endpoint.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/hello")]
        public IActionResult GetHello()
        {
            return  Ok("Hello World");
        }
        /// <summary>
        /// This method returns the required response to the hello endpoint by accepting name as the parameter.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        //[HttpGet("/hello/{name}")]
        //public IActionResult GetHelloName(string name)
        //{
        //    //Validate the name using the NameValidationService
        //    var validationResult = _nameValidationService.ValidateName(name);
        //    var traceId = HttpContext.TraceIdentifier;
        //    if (!validationResult.IsValid)
        //    {
        //        var erroResponse = _errorService.ErrorResponse(validationResult, HttpContext.Request.Path, 400,traceId);
        //        return BadRequest(erroResponse);
        //    }
        //    return Ok(new
        //    {
        //        Message = $"Hello {name}",
        //        Timestamp = DateTime.UtcNow.ToString("o")
        //    });
        //}
        /// <summary>
        /// This method returns the required response to the hello endpoint by accepting name as the parameter.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("/hello-async/{name}")]
        public async Task<IActionResult> GetHelloNameAsyncV2([FromRoute] string name, CancellationToken cancellationToken = default)
        {
            //Validate the name using the NameValidationService
            var validationResult = await _nameValidationService.ValidateNameAsync(name,cancellationToken);
            var traceId = HttpContext.TraceIdentifier;
            if (!validationResult.IsValid)
            {
                var erroResponse = _errorService.ErrorResponse(validationResult, HttpContext.Request.Path, 400, traceId);
                return BadRequest(erroResponse);
            }
            return Ok(new
            {
                Message = $"Hello {name}",
                Timestamp = DateTime.UtcNow.ToString("o")
            });
        }
        /// <summary>
        /// This methid returns the status of the hello endpoint.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/hello/status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                Version = "1.0.0",
            });
        }
    }
}
