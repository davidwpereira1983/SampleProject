using System;

namespace Company.TestProject.WebApiClient.DTO
{
    public record ExceptionDetail
    {
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
    }
}
