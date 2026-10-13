using Microsoft.AspNetCore.Http;

namespace TechMove.GLMS.Web.Services
{
    public class FileUploadResult
    {
        public bool IsSuccess { get; set; }
        public string? StoredFileName { get; set; }
        public string? OriginalFileName { get; set; }
        public string? RelativePath { get; set; }
        public string? FullPath { get; set; }
        public long FileSizeBytes { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IFileStorageService
    {
        Task<FileUploadResult> SaveContractAgreementPdfAsync(IFormFile? file);
        bool ValidatePdfFile(IFormFile? file, out string? errorMessage);
        (byte[] fileBytes, string contentType, string downloadName)? GetFileForDownload(string relativePath, string? originalFileName);
    }
}
