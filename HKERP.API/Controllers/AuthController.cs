using HKERP.Application.DTOs;
using HKERP.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HKERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILoginService _loginService;

        public AuthController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = _loginService.Login(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { Success = false, Message = "Error: " + ex.Message });
            }
        }
    }
}
