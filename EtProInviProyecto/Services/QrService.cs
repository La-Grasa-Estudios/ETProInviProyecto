using QRCoder;

namespace ETPro.Services
{
    public class QrService
    {
        public string GenerateBase64Qr(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = pngQrCode.GetGraphic(5);
            return Convert.ToBase64String(qrBytes);
        }
    }
}