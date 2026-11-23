using Hinet.Model.Entities;
using Hinet.Repository.PlatformManageRepository;
using Hinet.Service.Common;
using Hinet.Service.Common.Service;
using Hinet.Service.Constant;
using Hinet.Service.PlatformManageService.Dto;
using Hinet.Service.PlatformManageService.Request;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hinet.Service.PlatformManageService;

// Tận dụng C# 12 Primary Constructor để tự động inject Repository
public class PlatformManageService(IPlatformManageRepository repository) 
    : Service<PlatformManage>(repository), IPlatformManageService
{
    public async Task<PagedList<PlatformManageDto>> GetData(PlatformManageSearch search)
    {
        var query = GetQueryable().Select(q => new PlatformManageDto
        {
            Id = q.Id,
            Status = q.Status,
            PlatformManageTypeId = q.AppManageTypeId != null ? q.AppManageTypeId.ToString() : null,
            OrganizationId = q.OrganizationId,
            CompanyName = q.CompanyName,
            CompanyTaxCode = q.CompanyTaxCode,
            Name = q.Name,
            Domain = q.Domain,
            RepresenterName = q.RepresenterName,
            RepresenterMobile = q.RepresenterMobile,
            RepresenterEmail = q.RepresenterEmail,
            CreatedDate = q.CreatedDate
        });

        if (search != null)
        {
            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var k = search.Keyword.Trim().ToUpper();
                query = query.Where(x =>
                    (x.Name != null && x.Name.ToUpper().Contains(k)) ||
                    (x.CompanyName != null && x.CompanyName.ToUpper().Contains(k)) ||
                    (x.CompanyTaxCode != null && x.CompanyTaxCode.ToUpper().Contains(k)));
            }

            if (search.Status.HasValue)
                query = query.Where(x => x.Status == search.Status.Value);

            if (!string.IsNullOrWhiteSpace(search.PlatformManageTypeId))
                query = query.Where(x => x.PlatformManageTypeId == search.PlatformManageTypeId);
        }

        query = query.OrderByDescending(x => x.CreatedDate);
        var result = await PagedList<PlatformManageDto>.CreateAsync(query, search ?? new PlatformManageSearch());

        foreach (var item in result.Items)
        {
            item.StatusName = GetStatusName(item.Status);
            item.PlatformManageTypeName = GetTypeName(item.PlatformManageTypeId);
        }

        return result;
    }

    public async Task<PlatformManageDto> GetDto(Guid id)
    {
        var item = await GetQueryable().Where(x => x.Id == id).Select(q => new PlatformManageDto
        {
            Id = q.Id,
            Status = q.Status,
            PlatformManageTypeId = q.AppManageTypeId != null ? q.AppManageTypeId.ToString() : null,
            OrganizationId = q.OrganizationId,
            CompanyName = q.CompanyName,
            CompanyTaxCode = q.CompanyTaxCode,
            Name = q.Name,
            Domain = q.Domain,
            RepresenterName = q.RepresenterName,
            RepresenterMobile = q.RepresenterMobile,
            RepresenterEmail = q.RepresenterEmail,
            CreatedDate = q.CreatedDate
        }).FirstOrDefaultAsync() ?? throw new Exception("Không tìm thấy thông tin nền tảng");

        item.StatusName = GetStatusName(item.Status);
        item.PlatformManageTypeName = GetTypeName(item.PlatformManageTypeId);
        return item;
    }

    public async Task<bool> TransitionStatusAsync(Guid id, int targetStatus, string note)
    {
        var hoso = await GetByIdOrThrowAsync(id);
        
        // Tận dụng C# 12 Pattern Matching & Switch Expression để kiểm soát Workflow
        var isValidTransition = (hoso.Status, targetStatus) switch
        {
            // Luồng Chuyên viên xử lý từ Chờ duyệt (1)
            (1, 4) => true, // Duyệt điện tử -> Chờ Trưởng phòng duyệt
            (1, 6) => true, // Yêu cầu bổ sung -> Doanh nghiệp
            (1, 3) => true, // Từ chối
            (1, 25) => true, // Xin ý kiến phối hợp liên ngành

            // Luồng Chuyên viên từ Đang xin ý kiến (25)
            (25, 4) => true, // Duyệt điện tử
            (25, 6) => true, // Yêu cầu bổ sung
            (25, 3) => true, // Từ chối

            // Luồng Trưởng phòng xử lý từ Đã duyệt điện tử (4)
            (4, 26) => true, // Đã review -> Trình Lãnh đạo Cục
            (4, 28) => true, // Đề nghị nộp bản cứng
            (4, 6) => true,  // Yêu cầu bổ sung
            (4, 3) => true,  // Từ chối

            // Luồng Trưởng phòng xử lý sau khi nộp bản cứng (28)
            (28, 26) => true, // Đã review -> Trình Lãnh đạo Cục
            (28, 6) => true,  // Yêu cầu bổ sung
            (28, 3) => true,  // Từ chối

            // Luồng Lãnh đạo xử lý từ Đã review (26)
            (26, 5) => true, // Lãnh đạo XÁC NHẬN (Thành công)
            (26, 6) => true, // Yêu cầu bổ sung
            (26, 3) => true, // Từ chối

            // Luồng Doanh nghiệp nộp lại hồ sơ sau khi bổ sung (6) hoặc chỉnh sửa (2)
            (6, 1) => true,  // Nộp lại -> Chờ duyệt
            (2, 1) => true,  // Nộp lại -> Chờ duyệt

            _ => false // Luồng đi không hợp lệ!
        };

        if (!isValidTransition)
        {
            throw new InvalidOperationException($"Không thể chuyển trạng thái hồ sơ từ {GetStatusName(hoso.Status)} sang {GetStatusName(targetStatus)}.");
        }

        hoso.Status = targetStatus;
        
        // Ghi nhận nghiệp vụ đặc thù
        if (targetStatus == PlatformStatusConstant.DaDuyetDienTu)
        {
            hoso.ReviewDate = DateTime.Now;
        }
        else if (targetStatus == PlatformStatusConstant.CanBoSungThongTin)
        {
            hoso.RequestChangeDate = DateTime.Now;
        }
        else if (targetStatus == PlatformStatusConstant.DaXacNhan)
        {
            hoso.SubmitDate = DateTime.Now; // Ngày chính thức phê duyệt
        }

        await UpdateAsync(hoso);

        // TODO: Ghi nhận lịch sử (History) và đồng bộ API thuế/DVC nếu targetStatus == 5
        if (targetStatus == PlatformStatusConstant.DaXacNhan)
        {
            await SynchDataToDvcAndTaxAsync(hoso);
        }

        return true;
    }

    #region Helpers

    private static string? GetStatusName(int status) => 
        GetDisplayName(typeof(PlatformStatusConstant), status);

    private static string? GetTypeName(string? typeCode) => 
        typeCode != null ? GetDisplayName(typeof(PlatformManageTypeConstant), typeCode) : null;

    private static string? GetDisplayName(Type constantType, object value)
    {
        foreach (var prop in constantType.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            var v = prop.GetValue(null);
            if (v != null && v.Equals(value))
            {
                var attr = prop.GetCustomAttribute<DisplayNameAttribute>();
                return attr?.DisplayName ?? prop.Name;
            }
        }
        return null;
    }

    private async Task SynchDataToDvcAndTaxAsync(PlatformManage hoso)
    {
        // Logic đồng bộ API sang Cổng Dịch vụ công & Thuế quốc gia
        await Task.CompletedTask;
    }

    #endregion
}
