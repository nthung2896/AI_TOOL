using Hinet.Model.Entities;
using Hinet.Service.Common;
using Hinet.Service.Common.Service;
using Hinet.Service.CompanyInfoService.Dto;
using Hinet.Service.CompanyInfoService.Request;

namespace Hinet.Service.CompanyInfoService
{
    public interface ICompanyInfoService : IService<CompanyInfo>
    {
        Task<PagedList<CompanyInfoDto>> GetData(CompanyInfoSearch search);
        Task<CompanyInfoDto> GetDto(Guid id);
        Task<CompanyInfo> UpdateStatus(CompanyInfoUpdateStatusRequest request);
    }
}
