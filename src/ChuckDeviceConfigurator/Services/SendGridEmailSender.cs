namespace ChuckDeviceConfigurator.Services
{
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Options;

    using SendGrid;
    using SendGrid.Helpers.Mail;

    public class AuthMessageSenderOptions
    {
        public string? SendGridKey { get; set; }
    }

    public class SendGridEmailSender : IEmailSender
	{
        private readonly ILogger<SendGridEmailSender> _logger;

        public AuthMessageSenderOptions Options { get; }

        public SendGridEmailSender(
            ILogger<SendGridEmailSender> logger,
            IOptions<AuthMessageSenderOptions> optionsAccessor)
        {
            Options = optionsAccessor.Value;
            _logger = logger;
        }


        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(Options.SendGridKey))
            {
                throw new Exception("SendGridKey is null");
            }
            await Execute(Options.SendGridKey, subject, message, toEmail);
        }

        public async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                //From = new EmailAddress("Joe@contoso.com", "Password Recovery"),
                From = new EmailAddress("versx@ver.sx", "ChuckDeviceConfigurator"),
                ReplyTo = new EmailAddress("versx@ver.sx", "ChuckDeviceConfigurator"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            var response = await client.SendEmailAsync(msg);
            _logger.LogInformation(response.IsSuccessStatusCode
                                   ? $"Email to {toEmail} queued successfully!"
                                   : $"Failure Email to {toEmail}");
        }
    }
}