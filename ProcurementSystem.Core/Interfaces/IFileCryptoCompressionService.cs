namespace ProcurementSystem.Core.Interfaces
{
    public interface IFileCryptoCompressionService
    {
        /// <summary>
        /// Nén luồng dữ liệu (GZip) và mã hóa đối xứng (AES-256-CBC) với IV 16 bytes ngẫu nhiên gắn đầu tệp
        /// </summary>
        Task<Stream> ProcessForStorageAsync(Stream inputStream);

        /// <summary>
        /// Giải mã đối xứng (AES-256-CBC) và giải nén (GZip) để khôi phục lại tệp gốc
        /// </summary>
        Task<Stream> RestoreFromStorageAsync(Stream encryptedStream);

        /// <summary>
        /// Kiểm tra Magic Bytes phần header nhị phân để đảm bảo tệp không bị giả mạo phần mở rộng
        /// </summary>
        bool ValidateMagicBytes(Stream stream, string extension);

        /// <summary>
        /// Tính toán chuỗi băm SHA-256 để lưu trữ và đối soát tính toàn vẹn của tệp
        /// </summary>
        string ComputeSha256(Stream stream);
    }
}
