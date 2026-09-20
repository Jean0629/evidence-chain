using EvidenceChain.Application.DTOs;
using EvidenceChain.Application.Transfers;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EvidenceChain.Api.Controllers
{
    [Route("api/v1/custody-transfers")]
    [ApiController]
    public class CustodyTransfersController(ICustodyTransferService service) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransferRequestDto request)
        {
            if (!Request.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
                return Problem(title: "Falta el header Idempotency-Key", statusCode: 400);

            // TODO: reemplazar por el Id del custodio autenticado (claim del JWT), falta implementar auth
            var requestedBy = request.EvidenceId;

            var result = await service.CreateAsync(request, key!, requestedBy);
            Response.Headers.ETag = result.ETag;
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }

        [HttpPost("{id}/accept")]
        public async Task<IActionResult> Accept(Guid id)
        {
            var ifMatch = Request.Headers.IfMatch.ToString();
            if (string.IsNullOrWhiteSpace(ifMatch))
                return Problem(title: "Falta el header If-Match", statusCode: 400);

            var result = await service.AcceptAsync(id, ifMatch);
            Response.Headers.ETag = result.ETag;
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id)
        {
            var ifMatch = Request.Headers.IfMatch.ToString();
            if (string.IsNullOrWhiteSpace(ifMatch))
                return Problem(title: "Falta el header If-Match", statusCode: 400);

            var result = await service.RejectAsync(id, ifMatch);
            Response.Headers.ETag = result.ETag;
            return Ok(result);
        }
    }
}
