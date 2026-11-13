using System.Net;

namespace TechMove.GLMS.Web.Services.ApiClients
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? ResponseBody { get; }

        public ApiException(HttpStatusCode statusCode, string message, string? responseBody = null)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}

