using Hinet.Model.Entities;
using Hinet.Repository.CompanyInfoRepository;
using Hinet.Service.Common;
using Hinet.Service.Common.Service;
using Hinet.Service.CompanyInfoService.Dto;
using Hinet.Service.CompanyInfoService.Request;
using Hinet.Service.Constant;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Reflection;

namespace Hinet.Service.CompanyInfoService
{
    public class CompanyInfoService : Service<CompanyInfo>, ICompanyInfoService
    {
        public CompanyInfoService(ICompanyInfoRepository repository) : base(repository)
        {
        }

        public async Task<PagedList<CompanyInfoDto>> GetData(CompanyInfoSearch search)
        {
            var query = GetQueryable().Select(q => new CompanyInfoDto
            {
                Id = q.Id,
                Status = q.Status,
                Name = q.Name,
                EnglishName = q.EnglishName,
                ShortName = q.ShortName,
                TaxCode = q.TaxCode,
                Address = q.Address,
                CityId = q.CityId,
                CityName = q.CityName,
                Phone = q.Phone,
                Fax = q.Fax,
                Email = q.Email,
                RepresenterName = q.RepresenterName,
                RepresenterMobile = q.RepresenterMobile,
                RepresenterPhone = q.RepresenterPhone,
                RepresenterEmail = q.RepresenterEmail,
                RepresenterCCCD = q.RepresenterCCCD,
                Detail = q.Detail,
                ApproveDateOnline = q.ApproveDateOnline,
                TinhId = q.TinhId,
                TinhIdNew = q.TinhIdNew,
                XaId = q.XaId,
                QuocGiaId = q.QuocGiaId,
                TypeOrganization = q.TypeOrganization,
                IsNuocNgoai = q.IsNuocNgoai,
                IsVonDauTuNuocNgoai = q.IsVonDauTuNuocNgoai,
                DKKD = q.DKKD,
                CreatedDate = q.CreatedDate,
                UpdatedDate = q.UpdatedDate,
                CreatedBy = q.CreatedBy,
                UpdatedBy = q.UpdatedBy,
            });

            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Keyword))
                {
                    var k = search.Keyword.Trim().ToUpper();
                    query = query.Where(x =>
                        (x.Name != null && x.Name.ToUpper().Contains(k)) ||
                        (x.TaxCode != null && x.TaxCode.ToUpper().Contains(k)) ||
                        (x.Email != null && x.Email.ToUpper().Contains(k)) ||
                        (x.RepresenterName != null && x.RepresenterName.ToUpper().Contains(k)));
                }

                if (!string.IsNullOrWhiteSpace(search.TaxCode))
                    query = query.Where(x => x.TaxCode != null && x.TaxCode.Contains(search.TaxCode.Trim()));

                if (!string.IsNullOrWhiteSpace(search.Name))
                    query = query.Where(x => x.Name != null && x.Name.ToUpper().Contains(search.Name.Trim().ToUpper()));

                if (search.Status.HasValue)
                    query = query.Where(x => x.Status == search.Status.Value);

                if (!string.IsNullOrWhiteSpace(search.TypeOrganization))
                    query = query.Where(x => x.TypeOrganization == search.TypeOrganization);

                if (search.TinhId.HasValue)
                    query = query.Where(x => x.TinhId == search.TinhId.Value);

                if (search.TinhIdNew.HasValue)
                    query = query.Where(x => x.TinhIdNew == search.TinhIdNew.Value);

                if (search.IsNuocNgoai.HasValue)
                    query = query.Where(x => x.IsNuocNgoai == search.IsNuocNgoai.Value);
            }

            query = query.OrderByDescending(x => x.CreatedDate);
            var result = await PagedList<CompanyInfoDto>.CreateAsync(query, search ?? new CompanyInfoSearch());

            foreach (var item in result.Items)
            {
                item.StatusName = GetStatusName(item.Status);
                item.TypeOrganizationName = GetTypeOrganizationName(item.TypeOrganization);
            }
            return result;
        }

        public async Task<CompanyInfoDto> GetDto(Guid id)
        {
            var item = await GetQueryable().Where(x => x.Id == id).Select(q => new CompanyInfoDto
            {
                Id = q.Id,
                Status = q.Status,
                Name = q.Name,
                EnglishName = q.EnglishName,
                ShortName = q.ShortName,
                TaxCode = q.TaxCode,
                Address = q.Address,
                CityId = q.CityId,
                CityName = q.CityName,
                Phone = q.Phone,
                Fax = q.Fax,
                Email = q.Email,
                RepresenterName = q.RepresenterName,
                RepresenterMobile = q.RepresenterMobile,
                RepresenterPhone = q.RepresenterPhone,
                RepresenterEmail = q.RepresenterEmail,
                RepresenterCCCD = q.RepresenterCCCD,
                Detail = q.Detail,
                ApproveDateOnline = q.ApproveDateOnline,
                TinhId = q.TinhId,
                TinhIdNew = q.TinhIdNew,
                XaId = q.XaId,
                QuocGiaId = q.QuocGiaId,
                TypeOrganization = q.TypeOrganization,
                IsNuocNgoai = q.IsNuocNgoai,
                IsVonDauTuNuocNgoai = q.IsVonDauTuNuocNgoai,
                DKKD = q.DKKD,
                CreatedDate = q.CreatedDate,
                UpdatedDate = q.UpdatedDate,
                CreatedBy = q.CreatedBy,
                UpdatedBy = q.UpdatedBy,
            }).FirstOrDefaultAsync() ?? throw new Exception("Không tìm thấy thông tin doanh nghiệp");

            item.StatusName = GetStatusName(item.Status);
            item.TypeOrganizationName = GetTypeOrganizationName(item.TypeOrganization);
            return item;
        }

        public async Task<CompanyInfo> UpdateStatus(CompanyInfoUpdateStatusRequest request)
        {
            var entity = await GetByIdOrThrowAsync(request.Id);
            entity.Status = request.Status;
            await UpdateAsync(entity);
            return entity;
        }

        private static string? GetStatusName(int status)
        {
            return GetDisplayName(typeof(CompanyInfoStatusConstant), status);
        }

        private static string? GetTypeOrganizationName(string? code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            return GetDisplayName(typeof(TypeOrganizationConstant), code) ?? code;
        }

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
    }
}
