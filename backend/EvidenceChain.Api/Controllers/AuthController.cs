using EvidenceChain.Application.Auth;
using EvidenceChain.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EvidenceChain.Api.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController(EvidenceChainDbContext db, ITokenService tokenService) : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var custodian = await db.Custodians
                .FirstOrDefaultAsync(c => c.LoginCode == request.LoginCode);
            if (custodian is null)
                return Problem(title: "Código de acceso no encontrado", statusCode: 404);

            var token = tokenService.GenerateToken(custodian);
            return Ok(new LoginResponse(token, custodian.Id, custodian.DisplayName, custodian.Role.ToString()));
        }

    }
}
