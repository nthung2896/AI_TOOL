using Hinet.Model.Entities;
using Hinet.Service.Common;
using Hinet.Service.Common.Service;
using Hinet.Service.PlatformManageService.Dto;
using Hinet.Service.PlatformManageService.Request;
using System;
using System.Threading.Tasks;

namespace Hinet.Service.PlatformManageService;

public interface IPlatformManageService : IService<PlatformManage>
{
    Task<PagedList<PlatformManageDto>> GetData(PlatformManageSearch search);
    Task<PlatformManageDto> GetDto(Guid id);
    Task<bool> TransitionStatusAsync(Guid id, int targetStatus, string note);
}
