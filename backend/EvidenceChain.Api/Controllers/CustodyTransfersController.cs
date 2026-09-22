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
        [ProducesResponseType(typeof(TransferResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateTransferRequestDto request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return Problem(title: "Falta el header Idempotency-Key", statusCode: 400);

            var requestedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await service.CreateAsync(request, idempotencyKey, requestedBy);
            Response.Headers.ETag = result.ETag;
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }

        [HttpPost("{id}/accept")]
        [Authorize(Roles = "Custodio")]
        [ProducesResponseType(typeof(TransferResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Accept(Guid id, [FromHeader(Name = "If-Match")] string ifMatch)
        {
            if (string.IsNullOrWhiteSpace(ifMatch))
                return Problem(title: "Falta el header If-Match", statusCode: 400);

            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await service.AcceptAsync(id, ifMatch, currentUserId);
            Response.Headers.ETag = result.ETag;
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Custodio")]
        [ProducesResponseType(typeof(TransferResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reject(Guid id, [FromHeader(Name = "If-Match")] string ifMatch)
        {
            if (string.IsNullOrWhiteSpace(ifMatch))
                return Problem(title: "Falta el header If-Match", statusCode: 400);

            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await service.RejectAsync(id, ifMatch, currentUserId);
            Response.Headers.ETag = result.ETag;
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Custodio")]
        [ProducesResponseType(typeof(List<MyPendingTransferDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPending()
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await service.GetPendingForCustodianAsync(currentUserId);
            return Ok(result);
        }
    }
}
