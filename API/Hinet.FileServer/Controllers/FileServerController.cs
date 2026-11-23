using Hinet.FileServer.Configuration;
using Hinet.FileServer.Dto;
using Hinet.FileServer.Helper.FileHelper;
using Hinet.FileServer.Helper.Mapper;
using Hinet.FileServer.Helper.OfficeHelper;
using Hinet.FileServer.Helper.Pdf;
using Hinet.FileServer.Model.Entities;
using Hinet.FileServer.Request;
using Hinet.FileServer.Services.TaiLieuDinhKemService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using System.Security.Claims;
using System.Text.RegularExpressions;
using UploadContext = Hinet.FileServer.Helper.FileHelper.FilePathBuilder.UploadContext;

namespace Hinet.FileServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileServerController : ControllerBase
    {
        private readonly ILogger<FileServerController> _logger;
        private readonly ITaiLieuDinhKemService _taiLieuDinhKemService;
        private readonly IMapper _mapper;
        private readonly FileServerSettings _fileServerSettings;
        private readonly IConfiguration _configuration;

        public FileServerController(ITaiLieuDinhKemService taiLieuDinhKemService,
            ILogger<FileServerController> logger,
            IMapper mapper,
            IOptions<FileServerSettings> fileServerOptions,
            IConfiguration configuration)
        {
            _logger = logger;
            this._taiLieuDinhKemService = taiLieuDinhKemService;
            _mapper = mapper;
            _fileServerSettings = fileServerOptions.Value;
            _configuration = configuration;
        }

        protected Guid? UserId
        {
            get
            {
                var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(id, out var userId))
                    return userId;
                return null;
            }
        }

        protected Guid? DonViId
        {
            get
            {
                var id = User.FindFirst(ClaimTypes.Locality)?.Value;
                if (Guid.TryParse(id, out var donViId))
                    return donViId;
                return null;
            }
        }

        protected string? UserName => User.FindFirst(ClaimTypes.Name)?.Value;

        private static string UploadsRoot =>
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");


        [HttpPost("Upload")]
        public async Task<DataResponse<List<TaiLieuDinhKemDto>>> Upload([FromForm] UploadFileRequest form)
        {
            if (form.Files == null || form.Files.Count == 0)
                return DataResponse<List<TaiLieuDinhKemDto>>.False("Không có file nào được chọn");

            UploadContext ctx;
            try { ctx = BuildContext(form.Category, form.SubCategory, form.TaxCode, form.ItemId, form.FileType); }
            catch (ArgumentException ex) { return DataResponse<List<TaiLieuDinhKemDto>>.False(ex.Message); }

            var result = new List<TaiLieuDinhKemDto>();
            var entitiesToSave = new List<TaiLieuDinhKem>();

            foreach (var file in form.Files)
            {
                if (file == null || file.Length == 0)
                    return DataResponse<List<TaiLieuDinhKemDto>>.False("Uploaded file is empty");

                await using var stream = file.OpenReadStream();
                var error = await ProcessOneUploadedFileAsync(
                    file.FileName, file.Length, stream, ctx,
                    form.SerialNumber, form.IncludeExportInfo,
                    result, entitiesToSave, HttpContext.RequestAborted);
                if (error != null) return error;
            }

            await _taiLieuDinhKemService.CreateRangeAsync(entitiesToSave);
            result.ForEach(x => x.DuongDanFile = BuildClientFileUrl(x.Id, x.TenTaiLieu, x.DuongDanFile));
            return DataResponse<List<TaiLieuDinhKemDto>>.Success(result);
        }

        // Build UploadContext từ request, có hỗ trợ backward-compat:
        // nếu Category trống/General mà FileType cũ có giá trị → coi FileType là sub của general.
        private static UploadContext BuildContext(
            string category, string? subCategory, string? taxCode, Guid? itemId, string? legacyFileType)
        {
            if (string.IsNullOrWhiteSpace(category)) category = FileCategoryConstant.General;

            // Backward compat: client cũ chỉ có FileType → giữ tương thích.
            if (string.IsNullOrWhiteSpace(subCategory)
                && category == FileCategoryConstant.General
                && !string.IsNullOrWhiteSpace(legacyFileType))
            {
                subCategory = legacyFileType;
            }

            return new UploadContext
            {
                Category = category,
                SubCategory = subCategory,
                TaxCode = taxCode,
                ItemId = itemId,
                UploadedAt = DateTime.Now,
            };
        }





        [HttpPost("UploadBase64")]
        public async Task<DataResponse<List<TaiLieuDinhKemDto>>> UploadBase64([FromBody] UploadFileBase64Request form)
        {
            if (form.Files == null || form.Files.Count == 0)
                return DataResponse<List<TaiLieuDinhKemDto>>.False("Không có file nào được chọn");

            UploadContext ctx;
            try { ctx = BuildContext(form.Category, form.SubCategory, form.TaxCode, form.ItemId, form.FileType); }
            catch (ArgumentException ex) { return DataResponse<List<TaiLieuDinhKemDto>>.False(ex.Message); }

            var result = new List<TaiLieuDinhKemDto>();
            var entitiesToSave = new List<TaiLieuDinhKem>();

            foreach (var item in form.Files)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.FileName))
                    return DataResponse<List<TaiLieuDinhKemDto>>.False("Tên file không hợp lệ");

                byte[] bytes;
                try { bytes = Convert.FromBase64String(StripDataUrlBase64(item.Base64)); }
                catch (FormatException) { return DataResponse<List<TaiLieuDinhKemDto>>.False("Chuỗi Base64 không hợp lệ"); }

                await using var ms = new MemoryStream(bytes, writable: false);
                var error = await ProcessOneUploadedFileAsync(
                    item.FileName, bytes.Length, ms, ctx,
                    form.SerialNumber, form.IncludeExportInfo,
                    result, entitiesToSave, HttpContext.RequestAborted);
                if (error != null) return error;
            }

            await _taiLieuDinhKemService.CreateRangeAsync(entitiesToSave);
            result.ForEach(x => x.DuongDanFile = BuildClientFileUrl(x.Id, x.TenTaiLieu, x.DuongDanFile));
            return DataResponse<List<TaiLieuDinhKemDto>>.Success(result);
        }

        private static string StripDataUrlBase64(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";
            var s = raw.Trim();
            var comma = s.IndexOf(',', StringComparison.Ordinal);
            if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
                return s[(comma + 1)..].Trim();
            return s;
        }

        private async Task<DataResponse<List<TaiLieuDinhKemDto>>?> ProcessOneUploadedFileAsync(
            string fileName,
            long fileLengthBytes,
            Stream content,
            UploadContext ctx,
            string? serialNumber,
            bool includeExportInfo,
            List<TaiLieuDinhKemDto> result,
            List<TaiLieuDinhKem> entitiesToSave,
            CancellationToken cancellationToken = default)
        {
            if (fileLengthBytes == 0)
                return DataResponse<List<TaiLieuDinhKemDto>>.False("Uploaded file is empty");

            // ===== Enforce max file size =====
            var maxMb = _fileServerSettings.MaxFileSizeMB ?? 50;
            if (maxMb > 0 && fileLengthBytes > maxMb * 1024L * 1024L)
            {
                return DataResponse<List<TaiLieuDinhKemDto>>
                    .False($"Kích thước file vượt quá giới hạn {maxMb}MB");
            }

            var extension = Path.GetExtension(fileName).Replace(".", "").ToLower();
            if (_fileServerSettings.AllowedExtensions == null
                || !_fileServerSettings.AllowedExtensions.Contains(extension))
            {
                return DataResponse<List<TaiLieuDinhKemDto>>
                    .False($"Không cho phép upload file .{extension}");
            }

            // ===== Sinh path qua builder + guard chống traversal =====
            string relativeDir;
            try { relativeDir = FilePathBuilder.BuildDirectory(ctx); }
            catch (ArgumentException ex) { return DataResponse<List<TaiLieuDinhKemDto>>.False(ex.Message); }

            string absoluteDir;
            try { absoluteDir = SafePath.CombineSafe(UploadsRoot, relativeDir); }
            catch (PathTraversalException) { return DataResponse<List<TaiLieuDinhKemDto>>.False("Đường dẫn không hợp lệ"); }

            if (!Directory.Exists(absoluteDir))
                Directory.CreateDirectory(absoluteDir);

            var id = Guid.NewGuid();
            var storedFileName = FilePathBuilder.BuildStoredFileName(id, fileName);
            var absoluteFilePath = Path.Combine(absoluteDir, storedFileName);

            using (var stream = new FileStream(absoluteFilePath, FileMode.Create))
            {
                await content.CopyToAsync(stream, cancellationToken);
            }

            // Path lưu DB là relative (so với UploadsRoot), dùng forward slash.
            var dbPath = $"{relativeDir}/{storedFileName}".Replace("\\", "/");

            // ===== Verify chữ ký số PDF (nếu yêu cầu) =====
            var requireKySo = _configuration.GetValue<bool>("FileServer:RequirePdfDigitalSignature");
            if (extension == "pdf" && requireKySo)
            {
                var certFromfile = PdfHelper.GetLastValidSignerCertificate(absoluteFilePath);
                if (certFromfile == null)
                {
                    return DataResponse<List<TaiLieuDinhKemDto>>
                        .False("[100103] Không tìm thấy chứng thư số hợp lệ trong file PDF hoặc chứng thư đã hết hạn.");
                }
                if (!string.Equals(serialNumber, certFromfile.SerialNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return DataResponse<List<TaiLieuDinhKemDto>>
                        .False("[100105] Chứng thư số dùng để ký file không trùng với chứng thư số đang được chọn.");
                }
            }

            string? soToKhai = null;
            DateTime? ngayDangKy = null;
            if (extension == "pdf" && includeExportInfo)
                (soToKhai, ngayDangKy) = PdfHelper.ExtractInfoFromPdf(absoluteFilePath);

            bool? coChuKySo = null;
            if (extension == "docx" || extension == "xlsx")
                coChuKySo = OfficeSignatureHelper.HasDigitalSignature(absoluteFilePath);

            var dto = new TaiLieuDinhKemDto
            {
                Id = id,
                TenTaiLieu = fileName,
                Extension = extension,
                DuongDanFile = BuildClientFileUrl(id, fileName, dbPath),
                LoaiTaiLieu = ctx.SubCategory ?? ctx.Category,
                KichThuoc = fileLengthBytes / 1024,
                ItemId = ctx.ItemId,
                UserId = UserId,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                CreatedId = UserId,
                UpdatedId = UserId,
                CreatedBy = UserName,
                UpdatedBy = UserName,
                SoToKhai = soToKhai,
                NgayDangKy = ngayDangKy,
                CoChuKySo = coChuKySo,
            };

            result.Add(dto);
            var entityToSave = (TaiLieuDinhKem)dto;
            entityToSave.DuongDanFile = dbPath;
            entitiesToSave.Add(entityToSave);

            return null;
        }

        private string BuildClientFileUrl(Guid id, string fileName, string dbPath)
        {
            var clientBaseUrl = _configuration["FileServer:ClientBaseUrl"];
            var baseUrl = string.IsNullOrWhiteSpace(clientBaseUrl)
                ? _configuration["FileServer:BaseUrl"]
                : clientBaseUrl;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return dbPath;
            }

            return $"{baseUrl.TrimEnd('/')}/{id}/{Uri.EscapeDataString(fileName)}";
        }



        [Authorize]
        [HttpDelete("delete")]
        public async Task<DataResponse> Delete([FromBody] DeleteFileRequest model)
        {
            try
            {
                await _taiLieuDinhKemService.DeleteAsync(model.Ids ?? new List<Guid>(), UserId);
                return DataResponse.Success(true);
            }
            catch (Exception ex)
            {
                return DataResponse.False("Xóa file đính kèm thất bại" + ex.Message);
            }
        }



        [Authorize]
        [HttpGet("Get/{id}")]
        public async Task<DataResponse<TaiLieuDinhKem>> Get([FromRoute] Guid id)
        {
            var tailieu = await _taiLieuDinhKemService.GetById(id);
            if (tailieu != null)
            {
                return DataResponse<TaiLieuDinhKem>.Success(tailieu);
            }
            return DataResponse<TaiLieuDinhKem>.False("Không tìm thấy tài liệu đính kèm");

        }

        [HttpGet("Stream/{id}")]
        public async Task<IActionResult> Stream([FromRoute] Guid id)
        {
            var tailieu = await _taiLieuDinhKemService.GetById(id);
            if (tailieu == null) return NotFound();
            string filePath;
            try { filePath = SafePath.CombineSafe(UploadsRoot, tailieu.DuongDanFile); }
            catch (PathTraversalException) { return BadRequest("Invalid file path"); }
            if (!System.IO.File.Exists(filePath)) return NotFound();
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/octet-stream");
        }


        [Authorize]
        [HttpGet("GetByItemId/{itemId}")]
        public async Task<DataResponse<List<TaiLieuDinhKem>>> GetByItemId([FromRoute] Guid itemId)
        {
            var tailieus = await _taiLieuDinhKemService.GetByItemId(itemId);
            if (tailieus != null)
            {
                return DataResponse<List<TaiLieuDinhKem>>.Success(tailieus);
            }
            return DataResponse<List<TaiLieuDinhKem>>.False("Không tìm thấy tài liệu đính kèm");

        }


        [HttpPost("ValidateFile/{id}")]
        public async Task<DataResponse<FileValidationDto>> ValidateFile([FromRoute] Guid id)
        {
            var result = new FileValidationDto();
            var tl = await _taiLieuDinhKemService.GetById(id);
            if (tl != null && SafePath.IsSafe(UploadsRoot, tl.DuongDanFile))
            {
                var filePath = SafePath.CombineSafe(UploadsRoot, tl.DuongDanFile);
                var fileValidation = FileHelper.ValidateFile(filePath);
                result = _mapper.Map<FileValidationResult, FileValidationDto>(fileValidation);
                result.PdfCertificates = _mapper.MapList<PdfCertificateInfo, CertificateInfoDto>(fileValidation.PdfCertificates).ToList();
            }
            return DataResponse<FileValidationDto>.Success(result);
        }


        [HttpPost("GetCertificateList/{id}")]
        public async Task<DataResponse<List<CertificateInfoDto>>> GetCertificateList([FromRoute] Guid id)
        {
            var result = new List<CertificateInfoDto>();
            var tl = await _taiLieuDinhKemService.GetById(id);
            if (tl != null && SafePath.IsSafe(UploadsRoot, tl.DuongDanFile))
            {
                var filePath = SafePath.CombineSafe(UploadsRoot, tl.DuongDanFile);
                var listPdfSignatureInfo = PdfHelper.GetCertificateList(filePath);
                result = _mapper.MapList<PdfCertificateInfo, CertificateInfoDto>(listPdfSignatureInfo).ToList();
            }
            return DataResponse<List<CertificateInfoDto>>.Success(result);
        }
    }
}
