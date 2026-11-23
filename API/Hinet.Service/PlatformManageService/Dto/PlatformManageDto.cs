using System;

namespace Hinet.Service.PlatformManageService.Dto;

public class PlatformManageDto
{
    public Guid Id { get; set; }
    public int Status { get; set; }
    public string? StatusName { get; set; }
    public string? PlatformManageTypeId { get; set; }
    public string? PlatformManageTypeName { get; set; }
    public long? OrganizationId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyTaxCode { get; set; }
    public string? Name { get; set; }
    public string? Domain { get; set; }
    public string? RepresenterName { get; set; }
    public string? RepresenterMobile { get; set; }
    public string? RepresenterEmail { get; set; }
    public DateTime CreatedDate { get; set; }
}
