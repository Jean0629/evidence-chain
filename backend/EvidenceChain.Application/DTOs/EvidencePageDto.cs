namespace EvidenceChain.Application.DTOs
{
    public record EvidencePageDto(List<EvidenceListItemDto> Items, string? NextCursor);


}
