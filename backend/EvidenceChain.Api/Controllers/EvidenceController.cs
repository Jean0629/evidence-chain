using EvidenceChain.Application.DTOs;
using EvidenceChain.Application.Evidence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EvidenceChain.Api.Controllers
{
    [Route("api/v1/evidence")]
    [ApiController]
    [Authorize]
    public class EvidenceController(IEvidenceQueries evidenceQueries, IChainVerification chainVerification) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(EvidencePageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPage([FromQuery] string? search, [FromQuery] Guid? custodianId, [FromQuery] string? cursor, [FromQuery] string? sort)
        {
            var order = string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase)
                ? SortOrder.Ascending
                : SortOrder.Descending;

            var result = await evidenceQueries.GetPageAsync(search, custodianId, cursor, order);
            return Ok(result);
        }

        [HttpGet("{id}/chain/verify")]
        [ProducesResponseType(typeof(ChainVerifyResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyChain(Guid id)
        {
            var result = await chainVerification.VerifyAsync(id);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EvidenceDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await evidenceQueries.GetByIdAsync(id);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("{id}/chain")]
        [ProducesResponseType(typeof(List<CustodyEventDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChain(Guid id)
        {
            var result = await evidenceQueries.GetChainAsync(id);
            return Ok(result);
        }
    }
}
