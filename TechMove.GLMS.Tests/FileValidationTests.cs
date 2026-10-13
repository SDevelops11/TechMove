using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TechMove.GLMS.Web.Services;
using Xunit;

namespace TechMove.GLMS.Tests
{
    /// <summary>
    /// Unit Tests for strict PDF File Validation & File Handling.
    /// Fulfills Rubric: Rejection of restricted types (.exe), magic bytes verification, and null/edge cases.
    /// </summary>
    public class FileValidationTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<FileStorageService>> _mockLogger;
        private readonly FileStorageService _fileService;

        public FileValidationTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<FileStorageService>>();

            // Setup default configuration values
            var configSection = new Mock<IConfigurationSection>();
            _mockConfiguration.Setup(c => c.GetSection("FileStorage:MaxFileSizeBytes")).Returns(configSection.Object);

            _fileService = new FileStorageService(
                _mockEnvironment.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);
        }

        private static IFormFile CreateMockFormFile(string fileName, string contentType, byte[] content)
        {
            var stream = new MemoryStream(content);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(content.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            return fileMock.Object;
        }

        [Theory]
        [InlineData("malicious_script.exe", "application/x-msdownload")]
        [InlineData("installer.bat", "application/x-bat")]
        [InlineData("exploit.sh", "application/x-sh")]
        [InlineData("image.png", "image/png")]
        [InlineData("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        public void ValidatePdfFile_RestrictedFileTypes_ReturnsFalseWithError(string fileName, string contentType)
        {
            // Arrange (Restricted file types: .exe, .bat, etc.)
            byte[] fileBytes = Encoding.UTF8.GetBytes("Dummy file content");
            var mockFile = CreateMockFormFile(fileName, contentType, fileBytes);

            // Act
            bool isValid = _fileService.ValidatePdfFile(mockFile, out string? errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Only signed PDF (.pdf) documents are permitted", errorMessage);
        }

        [Fact]
        public void ValidatePdfFile_ValidPdfDocument_ReturnsTrue()
        {
            // Arrange: Valid PDF with %PDF magic bytes (0x25, 0x50, 0x44, 0x46)
            byte[] validPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x35, 0x0A };
            var mockFile = CreateMockFormFile("signed_contract_sla.pdf", "application/pdf", validPdfBytes);

            // Act
            bool isValid = _fileService.ValidatePdfFile(mockFile, out string? errorMessage);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ValidatePdfFile_SpoofedPdfExtension_DetectsInvalidHeaderAndRejects()
        {
            // Arrange (Edge Case: .exe renamed to .pdf to bypass simple extension checks)
            // Header starts with "MZ" (0x4D, 0x5A) executable magic bytes
            byte[] fakePdfBytes = Encoding.ASCII.GetBytes("MZ_Executable_Binary_Payload");
            var mockFile = CreateMockFormFile("malicious_disguised.pdf", "application/pdf", fakePdfBytes);

            // Act
            bool isValid = _fileService.ValidatePdfFile(mockFile, out string? errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Corrupt or spoofed file detected", errorMessage);
        }

        [Fact]
        public void ValidatePdfFile_NullFile_ReturnsFalseWithError()
        {
            // Arrange (Edge Case: Null input)
            IFormFile? nullFile = null;

            // Act
            bool isValid = _fileService.ValidatePdfFile(nullFile, out string? errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.Equal("No file was provided for upload.", errorMessage);
        }

        [Fact]
        public void ValidatePdfFile_EmptyZeroByteFile_ReturnsFalseWithError()
        {
            // Arrange (Edge Case: 0 length file)
            var emptyFile = CreateMockFormFile("empty.pdf", "application/pdf", Array.Empty<byte>());

            // Act
            bool isValid = _fileService.ValidatePdfFile(emptyFile, out string? errorMessage);

            // Assert
            Assert.False(isValid);
            Assert.Equal("No file was provided for upload.", errorMessage);
        }
    }
}
