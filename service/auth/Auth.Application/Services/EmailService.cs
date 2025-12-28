using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;

namespace Auth.Application.Services;

public class EmailService
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _baseUrl;

    public EmailService(IConfiguration configuration)
    {
        _apiKey = configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid:ApiKey is required");
        _fromEmail = configuration["SendGrid:FromEmail"] ?? throw new ArgumentNullException("SendGrid:FromEmail is required");
        _fromName = configuration["SendGrid:FromName"] ?? "Ecommerce Platform";
        _baseUrl = configuration["SendGrid:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<bool> SendVerificationEmailAsync(string toEmail, string toName, string verificationToken)
    {
        var client = new SendGridClient(_apiKey);
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(toEmail, toName);
        var subject = "Xác thực email của bạn";
        
        var verificationUrl = $"{_baseUrl}/auth/verify-email?token={verificationToken}";
        
        var htmlContent = $@"
            <html>
            <body>
                <h2>Xác thực email của bạn</h2>
                <p>Xin chào {toName},</p>
                <p>Cảm ơn bạn đã đăng ký tài khoản. Vui lòng click vào link bên dưới để xác thực email của bạn:</p>
                <p><a href=""{verificationUrl}"" style=""background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Xác thực email</a></p>
                <p>Hoặc copy link này vào trình duyệt:</p>
                <p>{verificationUrl}</p>
                <p>Link này sẽ hết hạn sau 24 giờ.</p>
                <p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
            </body>
            </html>";

        var plainTextContent = $"Xin chào {toName},\n\nCảm ơn bạn đã đăng ký tài khoản. Vui lòng truy cập link sau để xác thực email:\n{verificationUrl}\n\nLink này sẽ hết hạn sau 24 giờ.";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        
        try
        {
            var response = await client.SendEmailAsync(msg);
            var body = await response.Body.ReadAsStringAsync();
            Console.WriteLine($"Token: {verificationToken}");
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Body: {body}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

