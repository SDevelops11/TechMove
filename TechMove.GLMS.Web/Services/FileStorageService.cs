using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TechMove.GLMS.Web.Services
{
    /// <summary>
    /// Enterprise file storage service implementing robust file handling:
    /// - Strict validation (PDF only: extension, MIME type, and magic bytes verification).
    /// - UUID-based collision-safe file naming.
    /// - Storage on server disk.
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageService> _logger;

        // Magic bytes for standard PDF: "%PDF" (0x25, 0x50, 0x44, 0x46)
        private static readonly byte[] PdfMagicBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        public FileStorageService(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public bool ValidatePdfFile(IFormFile? file, out string? errorMessage)
        {
            if (file == null || file.Length == 0)
            {
                errorMessage = "No file was provided for upload.";
                return false;
            }

            // 1. Max size check
            long maxBytes = _configuration.GetValue("FileStorage:MaxFileSizeBytes", 10485760L); // 10MB default
            if (file.Length > maxBytes)
            {
                errorMessage = $"File size ({file.Length / 1024 / 1024}MB) exceeds the maximum allowed limit of {maxBytes / 1024 / 1024}MB.";
                return false;
            }

            // 2. Strict Extension check
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                errorMessage = $"Invalid file format '{extension}'. Only signed PDF (.pdf) documents are permitted.";
                return false;
            }

            // 3. MIME type check
            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
                !file.ContentType.Equals("application/x-pdf", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"Invalid content type '{file.ContentType}'. Expected 'application/pdf'.";
                return false;
            }

            // 4. Magic Bytes verification (prevents malicious executable disguises like malware.exe renamed to test.pdf)
            try
            {
                using var stream = file.OpenReadStream();
                var headerBuffer = new byte[4];
                int bytesRead = stream.Read(headerBuffer, 0, 4);

                if (bytesRead < 4 || !headerBuffer.SequenceEqual(PdfMagicBytes))
                {
                    errorMessage = "Corrupt or spoofed file detected. The file content does not match a valid PDF document header.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file header for {FileName}", file.FileName);
                errorMessage = "An error occurred while validating the PDF file header.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public async Task<FileUploadResult> SaveContractAgreementPdfAsync(IFormFile? file)
        {
            if (!ValidatePdfFile(file, out var validationError))
            {
                return new FileUploadResult
                {
                    IsSuccess = false,
                    ErrorMessage = validationError
                };
            }

            try
            {
                string uploadsRelative = _configuration.GetValue<string>("FileStorage:UploadDirectory") ?? "uploads/contracts";
                string webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string targetDir = Path.Combine(webRoot, uploadsRelative);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string safeOriginal = Path.GetFileName(file!.FileName);
                string uniqueId = Guid.NewGuid().ToString("N");
                string storedFileName = $"{uniqueId}_{safeOriginal}";
                string fullPath = Path.Combine(targetDir, storedFileName);
                string relativePath = $"/{uploadsRelative.Replace('\\', '/')}/{storedFileName}";

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Successfully saved signed agreement: {StoredName} ({Size} bytes)", storedFileName, file.Length);

                return new FileUploadResult
                {
                    IsSuccess = true,
                    StoredFileName = storedFileName,
                    OriginalFileName = safeOriginal,
                    RelativePath = relativePath,
                    FullPath = fullPath,
                    FileSizeBytes = file.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write PDF file to server storage.");
                return new FileUploadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Storage error: {ex.Message}"
                };
            }
        }

        public (byte[] fileBytes, string contentType, string downloadName)? GetFileForDownload(string relativePath, string? originalFileName)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;

            string webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string cleanRelative = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(webRoot, cleanRelative);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found on disk at {Path}", fullPath);
                return null;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            string downloadName = !string.IsNullOrWhiteSpace(originalFileName) ? originalFileName : Path.GetFileName(fullPath);

            return (bytes, "application/pdf", downloadName);
        }
    }
}
