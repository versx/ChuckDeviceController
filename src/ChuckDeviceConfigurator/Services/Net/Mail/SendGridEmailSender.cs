namespace ChuckDeviceConfigurator.Services.Net.Mail
{
    using System.Threading.Tasks;
    using ChuckDeviceConfigurator;

    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Options;

    using SendGrid;
    using SendGrid.Helpers.Mail;

    public class SendGridEmailSender : IEmailSender
    {
        private const string FromEmailAddress = "versx@ver.sx";
        private const string DefaultApplicationName = "ChuckDeviceConfigurator";

        private readonly ILogger<IEmailSender> _logger;

        public AuthMessageSenderOptions Options { get; }

        public SendGridEmailSender(
            ILogger<IEmailSender> logger,
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
            await Execute(Options.SendGridKey, toEmail, subject, message);
        }

        public async Task Execute(string apiKey, string toEmail, string subject, string message)
        {
            var client = new SendGridClient(apiKey);
            var fromName = Strings.AssemblyName ?? DefaultApplicationName;
            var fromEmail = new EmailAddress(FromEmailAddress, fromName);

            var msg = new SendGridMessage
            {
                From = fromEmail,
                ReplyTo = fromEmail,
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            //var json = msg.Serialize();
            //msg.SetFooterSetting(true, "html", "text");
            //msg.SetGoogleAnalytics(true, "");

            var response = await client.SendEmailAsync(msg);
            var responseMessage = response.IsSuccessStatusCode
                ? $"Successfully queued email to send to '{toEmail}'"
                : $"Failed to send email to '{toEmail}'";
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(responseMessage);
            }
            else
            {
                _logger.LogError(responseMessage);
            }
        }
    }
}