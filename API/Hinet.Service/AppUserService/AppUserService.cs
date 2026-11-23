using Hinet.Core.Email;
using Hinet.Extensions;
using Hinet.Model.Entities;
using Hinet.Repository.AppUserRepository;
using Hinet.Repository.DepartmentRepository;
using Hinet.Repository.DM_DuLieuDanhMucRepository;
using Hinet.Repository.DM_NhomDanhMucRepository;
using Hinet.Repository.CompanyInfoRepository;
using Hinet.Repository.RoleRepository;
using Hinet.Repository.UserRoleRepository;
using Hinet.Service.AppUserService.Dto;
using Hinet.Service.AppUserService.Request;
using Hinet.Service.AspNetUsersService.Dto;
using Hinet.Service.Common.Service;
using Hinet.Service.Common.TokenService;
using Hinet.Service.Constant;
using Hinet.Service.MoitSsoService.Dto;
using Hinet.Service.Dto;
using Hinet.Service.OperationService;
using Hinet.Service.TaiLieuDinhKemService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Drawing.Printing;
//using MongoDB.Driver.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Hinet.Service.AppUserService
{
    public class AppUserService : Service<AppUser>, IAppUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IOperationService _operationService;
        private readonly IDM_DuLieuDanhMucRepository _dM_DuLieuDanhMucRepository;
        private readonly IDM_NhomDanhMucRepository _dM_NhomDanhMucRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ITokenService _tokenService;
        private readonly ITaiLieuDinhKemService _taiLieuDinhKemService;
        private readonly ICompanyInfoRepository _companyInfoRepository;
        public AppUserService(
            UserManager<AppUser> userManager,
            IAppUserRepository appUserRepository,
            SignInManager<AppUser> signInManager,
            IUserRoleRepository userRoleRepository,
            IRoleRepository roleRepository,
            IOperationService operationService,
            IDM_DuLieuDanhMucRepository dM_DuLieuDanhMucRepository,
            IDM_NhomDanhMucRepository dM_NhomDanhMucRepository,
            IDepartmentRepository departmentRepository,
            ITaiLieuDinhKemService taiLieuDinhKemService,
            ICompanyInfoRepository companyInfoRepository,
            ITokenService tokenService = null) : base(appUserRepository)

        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
            _operationService = operationService;
            _dM_DuLieuDanhMucRepository = dM_DuLieuDanhMucRepository;
            _dM_NhomDanhMucRepository = dM_NhomDanhMucRepository;
            _departmentRepository = departmentRepository;
            _tokenService = tokenService;
            _taiLieuDinhKemService = taiLieuDinhKemService;
            _companyInfoRepository = companyInfoRepository;
        }

        public async Task<AppUserDto> ChangePassword(Guid? id, string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
                throw new Exception("The password is empty");

            if (newPassword != confirmPassword)
                throw new Exception("Mật khẩu nhập lại không trùng khớp với mật khẩu mới");

            var user = await _userManager.FindByIdAsync(id.ToString()) ?? throw new Exception("Không tìm thấy tài khoản");
            if (!await _userManager.CheckPasswordAsync(user, oldPassword))
                throw new Exception("Mật khẩu cũ không chính xác");

            var data = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!data.Succeeded)
                throw new Exception("Đổi mật khẩu thất bại: " + string.Join(", ", data.Errors.Select(x => x.Description)));

            return AppUserDto.FromAppUser(user);
        }

        public async Task<LoginResponseDto> LoginUser(string username, string password)
        {

            //var groupSoLanDangNhapToiDa = _dM_NhomDanhMucRepository.FindBy(x => x.GroupCode == MaDanhMucConstant.SOLANDANGNHAPTOIDA).FirstOrDefault();
            if (string.IsNullOrEmpty(password))
                throw new Exception("Tài khoản hoặc mật khẩu không chính xác");

            //if (long.TryParse(password, out var number))
            //{
            //    if (number > int.MaxValue || number < 0) 
            //        throw new Exception("Tài khoản hoặc mật khẩu không chính xác");
            //}

            var user = await _userManager.FindByNameAsync(username) ?? throw new Exception("Tài khoản hoặc mật khẩu không chính xác");

            if (user.IsSSO == true)
            {
                //return GenToken(user);
            }


            //Lấy cấu hình
            //var groupConfig = _dM_NhomDanhMucRepository
            //    .FindBy(x => x.GroupCode == MaDanhMucConstant.SOLANDANGNHAPTOIDA)
            //    .FirstOrDefault() ?? new DM_NhomDanhMuc();

            //var soLanSaiToiDa = _dM_DuLieuDanhMucRepository
            //    .FindBy(x => x.GroupId == groupConfig.Id && x.Code == MaDanhMucConstant.SOLANDANGNHAP)
            //    .Select(x => x.Priority)
            //    .FirstOrDefault() ?? 5;

            //var thoiGianKhoa = _dM_DuLieuDanhMucRepository
            //    .FindBy(x => x.GroupId == groupConfig.Id && x.Code == MaDanhMucConstant.THOIGIANKHOA)
            //    .Select(x => x.Priority)
            //    .FirstOrDefault() ?? 1;

            //Kiểm tra xem tài khoản có bị khóa không
            if (user.LockoutEnabled && user.LockoutEnd > DateTime.Now)
            {
                throw new Exception($"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}: Tài khoản đã bị khóa. Thử lại sau: {user.LockoutEnd.Value:dd/MM/yyyy HH:mm}");
            }
            else if (user.LockoutEnabled)
            {
                user.AccessFailedCount = 0;
                user.LockoutEnabled = false;
                await _userManager.UpdateAsync(user);
            }
            // Kiểm tra mật khẩu

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
            {
                // Tăng counter sai mật khẩu; nếu vượt ngưỡng MaxFailedAccessAttempts thì lockout 15 phút.
                user.AccessFailedCount++;
                const int maxAttempts = 5;
                if (user.AccessFailedCount >= maxAttempts)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTime.Now.AddMinutes(15);
                    user.AccessFailedCount = 0;
                    await _userManager.UpdateAsync(user);
                    throw new Exception(
                        $"Tài khoản đã bị khóa do nhập sai mật khẩu quá {maxAttempts} lần. Thử lại sau: {user.LockoutEnd.Value:HH:mm}");
                }
                await _userManager.UpdateAsync(user);

                throw new Exception("Tài khoản hoặc mật khẩu không chính xác");
            }

            // Đăng nhập thành công – reset counter.
            if (user.AccessFailedCount > 0)
            {
                user.AccessFailedCount = 0;
                await _userManager.UpdateAsync(user);
            }
            return GenToken(user);
        }

        public async Task<LoginResponseDto> RefreshToken(string refreshToken)
        {
            // Note: The cache-related logic is commented out in the original code, so it's omitted here.
            // If you need to reimplement it, you'll need to adjust accordingly.
            throw new NotImplementedException("Refresh token logic with cache is not implemented.");
        }

        public async Task<AppUserDto> CheckLogin(Guid? id)
        {
            var user = await this.GetByIdAsync(id) ?? throw new Exception("Can't find user");
            return AppUserDto.FromAppUser(user);
        }

        public async Task<string> ResetPassword(string email, string baseUri)
        {
            var user = await _userManager.FindByEmailAsync(email)
                ?? throw new Exception("There is no user with this Email address");

            // Sinh password ngẫu nhiên đủ mạnh
            var passwordBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(9);
            var password = Convert.ToBase64String(passwordBytes).Replace("/", "_").Replace("+", "-");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var data = await _userManager.ResetPasswordAsync(user, token, password);

            if (!data.Succeeded)
                throw new Exception("Password reset failed: " + data.Errors.FirstOrDefault()?.Code);

            // TODO: Gửi password qua email/SMS. Hiện tại chưa wire mail flow.
            //EmailProvider.SendMailResetPassword(user.Email, password, baseUri);

            // KHÔNG trả plaintext password ra client/log.
            // Trả về string rỗng để giữ chữ ký method; controller chịu trách nhiệm trả message generic.
            return string.Empty;
        }

        public async Task LogoutUser()
        {
            try
            {
                await _tokenService.DeactivateCurrentToken();
            }
            catch (Exception ex)
            {
                throw new Exception("Sign out failed", ex);
            }
        }

        public async Task<AppUserDto> Update(AppUser user)
        {
            var data = await _userManager.UpdateAsync(user);
            if (!data.Succeeded)
                throw new Exception("Update user failed: " + string.Join(", ", data.Errors.Select(x => x.Description)));

            return AppUserDto.FromAppUser(user);
        }

        private LoginResponseDto GenToken(AppUser? user, string? refreshToken = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(), nameof(Guid)),
                new Claim("rental_session", user.SecurityStamp ?? ""),
                new Claim(ClaimTypes.Locality, user.DonViId.ToString(), nameof(Guid)),
            };

            if (!string.IsNullOrEmpty(user.Name))
                claims.Add(new Claim(ClaimTypes.Name, user.Name));

            //var listRole = (from role in _roleRepository.GetQueryable()
            //                join userRole in _userRoleRepository.GetQueryable().Where(x => x.UserId == user.ItemId)
            //                on role.ItemId equals userRole.RoleId
            //                select role.Code).ToList();

            // lấy list role theo vai trò (nhóm quyền)
            var listRole = _userRoleRepository.GetQueryable().Where(x => x.UserId == user.Id)
                .Join(_roleRepository.GetQueryable(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { role.Code })
                .Select(x => x.Code)
                .Distinct()
                .ToList() ?? new List<string>();

            claims.Add(new Claim(ClaimTypes.Role, string.Join(",", listRole)));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(AppSettings.AuthSetting.Key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = AppSettings.AuthSetting.Issuer,
                Audience = AppSettings.AuthSetting.Audience,
                Expires = DateTime.UtcNow.AddSeconds(AppSettings.AuthSetting.SecondsExpires),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var userTo = AppUserDto.FromAppUser(user);
            userTo.ListRole = listRole;
            userTo.IsSSO = user.IsSSO;



            return new LoginResponseDto
            {
                User = userTo,
                Token = tokenString,
                RefreshToken = refreshToken,
                Expire = token.ValidTo
            };
        }

        private string GenRefreshToken(Guid? userId)
        {
            var bytes = new byte[64];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var refreshToken = Convert.ToBase64String(bytes);
            // Cache logic is commented out in the original code, so it's omitted here.
            return refreshToken;
        }

        public Task<AppUser?> GetByUserName(string UserName)
        {
            return _userManager.FindByNameAsync(UserName);
        }

        public async Task<AppUserDto> GetInfo(Guid? id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString()) ?? throw new Exception("Không tìm thấy thông tin người dùng");
            var userInfor = AppUserDto.FromAppUser(user);

            var userDto = new AppUserDto
            {
                Id = id,
                Name = userInfor.Name,
                Email = userInfor.Email,
                Gender = userInfor.Gender,
                Picture = userInfor.Picture,
                DonViId = userInfor.DonViId,
                Type = userInfor.Type,
                IdJoin = user.Id,
                PhoneNumber = userInfor.PhoneNumber,
                UserName = userInfor.UserName,
                DiaChi = userInfor.DiaChi,
                NgaySinh = userInfor.NgaySinh,
                CCCD = userInfor.CCCD,
                AnhDaiDien = userInfor.AnhDaiDien,
                GioiTinh_txt = userInfor.GioiTinh_txt,
                vaiTro = userInfor.vaiTro,


            };


            // lấy list role theo vai trò (nhóm quyền)
            var listRole = await _userRoleRepository.GetQueryable().Where(x => x.UserId == userDto.IdJoin)
                .Join(_roleRepository.GetQueryable(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { role.Code })
                .Select(x => x.Code)
                .Distinct()
                .ToListAsync() ?? new List<string>();

            userDto.ListRole = listRole ?? new List<string>();
            userDto.MenuData = await _operationService.GetListMenu(user.Id, userDto.ListRole);
            userDto.TenDonVi_txt = _departmentRepository.FindBy(x => x.Id == userDto.DonViId).FirstOrDefault()?.Name;

            return userDto;
        }

        public async Task<AppUserDto> GetDto(Guid? id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString()) ?? throw new Exception("Không tìm thấy thông tin người dùng");

            var userDto = new AppUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Gender = user.Gender,
                Picture = user.Picture,
                CCCD = user.CCCD,
                DiaChi = user.DiaChi,
                NgaySinh = user.NgaySinh,
                Type = user.Type,
                TenDonVi_txt = _departmentRepository.FindBy(x => x.Id == user.DonViId).FirstOrDefault()?.Name
            };
            return userDto;
        }

        public async Task<AppUserDto> RegisterUser(RegisterViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
                throw new Exception("Tên đăng nhập và mật khẩu không được để trống");

            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
                throw new Exception("Tài khoản đã tồn tại");

            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
                throw new Exception("Email đã tồn tại");

            var user = new AppUser
            {
                UserName = model.UserName,
                Email = model.Email,
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
                throw new Exception("Đăng ký thất bại: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));

            return AppUserDto.FromAppUser(user);
        }
        private async Task IncrementFailedLoginAttempts(AppUser user)
        {
            user.AccessFailedCount++;
            await _userManager.UpdateAsync(user);

        }

        private async Task ResetFailedLoginAttempts(AppUser user)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            user.LockoutEnabled = false;
            await _userManager.UpdateAsync(user);
        }

        public async Task<AppUserDto> UpdateProfile(ProfileUserEditRequest dto)
        {
            var user = await GetByIdAsync(dto.Id) ?? throw new Exception("User không tổn tại");

            user.Name = dto.Name;
            user.Gender = dto.Gender;
            user.NgaySinh = dto.NgaySinh;
            user.PhoneNumber = dto.PhoneNumber;
            user.DiaChi = dto.DiaChi;

            await UpdateAsync(user);

            return await GetInfo(user.Id);
        }

        public async Task<LoginResponseDto> LoginBySsoAsync(MoitUserInfo userInfo, MoitBusinessInfo? businessInfo)
        {
            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.LoginName))
                throw new Exception("Thông tin SSO không hợp lệ");

            // Upsert AppUser theo LoginName
            var user = await _userManager.FindByNameAsync(userInfo.LoginName);
            if (user == null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = userInfo.LoginName,
                    Email = userInfo.Email ?? userInfo.LoginName,
                    EmailConfirmed = true,
                    Name = userInfo.DisplayName ?? userInfo.LoginName,
                    PhoneNumber = userInfo.Mobile ?? userInfo.Phone,
                    IsSSO = true,
                    Type = AccountTypeConstant.DoanhNghiep,
                    LockoutEnabled = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    throw new Exception("Tạo tài khoản SSO thất bại: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));

                // Gán role DoanhNghiep
                var dnRole = _roleRepository.GetQueryable().FirstOrDefault(x => x.Code == RoleConstant.DoanhNghiep);
                if (dnRole != null)
                {
                    _userRoleRepository.Add(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = dnRole.Id,
                        DepartmentId = Guid.Empty,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                    });
                    await _userRoleRepository.SaveAsync();
                }
            }
            else
            {
                // Cập nhật thông tin mới nhất từ Cổng
                user.Name = userInfo.DisplayName ?? user.Name;
                user.Email = userInfo.Email ?? user.Email;
                user.PhoneNumber = userInfo.Mobile ?? userInfo.Phone ?? user.PhoneNumber;
                user.IsSSO = true;
                await _userManager.UpdateAsync(user);
            }

            // Upsert CompanyInfo theo TaxCode (chỉ khi có businessInfo và TaxCode hợp lệ - tài khoản DN, không phải chuyên viên)
            if (businessInfo != null
                && !string.IsNullOrWhiteSpace(businessInfo.TaxCode)
                && businessInfo.TaxCode != "000000000000")
            {
                await UpsertCompanyInfoAsync(businessInfo);
            }

            return GenToken(user);
        }

        private async Task UpsertCompanyInfoAsync(MoitBusinessInfo b)
        {
            var company = _companyInfoRepository.GetQueryable()
                .FirstOrDefault(x => x.TaxCode == b.TaxCode);

            var isNew = company == null;
            company ??= new CompanyInfo
            {
                Id = Guid.NewGuid(),
                Status = CompanyInfoStatusConstant.DaDuyet,
                CreatedDate = DateTime.Now,
            };

            company.Name = b.Name;
            company.EnglishName = b.EnglishName;
            company.ShortName = b.ShortName;
            company.TaxCode = b.TaxCode;
            company.Address = b.Address;
            company.CityId = b.CityId?.ToString();
            company.CityName = b.CityName;
            company.Phone = b.Phone;
            company.Fax = b.Fax;
            company.Email = b.Email;
            company.RepresenterName = b.RepresenterName;
            company.RepresenterMobile = b.RepresenterMobile;
            company.RepresenterPhone = b.RepresenterMobile;
            company.RepresenterEmail = b.RepresenterEmail;
            company.RepresenterCCCD = b.NumberId;
            company.Detail = b.Detail;
            company.ApproveDateOnline = b.ApprovedDate;
            company.QuocGiaId = "VN";
            company.DKKD = b.RegisterFileLink;
            company.UpdatedDate = DateTime.Now;

            // TypeOrganization theo TypeId từ cổng DVC
            company.TypeOrganization = MapTypeOrganization(b.TypeId);
            company.IsNuocNgoai = b.TypeId == 11;
            company.IsVonDauTuNuocNgoai = b.TypeId == 11 || b.TypeId == 7;

            if (isNew)
                _companyInfoRepository.Add(company);
            else
                _companyInfoRepository.Update(company);

            await _companyInfoRepository.SaveAsync();
        }

        private static string MapTypeOrganization(int? typeId)
        {
            if (typeId == 9) return TypeOrganizationConstant.Personal;
            if (typeId == 10 || typeId == 11) return TypeOrganizationConstant.Organization;
            return TypeOrganizationConstant.Company;
        }

        public async Task<AppUserDto> UpdateAvatarUser(Stream stream, string? fileName, string? fileType, Guid userId, Guid? itemId = null)
        {
            var user = await GetByIdAsync(userId) ?? throw new Exception("User không tổn tại");

            var imageId = await _taiLieuDinhKemService.UploadImage(stream, fileName, fileType, itemId, userId);
            var path = await _taiLieuDinhKemService.GetPathFromId(imageId);

            user.Picture = path;
            await UpdateAsync(user);

            return await GetInfo(user.Id);
        }

    }
}
