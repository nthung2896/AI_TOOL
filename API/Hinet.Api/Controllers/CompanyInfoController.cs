using Hinet.Api.Dto;
using Hinet.Service.Common;
using Hinet.Service.CompanyInfoService;
using Hinet.Service.CompanyInfoService.Dto;
using Hinet.Service.CompanyInfoService.Request;
using Microsoft.AspNetCore.Mvc;

namespace Hinet.Controllers
{
    [Route("api/[controller]")]
    public class CompanyInfoController : HinetController
    {
        private readonly ICompanyInfoService _service;
        private readonly ILogger<CompanyInfoController> _logger;

        public CompanyInfoController(ICompanyInfoService service, ILogger<CompanyInfoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("GetData")]
        public async Task<DataResponse<PagedList<CompanyInfoDto>>> GetData([FromBody] CompanyInfoSearch search)
        {
            try
            {
                var data = await _service.GetData(search);
                return DataResponse<PagedList<CompanyInfoDto>>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách doanh nghiệp");
                return DataResponse<PagedList<CompanyInfoDto>>.False(ex.Message);
            }
        }

        [HttpGet("Get/{id}")]
        public async Task<DataResponse<CompanyInfoDto>> Get(Guid id)
        {
            try
            {
                var dto = await _service.GetDto(id);
                return DataResponse<CompanyInfoDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết doanh nghiệp {Id}", id);
                return DataResponse<CompanyInfoDto>.False(ex.Message);
            }
        }

        [HttpPut("UpdateStatus")]
        public async Task<DataResponse<object>> UpdateStatus([FromBody] CompanyInfoUpdateStatusRequest request)
        {
            try
            {
                var entity = await _service.UpdateStatus(request);
                return DataResponse<object>.Success(entity, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái doanh nghiệp {Id}", request.Id);
                return DataResponse<object>.False(ex.Message);
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<DataResponse> Delete(Guid id)
        {
            try
            {
                var entity = await _service.GetByIdOrThrowAsync(id);
                await _service.DeleteAsync(entity);
                return DataResponse.Success(null, "Xoá thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xoá doanh nghiệp {Id}", id);
                return DataResponse.False(ex.Message);
            }
        }
    }
}
