using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EMS_Core_WebAPI.Services;
using EMS_Core_WebAPI.Models;

namespace EMS_Core_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(Login dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (result == null)
            {
                return Unauthorized(
                    new
                    {
                        message = "Invalid username or password."
                    }
                );
            }
            return Ok(result);
        }
    }
}
