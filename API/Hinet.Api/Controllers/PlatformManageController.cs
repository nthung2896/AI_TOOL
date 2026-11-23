using Hinet.Api.Dto;
using Hinet.Controllers;
using Hinet.Service.Common;
using Hinet.Service.PlatformManageService;
using Hinet.Service.PlatformManageService.Dto;
using Hinet.Service.PlatformManageService.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Hinet.Api.Controllers;

[Route("api/[controller]")]
public class PlatformManageController(
    IPlatformManageService service, 
    ILogger<PlatformManageController> logger) : HinetController
{
    [HttpPost("GetData")]
    public async Task<DataResponse<PagedList<PlatformManageDto>>> GetData([FromBody] PlatformManageSearch search)
    {
        try
        {
            var data = await service.GetData(search);
            return DataResponse<PagedList<PlatformManageDto>>.Success(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi lấy danh sách nền tảng");
            return DataResponse<PagedList<PlatformManageDto>>.False(ex.Message);
        }
    }

    [HttpGet("Get/{id}")]
    public async Task<DataResponse<PlatformManageDto>> Get(Guid id)
    {
        try
        {
            var dto = await service.GetDto(id);
            return DataResponse<PlatformManageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi lấy chi tiết nền tảng {Id}", id);
            return DataResponse<PlatformManageDto>.False(ex.Message);
        }
    }

    [HttpPost("Transition")]
    public async Task<DataResponse<object>> Transition([FromBody] PlatformTransitionRequest request)
    {
        try
        {
            var result = await service.TransitionStatusAsync(request.Id, request.TargetStatus, request.Note ?? string.Empty);
            return DataResponse<object>.Success(result, "Chuyển trạng thái hồ sơ thành công");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi chuyển trạng thái hồ sơ {Id}", request.Id);
            return DataResponse<object>.False(ex.Message);
        }
    }
}

public class PlatformTransitionRequest
{
    public Guid Id { get; set; }
    public int TargetStatus { get; set; }
    public string? Note { get; set; }
}
