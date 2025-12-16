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
            var data = $"{registrationId}:{eventId}:{DateTime.Now:yyyyMMddHHmmss}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return $"{registrationId}:{eventId}:{Convert.ToBase64String(hash)}";
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
                if (parts.Length != 3) return (false, 0, 0);

                if (!int.TryParse(parts[0], out int registrationId) ||
                    !int.TryParse(parts[1], out int eventId))
                {
                    return (false, 0, 0);
                }

                // Trong thực tế, bạn nên thêm kiểm tra thời gian hiệu lực của token
                return (true, registrationId, eventId);
            }
            catch
            {
                return (false, 0, 0);
            }
        }
    }
}