using Hinet.Model.Entities;
using Hinet.Repository.AppUserRepository;
using Hinet.Repository.DepartmentRepository;
using Hinet.Repository.RoleRepository;
using Hinet.Repository.UserRoleRepository;
using Hinet.Service.Common;
using Hinet.Service.Common.Service;
using Hinet.Service.Constant;
using Hinet.Service.DepartmentService.Dto;
using Hinet.Service.DepartmentService.Request;
using Hinet.Service.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.WebSockets;
using ZstdSharp.Unsafe;

namespace Hinet.Service.DepartmentService
{
    public class DepartmentService : Service<Department>, IDepartmentService
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IAppUserRepository _appUserRepository;
        private readonly IRoleRepository _roleRepository;

        public DepartmentService(
            IDepartmentRepository departmentRepository,
            IUserRoleRepository userRoleRepository,
            IAppUserRepository appUserRepository,
            IRoleRepository roleRepository) : base(departmentRepository)
        {
            _userRoleRepository = userRoleRepository;
            _appUserRepository = appUserRepository;
            _roleRepository = roleRepository;
        }

        public async Task<PagedList<DepartmentDto>> GetData(DepartmentSearch search)
        {
            var query = from q in GetQueryable()
                        select new DepartmentDto
                        {
                            CreatedId = q.CreatedId,
                            UpdatedId = q.UpdatedId,
                            ParentId = q.ParentId,
                            Priority = q.Priority,
                            Name = q.Name,
                            Code = q.Code,
                            ShortName = q.ShortName,
                            DiaDanh = q.DiaDanh,
                            Loai = q.Loai,
                            Level = q.Level,
                            IsActive = q.IsActive,
                            IsDeleted = q.IsDeleted,
                            Id = q.Id,
                            CreatedBy = q.CreatedBy,
                            UpdatedBy = q.UpdatedBy,
                            DeletedId = q.DeletedId,
                            CreatedDate = q.CreatedDate,
                            UpdatedDate = q.UpdatedDate,
                            DeletedDate = q.DeletedDate,
                        };

            if (search != null)
            {
                if (!string.IsNullOrEmpty(search.Name))
                    query = query.Where(x => x.Name.ToUpper().Contains(search.Name.Trim().ToUpper()));

                if (!string.IsNullOrEmpty(search.Code))
                    query = query.Where(x => x.Code.ToUpper().Contains(search.Code.Trim().ToUpper()));

                if (search.IsActive != null)
                    query = query.Where(x => x.IsActive == search.IsActive);

                if (search.Level != null)
                    query = query.Where(x => x.Level == search.Level);

                if (!string.IsNullOrEmpty(search.Loai))
                    query = query.Where(x => x.Loai.Equals(search.Loai));
            }

            query = query.OrderByDescending(x => x.CreatedDate);
            return await PagedList<DepartmentDto>.CreateAsync(query, search);
        }

        public async Task<DepartmentDto> GetDto(Guid id)
        {
            var item = await (from q in GetQueryable().Where(x => x.Id == id)
                              select new DepartmentDto
                              {
                                  CreatedId = q.CreatedId,
                                  UpdatedId = q.UpdatedId,
                                  ParentId = q.ParentId,
                                  Priority = q.Priority,
                                  Name = q.Name,
                                  Code = q.Code,
                                  Loai = q.Loai,
                                  Level = q.Level,
                                  IsActive = q.IsActive,
                                  IsDeleted = q.IsDeleted,
                                  Id = q.Id,
                                  CreatedBy = q.CreatedBy,
                                  UpdatedBy = q.UpdatedBy,
                                  DeletedId = q.DeletedId,
                                  CreatedDate = q.CreatedDate,
                                  UpdatedDate = q.UpdatedDate,
                                  DeletedDate = q.DeletedDate,
                              }).FirstOrDefaultAsync();

            return item ?? throw new Exception("Department not found");
        }

        public async Task<DepartmentDto> GetDetail(Guid id)
        {
            var currentDept = await GetByIdAsync(id) ?? throw new Exception("Department not found");

            var deptUser = new List<DepartmentUser>();
            if (currentDept.Loai.Equals(DepartmentTypeConstant.Phong))
            {
                var userIds = _userRoleRepository.GetQueryable().Where(x => x.DepartmentId.Equals(id)).Select(x => x.UserId);
                deptUser = _appUserRepository
                    .GetQueryable()
                    .Where(x => userIds.Contains(x.Id))
                    .Select(x => new DepartmentUser
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).ToList();
            }

            return new DepartmentDto
            {
                CreatedId = currentDept.CreatedId,
                UpdatedId = currentDept.UpdatedId,
                ParentId = currentDept.ParentId,
                Priority = currentDept.Priority,
                Name = currentDept.Name,
                Code = currentDept.Code,
                Loai = currentDept.Loai,
                Level = currentDept.Level,
                IsActive = currentDept.IsActive,
                IsDeleted = currentDept.IsDeleted,
                Id = currentDept.Id,
                CreatedBy = currentDept.CreatedBy,
                UpdatedBy = currentDept.UpdatedBy,
                DeletedId = currentDept.DeletedId,
                CreatedDate = currentDept.CreatedDate,
                UpdatedDate = currentDept.UpdatedDate,
                DeletedDate = currentDept.DeletedDate,
                Users = deptUser,
            };
        }

        public List<DepartmentHierarchy> GetDepartmentHierarchy()
        {
            var departments = GetQueryable()
                .Select(d => new DepartmentHierarchy
                {
                    Id = d.Id,
                    Title = d.Name,
                    Code = d.Code,
                    ShortName = d.ShortName,
                    DiaDanh = d.DiaDanh,
                    ParentId = d.ParentId,
                    Priority = d.Priority,
                    Level = d.Level,
                    Loai = d.Loai,
                    IsActive = d.IsActive,
                })
                .OrderBy(d => d.Priority).ToList();

            var departmentHierarchy = departments
                .Where(d => d.ParentId == null)
                .Select(d => new DepartmentHierarchy
                {
                    Id = d.Id,
                    Title = d.Title,
                    Code = d.Code,
                    ParentId = d.ParentId,
                    Priority = d.Priority,
                    Level = d.Level,
                    Loai = d.Loai,
                    ShortName = d.ShortName,
                    DiaDanh = d.DiaDanh,
                    IsActive = d.IsActive,
                    Children = GetChildren(d.Id, departments)
                });

            return departmentHierarchy.ToList();
        }

        private List<DepartmentHierarchy>? GetChildren(Guid parentId, List<DepartmentHierarchy> departments)
        {
            var children = departments
                .Where(d => d.ParentId == parentId)
                .Select(d => new DepartmentHierarchy
                {
                    Id = d.Id,
                    Title = d.Title,
                    Code = d.Code,
                    ParentId = d.ParentId,
                    ShortName = d.ShortName,
                    Priority = d.Priority,
                    Level = d.Level,
                    Loai = d.Loai,
                    IsActive = d.IsActive,
                    Children = GetChildren(d.Id, departments)
                })
                .ToList();
            return children.Any() ? children : null;
        }

        public async Task<List<DropdownOption>> GetDropDown(string? selected)
        {
            try
            {
                return await (from departmentTbl in GetQueryable()
                              select new DropdownOption
                              {
                                  Label = departmentTbl.Name,
                                  Value = departmentTbl.Code,
                                  Selected = selected != null ? selected == departmentTbl.Id.ToString() : false
                              }).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve dropdown options: " + ex.Message);
            }
        }


        #region Hàm lấy dropdown phòng ban dưới dạng cây dùng trong TreeSelect của antd

        public async Task<List<DropdownOptionTree>> GetDropdownTreeOption(bool disabledParent = true)
        {
            var departments = await GetQueryable()
                .Where(x => x.IsActive).ToListAsync();

            var rootDepartments = departments.Where(d => d.ParentId == null).ToList();
            var result = new List<DropdownOptionTree>();

            foreach (var dept in rootDepartments)
            {
                result.Add(BuildTree(dept, departments, disabledParent));
            }

            return result;
        }

        public async Task<List<DropdownOptionTree>> GetDropdownTreeOptionByUserDepartment(bool disabledParent = true, Guid? donViId = null)
        {
            var result = new List<DropdownOptionTree>();

            var departments = await GetQueryable()
                .Where(x => x.IsActive).ToListAsync() ?? new List<Department>();

            List<Department>? rootDepartments;

            // nếu là admin thì lấy hết phòng ban
            if (donViId != null)
            {
                var dpName = departments.FirstOrDefault(x => x.Id == donViId)?.Name;
                result.Add(new DropdownOptionTree()
                {
                    Children = null,
                    Disabled = false,
                    Title = dpName,
                    Value = donViId.ToString(),
                });
            }
            // chỉ lấy chính phòng đó
            else
            {
                rootDepartments = departments.Where(x => x.ParentId == donViId).ToList();
                foreach (var dept in rootDepartments)
                {
                    result.Add(BuildTree(dept, departments, disabledParent));
                }

            }

            return result;
        }

        public async Task<List<DropdownOptionTree>> GetSubAndCurrentUnitDropdownTreeByUserDepartment(bool disabledParent = true, Guid? donViId = null)
        {
            var result = new List<DropdownOptionTree>();
            var departments = await GetQueryable()
                .Where(x => x.IsActive).ToListAsync() ?? new List<Department>();
            var department = departments.Where(x => x.Id == donViId).FirstOrDefault();
            result.Add(BuildTree(department, departments, disabledParent));
            return result;
        }


        private DropdownOptionTree BuildTree(Department department, List<Department> allDepartments, bool disabledParent)
        {
            var children = allDepartments.Where(d => d.ParentId == department.Id).ToList();

            return new DropdownOptionTree
            {
                Value = department.Id.ToString().ToLower(),
                Title = department.Name,
                Disabled = disabledParent && department.ParentId == null,
                Children = children.Any()
                    ? children.Select(child => BuildTree(child, allDepartments, disabledParent)).ToList()
                    : new List<DropdownOptionTree>()
            };
        }

        #endregion Hàm lấy dropdown phòng ban dưới dạng cây dùng trong TreeSelect của antd

        public List<DepartmentVM> BuildDepartmentHierarchy()
        {
            var departments = GetQueryable().ToList();
            return GetDepartmentHierarchy(departments, null);
        }

        public async Task<List<DropdownOption>> GetDropdownListKCQCN()
        {
            return GetQueryable().Where(x => x.Loai == DepartmentTypeConstant.KhoiCoQuanChiNhanh).Select(x => new DropdownOption
            {
                Value = x.Code,
                Label = x.Name

            }).ToList();

        }
        public async Task<List<DropdownOption>> GropdownDownPhongByKCQCN(List<string> codes)
        {
            if (codes == null || !codes.Any())
            {
                return new List<DropdownOption>();
            }

            var allItems = GetQueryable().Where(x => x.Loai == DepartmentTypeConstant.Phong).ToList();
            var lookup = allItems.ToLookup(x => x.ParentId);
            var result = new List<DropdownOption>();

            foreach (var code in codes)
            {
                var kcqcn = FindBy(x => x.Code == code).Select(x => x.Id).FirstOrDefault();
                if (kcqcn != Guid.Empty)
                {
                    BuildDropdown(kcqcn, lookup, result);
                }
            }

            return result;
        }


        private void BuildDropdown(
            Guid parentId,
            ILookup<Guid?, Department> lookup,
            List<DropdownOption> result
        )
        {
            foreach (var item in lookup[parentId])
            {
                result.Add(new DropdownOption
                {
                    Value = item.Id.ToString(),
                    Label = item.Name
                });

                BuildDropdown(item.Id, lookup, result);
            }
        }
        private List<DepartmentVM> GetDepartmentHierarchy(List<Department> departments, Guid? parentId = null)
        {
            return departments
                .Where(d => d.ParentId == parentId)
                .Select(d => new DepartmentVM
                {
                    Id = d.Id,
                    Name = d.Name,
                    Code = d.Code,
                    ParentId = d.ParentId,
                    Priority = d.Priority,
                    Level = d.Level,
                    IsActive = d.IsActive,
                    DepartmentChilds = GetDepartmentHierarchy(departments, d.Id)
                })
                .ToList();
        }

        public async Task<List<DepartmentExport>> GetDepartmentExportData(string type)
        {
            var query = (from q in GetQueryable().Where(x => x.Loai.Equals(type))
                         join d in GetQueryable()
                         on q.ParentId equals d.Id into departmentGroup
                         from dept in departmentGroup.DefaultIfEmpty()
                         select new
                         {
                             Parent = dept != null ? dept.Name : "",
                             Name = q.Name,
                             Code = q.Code,
                             IsActive = q.IsActive,
                             CreatedDate = q.CreatedDate,
                         }).OrderByDescending(x => x.CreatedDate);

            var departments = await query.AsNoTracking().ToListAsync();
            return departments
                .Select((item, index) => new DepartmentExport
                {
                    STT = index + 1,
                    Name = item.Name,
                    Code = item.Code,
                    Status = item.IsActive ? "Hoạt động" : "Khoá",
                    Parent = item.Parent,
                    CreatedDate = item.CreatedDate.ToString("dd/MM/yyyy"),
                }).ToList();
        }

        // Lấy id các đơn vị con  
        public List<Guid> GetChildIds(List<Guid> ids)
        {
            var result = new List<Guid>();
            if (ids == null || !ids.Any()) return result;
            var childIds = FindBy(x => x.ParentId != null && ids.Contains(x.ParentId.Value))
            .Select(x => x.Id)
            .ToList();

            if (childIds != null && childIds.Any())
            {
                result.AddRange(childIds);
                result.AddRange(GetChildIds(childIds));
            }
            return result;
        }

        public async Task<List<DropdownOption>> GetDropdownDonViId(Guid? IdDepartment)
        {
            var query = await (from q in GetQueryable().Where(x => x.Id == IdDepartment)
                               select new DropdownOption
                               {
                                   Label = q.Name,
                                   Value = q.Id.ToString()
                               }).ToListAsync();
            return query;
        }


        //Lấy dropdown đơn vị
        public async Task<List<DropdownOptionTree>?> GetDropdownDonVi(bool disabledParent = true,
            Guid? donViId = null)
        {
            var result = new List<DropdownOptionTree>();

            var departments = await GetQueryable()
                .Where(x => x.IsActive).ToListAsync() ?? new List<Department>();

            List<Department>? rootDepartments;

            // nếu là admin thì lấy hết phòng ban
            if (donViId != null)
            {
                var dpName = departments.FirstOrDefault(x => x.Id == donViId)?.Name;
                result.Add(new DropdownOptionTree()
                {
                    Children = null,
                    Disabled = false,
                    Title = dpName,
                    Value = donViId.ToString(),
                });
            }
            // chỉ lấy chính phòng đó
            else
            {
                rootDepartments = departments.Where(x => x.ParentId == donViId).ToList();
                foreach (var dept in rootDepartments)
                {
                    result.Add(BuildTree(dept, departments, true));
                }
            }

            return result;
        }

        public async Task<List<DropdownOption>> GetDropdownDonVi()
        {
            var query = await (from q in GetQueryable()
                               select new DropdownOption
                               {
                                   Label = q.Name,
                                   Value = q.Id.ToString()
                               }).ToListAsync();
            return query;
        }

        public async Task<List<DropdownOption>> GetDropdownNhomNhanSu(bool? parent = true)
        {

            var query = await (from q in GetQueryable()
                               where parent == true && q.Level == 0 || parent == false && q.Level == 1
                               select new DropdownOption
                               {
                                   Label = q.Name,
                                   Value = q.Id.ToString()
                               }).ToListAsync();
            return query;
        }
    }
}