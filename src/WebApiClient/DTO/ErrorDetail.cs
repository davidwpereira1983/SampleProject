namespace Company.TestProject.WebApiClient.DTO
{
    public record ErrorDetail
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }
        public string Severity { get; set; }
    }
}
