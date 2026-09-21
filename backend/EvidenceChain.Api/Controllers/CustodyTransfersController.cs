using EvidenceChain.Application.DTOs;
using EvidenceChain.Application.Transfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EvidenceChain.Api.Controllers
{
    [Route("api/v1/custody-transfers")]
    [ApiController]
    [Authorize]
    public class CustodyTransfersController(ICustodyTransferService service) : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = "Investigador")]
        public async Task<IActionResult> Create([FromBody] CreateTransferRequestDto request)
        {
            if (!Request.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
                return Problem(title: "Falta el header Idempotency-Key", statusCode: 400);

            var requestedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await service.CreateAsync(request, key!, requestedBy);
            Response.Headers.ETag = result.ETag;
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }

        [HttpPost("{id}/accept")]
        [Authorize(Roles = "Custodio")]
        public async Task<IActionResult> Accept(Guid id)
        {
            var ifMatch = Request.Headers.IfMatch.ToString();
            if (string.IsNullOrWhiteSpace(ifMatch))
                return Problem(title: "Falta el header If-Match", statusCode: 400);

            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await service.AcceptAsync(id, ifMatch, currentUserId);
            Response.Headers.ETag = result.ETag;
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Custodio")]
        public async Task<IActionResult> Reject(Guid id)
        {
            var ifMatch = Request.Headers.IfMatch.ToString();
            if (string.IsNullOrWhiteSpace(ifMatch))
                return Problem(title: "Falta el header If-Match", statusCode: 400);

            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await service.RejectAsync(id, ifMatch, currentUserId);
            Response.Headers.ETag = result.ETag;
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Custodio")]
        public async Task<IActionResult> GetPending()
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await service.GetPendingForCustodianAsync(currentUserId);
            return Ok(result);
        }
    }
}
