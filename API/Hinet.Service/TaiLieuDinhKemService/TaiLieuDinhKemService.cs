using CommonHelper.String;
using DocumentFormat.OpenXml.Spreadsheet;
using Hinet.Model;
using Hinet.Model.Entities;
using Hinet.Repository.TaiLieuDinhKemRepository;
using Hinet.Service.Common;
using Hinet.Service.Common.Service;
using Hinet.Service.Constant;
using Hinet.Service.TaiLieuDinhKemService.Dto;
using Hinet.Service.TaiLieuDinhKemService.Request;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Hinet.Service.TaiLieuDinhKemService
{
    public class TaiLieuDinhKemService : Service<TaiLieuDinhKem>, ITaiLieuDinhKemService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly string _webRootPath = "wwwroot";
        private readonly ITaiLieuDinhKemRepository _taiLieu;


        private const long MaxBytes = 5 * 1024 * 1024; // 5MB

        private static readonly HashSet<string> AllowedExts = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private static readonly HashSet<string> AllowedMime = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };

        private static readonly Dictionary<string, List<byte[]>> AllowedFileSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        // Hình ảnh
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }, // Bắt đầu bằng RIFF

        // Tài liệu
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }, // %PDF
		{ ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // ZIP format (Dùng chung cho xlsx, pptx)

        // Video (Mới thêm)
        { ".mp4", new List<byte[]> {
            new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 }, // ftyp
            new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 },
            new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }
        }}
    };

        public TaiLieuDinhKemService(ITaiLieuDinhKemRepository taiLieuDinhKem,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env
            ) : base(taiLieuDinhKem)
        {
            this._httpClientFactory = httpClientFactory;
            _env = env;
            _taiLieu = taiLieuDinhKem;
        }

        public async Task<List<TaiLieuDinhKem>> GetByItemAsync(Guid itemId)
        {
            return await Where(x => x.ItemId == itemId).ToListAsync();
        }

        public Task<List<TaiLieuDinhKem>> GetByIdsAsync(List<Guid> ids)
        {
            return Where(x => ids.Contains(x.Id)).ToListAsync();
        }


        public async Task<TaiLieuDinhKem> UpdateItemIdAsync(Guid itemId, Guid id)
        {
            var item = await GetByIdAsync(id);
            if (item != null)
            {
                item.ItemId = itemId;
                await UpdateAsync(item);
            }
            return item;
        }

        public async Task<List<TaiLieuDinhKem>> UpdateItemIdAsync(Guid itemId, List<Guid> ids)
        {
            var items = await GetByIdsAsync(ids);
            foreach (var item in items)
            {
                item.ItemId = itemId;
            }
            await UpdateAsync(items);
            return items;
        }

        public async Task<PagedList<TaiLieuDinhKemDto>> GetData(TaiLieuDinhKemSearch search)
        {
            try
            {
                var query = from q in GetQueryable()
                            select new TaiLieuDinhKemDto
                            {
                                ItemId = q.ItemId,
                                UserId = q.UserId,
                                KichThuoc = q.KichThuoc,
                                TenTaiLieu = q.TenTaiLieu,
                                LoaiTaiLieu = q.LoaiTaiLieu,
                                DuongDanFile = q.DuongDanFile,
                                DuongDanFilePDF = q.DuongDanFilePDF,
                                Extension = q.Extension,
                                CreatedDate = q.CreatedDate,
                                UpdatedDate = q.UpdatedDate,
                                Id = q.Id,
                                isXoaFile = q.IsDeleted,

                            };

                if (search != null)
                {
                    if (!string.IsNullOrEmpty(search.ItemId))
                    {
                        query = query.Where(x => x.ItemId == search.ItemId.ToGuid());
                    }
                    if (!string.IsNullOrEmpty(search.TenTaiLieu))
                    {
                        query = query.Where(x => x.TenTaiLieu.ToUpper().Contains(search.TenTaiLieu.Trim().ToUpper()));
                    }

                    if (!string.IsNullOrEmpty(search.DinhDangFile))
                    {
                        query = query.Where(x => x.Extension.ToUpper().Equals(search.DinhDangFile.Trim().ToUpper()));
                    }

                    if (search.KichThuocMin != null)
                    {
                        query = query.Where(x => x.KichThuoc != null && x.KichThuoc >= search.KichThuocMin);
                    }

                    if (search.KichThuocMax != null)
                    {
                        query = query.Where(x => x.KichThuoc != null && x.KichThuoc <= search.KichThuocMax);
                    }

                    if (!string.IsNullOrEmpty(search.LoaiTaiLieu))
                    {
                        query = query.Where(x => x.LoaiTaiLieu != null && x.LoaiTaiLieu.ToUpper().Equals(search.LoaiTaiLieu.Trim().ToUpper()));
                    }


                    if (search.IsDonVi.HasValue && search.IsDonVi.Value)
                    {
                        query = query.Where(x => x.LoaiTaiLieu != null &&
                            x.LoaiTaiLieu.ToLower().Equals(LoaiTaiLieuConstant.TaiLieuDonVi) &&
                            x.ItemId != null &&
                            x.ItemId.ToString().ToUpper().Equals(search.ItemId.Trim().ToUpper()));
                    }
                }

                query = query.OrderByDescending(x => x.CreatedDate);
                return await PagedList<TaiLieuDinhKemDto>.CreateAsync(query, search);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to retrieve data: " + ex.Message, ex);
            }

        }



        public async Task<string> GetPathFromId(Guid id)
        {
            var entity = await GetQueryable().FirstOrDefaultAsync(x => x.Id == id);
            return entity?.DuongDanFile ?? throw new Exception("Attachment not found");
        }


        public async Task<Stream?> GetStreamAsync(Guid? fileId)
        {
            var client = _httpClientFactory.CreateClient("FileServerClient");

            var response = await client.GetAsync($"/fileserver/{fileId}", HttpCompletionOption.ResponseHeadersRead);

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<Guid> UploadAsync(Stream fileStream, string? fileName, string? fileType, Guid? itemId)
        {
            var client = _httpClientFactory.CreateClient("FileServerClient");

            using var form = new MultipartFormDataContent();

            fileStream.Position = 0; // Đảm bảo con trỏ ở đầu stream
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream"); // Hoặc loại file cụ thể nếu biết

            form.Add(fileContent, "Files", fileName);

            if (!string.IsNullOrEmpty(fileType))
            {
                form.Add(new StringContent(fileType), "FileType");
            }

            if (itemId.HasValue)
            {
                form.Add(new StringContent(itemId.Value.ToString()), "ItemId");
            }

            var response = await client.PostAsync("/api/fileserver/upload", form);

            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<DataResponseUpload>(content);
                if (data != null && data.Status && data.Data.Count > 0)
                {
                    return data.Data[0].Id;
                }
                else
                {
                    throw new Exception($"Upload thất bại: {data?.Message}");
                }
            }
            catch (Exception ex)
            {

                throw new Exception($"Upload thất bại: {ex.Message}");
            }
        }

        private class DataResponseUpload
        {
            public string? Message { get; set; }
            public List<TaiLieuDinhKem> Data { get; set; } = new List<TaiLieuDinhKem>();
            public bool Status { get; set; }

        }
        public async Task DeleteAsync(List<Guid> guids, Guid? userId)
        {
            var entities = await GetQueryable()
                .Where(t => guids.Contains(t.Id) && !t.IsDeleted)
                .ToListAsync();
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.DeletedId = userId;
                entity.DeletedDate = DateTime.Now;
            }
            await UpdateAsync(entities);
        }
        public async Task<List<TaiLieuDinhKem>> GetByItemId(Guid itemId, string? type)
        {
            var query = GetQueryable().Where(t => t.ItemId == itemId && !t.IsDeleted);
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(e => e.LoaiTaiLieu == type);
            }
            return await query.ToListAsync();
        }
        public async Task<TaiLieuDinhKem?> GetById(Guid id)
        {
            return await GetQueryable()
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<Guid> UploadImage(Stream stream, string? fileName, string? fileType, Guid? itemId, Guid userId)
        {
            if (stream == null)
                throw new ArgumentException("File rỗng");

            if (stream.CanSeek)
            {
                if (stream.Length == 0)
                    throw new ArgumentException("File rỗng");

                if (stream.Length > MaxBytes)
                    throw new ArgumentException($"Dung lượng ảnh vượt quá giới hạn {MaxBytes / (1024 * 1024)}MB");
            }

            var ext = Path.GetExtension(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExts.Contains(ext))
                throw new ArgumentException("Định dạng ảnh không hỗ trợ (chỉ jpg, jpeg, png, webp, gif)");

            if (string.IsNullOrWhiteSpace(fileType) || !AllowedMime.Contains(fileType))
                throw new ArgumentException("Content-Type không hợp lệ");

            var dateFolder = DateTime.Now.ToString("yyyyMMdd");
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "images", dateFolder);
            Directory.CreateDirectory(uploadsRoot);

            var idFile = Guid.NewGuid();
            var safeOriginalName = Path.GetFileName(fileName ?? "image");
            var savedFileName = $"{idFile}_{safeOriginalName}";
            var fullPath = Path.Combine(uploadsRoot, savedFileName);

            if (stream.CanSeek) stream.Position = 0;

            long totalWritten = 0;
            await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalWritten += read;
                    if (totalWritten > MaxBytes)
                    {
                        fs.Close();
                        if (File.Exists(fullPath)) File.Delete(fullPath);
                        throw new ArgumentException($"Dung lượng ảnh vượt quá giới hạn {MaxBytes / (1024 * 1024)}MB");
                    }

                    await fs.WriteAsync(buffer, 0, read);
                }
            }

            if (totalWritten == 0)
            {
                if (File.Exists(fullPath)) File.Delete(fullPath);
                throw new ArgumentException("File rỗng");
            }

            var relativePath = $"/uploads/images/{dateFolder}/{savedFileName}".Replace("\\", "/");

            var entity = new TaiLieuDinhKem
            {
                Id = idFile,
                TenTaiLieu = safeOriginalName,
                Extension = ext,
                LoaiTaiLieu = fileType ?? "",
                DuongDanFile = relativePath,
                KichThuoc = totalWritten,
                ItemId = itemId,
                UserId = userId,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                CreatedId = userId,
                UpdatedId = userId,
            };

            await CreateAsync(entity);
            return entity.Id;
        }

        public async Task<string> UploadDocumentAsync(Stream stream, string fileName, string folderType = "files")
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("File tải lên bị rỗng.");

            if (stream.Length > MaxBytes)
                throw new ArgumentException($"Dung lượng file vượt quá giới hạn {MaxBytes / (1024 * 1024)}MB.");

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedFileSignatures.ContainsKey(ext))
                throw new ArgumentException($"Định dạng file '{ext}' không được hỗ trợ.");

            if (!await IsValidFileSignatureAsync(stream, ext))
                throw new ArgumentException("Nội dung file không hợp lệ. Phát hiện nghi ngờ giả mạo định dạng file!");

            var dateFolder = DateTime.Now.ToString("yyyyMMdd");
            var uploadsRoot = Path.Combine(_webRootPath, "uploads", folderType, dateFolder);

            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            var idFile = Guid.NewGuid();
            var safeOriginalName = Path.GetFileName(fileName);
            var savedFileName = $"{idFile}_{safeOriginalName}";
            var fullPath = Path.Combine(uploadsRoot, savedFileName);

            if (stream.CanSeek) stream.Position = 0;

            using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs);
            }

            return $"/uploads/{folderType}/{dateFolder}/{savedFileName}".Replace("\\", "/");
        }

        private async Task<bool> IsValidFileSignatureAsync(Stream stream, string ext)
        {
            if (!AllowedFileSignatures.TryGetValue(ext, out var signatures))
                return false;

            var maxSignatureLength = signatures.Max(s => s.Length);
            var headerBytes = new byte[maxSignatureLength];

            if (stream.CanSeek) stream.Position = 0;

            var bytesRead = await stream.ReadAsync(headerBytes, 0, headerBytes.Length);
            if (bytesRead < maxSignatureLength)
                return false;

            if (stream.CanSeek) stream.Position = 0;

            foreach (var signature in signatures)
            {
                if (headerBytes.Take(signature.Length).SequenceEqual(signature))
                    return true;
            }

            return false;
        }

        public async Task<List<DanhSachTaiLieuDto>> GetDanhSachTaiLieu(string? keyword, string? loaiTaiLieu)
        {
            var query = GetQueryable();

            if (!string.IsNullOrEmpty(loaiTaiLieu))
            {
                // Tách chuỗi từ Frontend thành danh sách
                var rawExtensions = loaiTaiLieu.ToLower()
                                               .Split(',')
                                               .Select(e => e.Trim().Replace(".", ""))
                                               .ToList();

                var validExtensions = new List<string>();
                foreach (var ext in rawExtensions)
                {
                    validExtensions.Add(ext);       //  jpg
                    validExtensions.Add("." + ext); // .jpg
                }

                query = query.Where(x => validExtensions.Contains(x.Extension.ToLower()));
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TenTaiLieu.Contains(keyword) || x.LoaiTaiLieu.Contains(keyword));
            }
            var result = query.Select(x => new DanhSachTaiLieuDto
            {
                Id = x.Id,
                TenTaiLieu = x.TenTaiLieu,
                LoaiTaiLieu = x.Extension,
                DuongDanFile = x.DuongDanFile
            });
            return await result.ToListAsync();
        }

        public async Task<TaiLieuDinhKem> UploadAndSaveDb(
            Stream stream,
            string fileName,
            string tenTaiLieuText,
            string folderType = "files",
            Guid? itemId = null,
            Guid? userId = null)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("File tải lên bị rỗng.");

            if (stream.Length > MaxBytes)
                throw new ArgumentException($"Dung lượng file vượt quá giới hạn {MaxBytes / (1024 * 1024)}MB.");

            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedFileSignatures.ContainsKey(ext))
                throw new ArgumentException($"Định dạng file '{ext}' không được hỗ trợ.");

            if (!await IsValidFileSignatureAsync(stream, ext))
                throw new ArgumentException("Nội dung file không hợp lệ.");

            var dateFolder = DateTime.Now.ToString("yyyyMMdd");
            var uploadsRoot = Path.Combine(_webRootPath, "uploads", folderType, dateFolder);

            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            var idFile = Guid.NewGuid();
            var safeOriginalName = Path.GetFileName(fileName);
            var savedFileName = $"{idFile}_{safeOriginalName}";
            var fullPath = Path.Combine(uploadsRoot, savedFileName);

            if (stream.CanSeek) stream.Position = 0;
            using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs);
            }

            var relativePath = $"/uploads/{folderType}/{dateFolder}/{savedFileName}".Replace("\\", "/");

            var tailieu = new TaiLieuDinhKem
            {
                Id = idFile,
                TenTaiLieu = safeOriginalName,
                TenTaiLieuText = tenTaiLieuText,
                Extension = ext.Replace(".", "").ToLower(),
                DuongDanFile = relativePath,
                LoaiTaiLieu = folderType,
                KichThuoc = stream.Length / 1024,
                ItemId = itemId,
                UserId = userId,
                CreatedDate = DateTime.Now,
                CreatedId = userId,
                UpdatedDate = DateTime.Now,
                UpdatedId = userId
            };

            await CreateAsync(tailieu);

            return tailieu;
        }

        public async Task<(bool IsSuccess, string Message, FileInfoResponseDto? Data)> GetFileInfo(Guid id)
        {
            try
            {
                var file = await GetByIdAsync(id);
                if (file == null || file.IsDeleted)
                {
                    return (false, "Không tìm thấy tài liệu hoặc tài liệu đã bị xóa.", null);
                }

                var dto = new FileInfoResponseDto
                {
                    Id = file.Id,
                    TenTaiLieuGoc = file.TenTaiLieu,
                    TenTaiLieuText = file.TenTaiLieuText,
                    KichThuoc = file.KichThuoc,
                    Extension = file.Extension,
                };

                return (true, "Lấy thông tin thành công.", dto);
            }
            catch (Exception ex)
            {
                return (false, "Lỗi hệ thống khi lấy thông tin file: " + ex.Message, null);
            }
        }
    }
}