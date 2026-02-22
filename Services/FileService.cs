using Microsoft.AspNetCore.StaticFiles;

namespace LostAndFoundApp.Services
{
    /// <summary>
    /// Handles secure file upload with server-side validation for type, size, and storage
    /// outside of web root. Files are never served directly — only through authenticated controller actions.
    /// </summary>
    public class FileService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FileService> _logger;

        public FileService(IConfiguration config, ILogger<FileService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Saves an uploaded photo to the configured secure storage path.
        /// Returns the stored file name on success, or null on failure.
        /// </summary>
        public async Task<string?> SavePhotoAsync(IFormFile file)
        {
            var allowedExtensions = _config.GetSection("FileUpload:AllowedPhotoExtensions").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var storagePath = _config["FileUpload:PhotoStoragePath"] ?? "./SecureStorage/Photos";
            return await SaveFileAsync(file, storagePath, allowedExtensions);
        }

        /// <summary>
        /// Saves an uploaded attachment to the configured secure storage path.
        /// Returns the stored file name on success, or null on failure.
        /// </summary>
        public async Task<string?> SaveAttachmentAsync(IFormFile file)
        {
            var allowedExtensions = _config.GetSection("FileUpload:AllowedAttachmentExtensions").Get<string[]>()
                ?? new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".jpeg", ".png" };
            var storagePath = _config["FileUpload:AttachmentStoragePath"] ?? "./SecureStorage/Attachments";
            return await SaveFileAsync(file, storagePath, allowedExtensions);
        }

        private async Task<string?> SaveFileAsync(IFormFile file, string storagePath, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file size
            var maxSize = _config.GetValue<long>("FileUpload:MaxFileSizeBytes", 10485760); // 10MB default
            if (file.Length > maxSize)
            {
                _logger.LogWarning("File upload rejected: size {Size} exceeds max {Max}", file.Length, maxSize);
                return null;
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("File upload rejected: extension '{Ext}' is not allowed", extension);
                return null;
            }

            // Ensure storage directory exists
            Directory.CreateDirectory(storagePath);

            // Generate unique file name to prevent overwriting and path traversal
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(storagePath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File saved successfully: {FileName}", uniqueFileName);
            return uniqueFileName;
        }

        /// <summary>
        /// Retrieves a file stream for authenticated download. Returns null if file not found.
        /// </summary>
        public (FileStream? Stream, string ContentType)? GetPhoto(string fileName)
        {
            var storagePath = _config["FileUpload:PhotoStoragePath"] ?? "./SecureStorage/Photos";
            return GetFile(fileName, storagePath);
        }

        /// <summary>
        /// Retrieves a file stream for authenticated download. Returns null if file not found.
        /// </summary>
        public (FileStream? Stream, string ContentType)? GetAttachment(string fileName)
        {
            var storagePath = _config["FileUpload:AttachmentStoragePath"] ?? "./SecureStorage/Attachments";
            return GetFile(fileName, storagePath);
        }

        private (FileStream? Stream, string ContentType)? GetFile(string fileName, string storagePath)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            // Prevent path traversal attacks by ensuring only the file name is used
            var sanitizedFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(storagePath, sanitizedFileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {Path}", filePath);
                return null;
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(sanitizedFileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (stream, contentType);
        }

        /// <summary>
        /// Deletes a file from secure storage. Used when replacing uploads.
        /// </summary>
        public void DeletePhoto(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            var storagePath = _config["FileUpload:PhotoStoragePath"] ?? "./SecureStorage/Photos";
            DeleteFile(fileName, storagePath);
        }

        public void DeleteAttachment(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            var storagePath = _config["FileUpload:AttachmentStoragePath"] ?? "./SecureStorage/Attachments";
            DeleteFile(fileName, storagePath);
        }

        private void DeleteFile(string fileName, string storagePath)
        {
            var sanitizedFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(storagePath, sanitizedFileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {FileName}", sanitizedFileName);
            }
        }
    }
}
