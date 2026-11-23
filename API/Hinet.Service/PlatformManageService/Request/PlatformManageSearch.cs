using Hinet.Service.Dto;

namespace Hinet.Service.PlatformManageService.Request;

public class PlatformManageSearch : SearchBase
{
    public string? Keyword { get; set; }
    public int? Status { get; set; }
    public string? PlatformManageTypeId { get; set; }
}
