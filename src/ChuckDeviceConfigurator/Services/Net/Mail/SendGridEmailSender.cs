namespace ChuckDeviceConfigurator.Services.Net.Mail;

using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailSender : IEmailSender
{
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
        var fromName = Options.FromName ?? Strings.AssemblyName;
        var fromEmail = new EmailAddress(Options.FromEmailAddress, fromName);

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
        msg.SetClickTracking(enable: false, enableText: false);

        //msg.SetFooterSetting(true, "html", "text");
        //msg.SetGoogleAnalytics(true, "");
        // TODO: Add GoogleAnalytics config option

        var response = await client.SendEmailAsync(msg);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"Successfully queued email to send to '{toEmail}'");
        }
        else
        {
            _logger.LogError($"Failed to send email to '{toEmail}'");
        }
    }
}