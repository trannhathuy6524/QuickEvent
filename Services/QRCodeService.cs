using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Security.Cryptography;

namespace QuickEvent.Services
{
    public class QRCodeService
    {
        private readonly string _secretKey = "TranVietHung@#!123K22Hutech"; // Thay thế bằng key phức tạp hơn trong production

        public string GenerateQRCodeToken(int registrationId, int eventId)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var data = $"{registrationId}:{eventId}:{timestamp}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return $"{registrationId}:{eventId}:{timestamp}:{Convert.ToBase64String(hash)}";
        }

        public byte[] GenerateQRCodeImage(string token)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        public (bool isValid, int registrationId, int eventId) ValidateQRCode(string token)
        {
            try
            {
                var parts = token.Split(':');
                if (parts.Length != 4) return (false, 0, 0);

                if (!int.TryParse(parts[0], out int registrationId) ||
                    !int.TryParse(parts[1], out int eventId))
                {
                    return (false, 0, 0);
                }

                // Lấy timestamp và signature từ token
                var timestamp = parts[2];
                var receivedSignature = parts[3];

                // Tính lại signature với cùng dữ liệu
                var data = $"{registrationId}:{eventId}:{timestamp}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var calculatedSignature = Convert.ToBase64String(hash);

                // So sánh signature (chống giả mạo)
                if (receivedSignature != calculatedSignature)
                {
                    return (false, 0, 0); // Token bị giả mạo!
                }

                return (true, registrationId, eventId);
            }
            catch
            {
                return (false, 0, 0);
            }
        }
    }
}