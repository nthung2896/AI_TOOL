using CommonHelper.Excel;
using CommonHelper.String;
using Hinet.Api.Dto;
using Hinet.Api.Filter;
using Hinet.Model.Entities;
using Hinet.Service.AppUserService.Dto;
using Hinet.Service.AspNetUsersService;
using Hinet.Service.AspNetUsersService.Dto;
using Hinet.Service.AspNetUsersService.Request;
using Hinet.Service.Common;
using Hinet.Service.Constant;
using Hinet.Service.Core.Mapper;
using Hinet.Service.DM_DuLieuDanhMucService;
using Hinet.Service.Dto;
using Hinet.Service.RoleService;
using Hinet.Service.UserRoleService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Net.WebSockets;

namespace Hinet.Controllers
{
    [Route("api/[controller]")]
    public class AspNetUsersController : HinetController
    {
        private readonly IAspNetUsersService _aspNetUsersService;
        private readonly IMapper _mapper;
        private readonly ILogger<AspNetUsersController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IRoleService _roleService;
        private readonly IUserRoleService _userRoleService;
        private readonly IDM_DuLieuDanhMucService _dM_DuLieuDanhMucService;


        public AspNetUsersController(
            IAspNetUsersService aspNetUsersService,
            IMapper mapper,
            IDM_DuLieuDanhMucService dM_DuLieuDanhMucService,
            ILogger<AspNetUsersController> logger,
            UserManager<AppUser> userManager,
            IRoleService roleService,
            IUserRoleService userRoleService
            )
        {
            _aspNetUsersService = aspNetUsersService;
            _mapper = mapper;
            _dM_DuLieuDanhMucService = dM_DuLieuDanhMucService;
            _logger = logger;
            _userManager = userManager;
            _roleService = roleService;
            _userRoleService = userRoleService;
        }


        [HttpPost("Create")]
        public async Task<DataResponse<AppUser>> Create([FromBody] AspNetUsersRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _aspNetUsersService.CreateAccount(model);

                    if (result.IsSuccess)
                    {
                        return new DataResponse<AppUser>()
                        {
                            Data = result.Data,
                            Status = true,
                            Message = "Tạo tài khoản thành công"
                        };
                    }
                    else
                    {
                        return DataResponse<AppUser>.False("Thêm mới thất bại", result.Errors);
                    }
                }
                catch (Exception ex)
                {
                    return DataResponse<AppUser>.False("Lỗi hệ thống", new string[] { ex.Message });
                }
            }

            return DataResponse<AppUser>.False("Dữ liệu đầu vào không hợp lệ", ModelStateError);
        }

        [HttpPut("Update")]
        public async Task<DataResponse<AppUser>> Update([FromBody] AspNetUsersRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var entity = await _aspNetUsersService.GetByIdAsync(model.Id);
                    if (entity == null)
                        return DataResponse<AppUser>.False("Người dùng không tồn tại");

                    entity.Name = model.Name;
                    entity.DonViId = model.DonViId;
                    entity.PhoneNumber = model.PhoneNumber;
                    entity.NgaySinh = model.NgaySinh;
                    entity.Gender = int.TryParse(model.Gender, out int dd) ? dd : 1;
                    entity.DiaChi = model.DiaChi;
                    entity.Email = model.Email;

                    await _aspNetUsersService.UpdateAsync(entity);

                    return new DataResponse<AppUser>() { Data = entity, Status = true };
                }
                catch (Exception ex)
                {
                    DataResponse<AppUser>.False(ex.Message);
                }
            }
            return DataResponse<AppUser>.False("Dữ liệu không hợp lệ", ModelStateError);
        }

        [HttpGet("Get/{id}")]
        public async Task<DataResponse<AspNetUsersDto>> Get(Guid id)
        {
            var result = await _aspNetUsersService.GetDto(id);
            return new DataResponse<AspNetUsersDto>
            {
                Data = result,
                Message = "Get AspNetUsersDto thành công",
                Status = true
            };
        }

        [HttpPost("GetData")]
        public async Task<DataResponse<PagedList<AppUserDto>>> GetData([FromBody] AspNetUsersSearch search)
        {
            var userDto = new AppUserDto();

            var result = await _aspNetUsersService.GetData(search, userDto);

            return new DataResponse<PagedList<AppUserDto>>
            {
                Data = result,
                Message = "GetData PagedList<AppUserDto> thành công",
                Status = true
            };
        }

        [HttpPost("GetListUserByRole")]
        public async Task<DataResponse<PagedList<AppUserDto>>> GetListUserByRole([FromBody] AspNetUsersSearch search)
        {
            var userDto = new AppUserDto();
            userDto.Id = UserId ?? new Guid();

            if (HasRole(RoleConstant.Admin))
            {
                userDto = null;
            }

            var result = await _aspNetUsersService.GetData(search, userDto);

            return new DataResponse<PagedList<AppUserDto>>
            {
                Data = result,
                Message = "GetData PagedList<AppUserDto> thành công",
                Status = true
            };
        }

        [HttpDelete("Delete/{id}")]
        public async Task<DataResponse> Delete(Guid id)
        {
            try
            {
                var entity = await _aspNetUsersService.GetByIdAsync(id);
                await _aspNetUsersService.DeleteAsync(entity);
                return DataResponse.Success(null);
            }
            catch (Exception ex)
            {
                return DataResponse.False(ex.Message);
            }
        }

        [HttpDelete("Lock/{id}")]
        public async Task<DataResponse> LockUser(Guid id)
        {
            var obj = await _aspNetUsersService.GetByIdAsync(id);
            if (obj == null)
            {
                return DataResponse.False("Không tìm thấy thông tin tài khoản");
            }
            try
            {
                obj.LockoutEnabled = !obj.LockoutEnabled;
                if (obj.LockoutEnabled)
                {
                    obj.LockoutEnd = DateTime.Now.AddDays(1);
                    obj.AccessFailedCount = 100;
                }
                else
                {
                    obj.LockoutEnd = null;
                    obj.AccessFailedCount = 0;
                }
                await _aspNetUsersService.UpdateAsync(obj);
            }
            catch (Exception ex)
            {
                return DataResponse.False("Không khóa/mở khóa được tài khoản");
            }
            return DataResponse.Success(null);
        }

        [HttpPost("export")]
        public async Task<DataResponse> ExportExcel([FromBody] AspNetUsersSearch search)
        {
            try
            {
                //var search = new AspNetUsersSearch();
                //search.PageIndex = 1;
                //search.PageSize = 20;
                //var data = await _aspNetUsersService.GetData(search);
                //var base64Excel = await ExportExcelHelperNetCore.Export<AppUserDto>(data?.Data?.Items);

                var exportData = await _aspNetUsersService.GetData(search);

                var base64Excel = exportData != null && exportData?.Items != null
                         ? ExportExcelHelperNetCore.ExportExcel(exportData.Items)
                            : null;
                if (string.IsNullOrEmpty(base64Excel))
                {
                    return DataResponse.False("Kết xuất thất bại hoặc dữ liệu trống");
                }
                return DataResponse.Success(base64Excel);
            }
            catch (Exception ex)
            {
                return DataResponse.False("Kết xuất thất bại");
            }
        }






        [HttpGet("GetDropDown")]
        public async Task<DataResponse<List<DropdownOption>>> GetDropdown()
        {
            var thuongHieuDropdown = await _aspNetUsersService.GetQueryable().Select(x => new DropdownOption
            {
                Label = string.IsNullOrEmpty(x.Name) ? x.UserName : x.Name,
                Value = x.Id.ToString().ToLower()
            }).ToListAsync();
            return new DataResponse<List<DropdownOption>>() { Data = thuongHieuDropdown, Status = true };
        }

        [HttpPost("GetUserByRole")]
        public async Task<DataResponse<PagedList<AppUserDto>>> GetUserByRole([FromBody] AspNetUsersSearch search)
        {
            var user = await _aspNetUsersService.GetUserByRole(search);
            return new DataResponse<PagedList<AppUserDto>>() { Data = user, Status = true };
        }

        [HttpGet("GetCanBoByDonVi")]
        public async Task<DataResponse<List<DropdownOption>>> GetCanBoByDonVi([FromQuery] Guid donViId)
        {
            var data = await _aspNetUsersService.GetCanBoByDonViId(donViId);

            if (data.IsSuccess)
            {
                return DataResponse<List<DropdownOption>>.Success(data.Data, data.Message);
            }
            else
            {
                return DataResponse<List<DropdownOption>>.False(data.Message);
            }
        }
    }
}