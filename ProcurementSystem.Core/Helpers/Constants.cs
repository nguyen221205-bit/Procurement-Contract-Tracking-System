namespace ProcurementSystem.Core.Helpers
{
    public static class Constants
    {
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Procurement = "Procurement";
            public const string Evaluator = "Evaluator";
            public const string Contractor = "Contractor";
        }

        public static class Pagination
        {
            public const int DefaultPageSize = 10;
            public const int MaxPageSize = 50;
        }

        public static class FileUpload
        {
            public const long MaxFileSize = 10 * 1024 * 1024; // 10MB
            public static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".png" };
            public const string UploadPath = "uploads";
        }
    }
}
