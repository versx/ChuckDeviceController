namespace ChuckDeviceConfigurator.Services.Net.Mail;

public class AuthMessageSenderOptions
{
    public bool Enabled { get; set; }

    public string? SendGridKey { get; set; }

    public string? FromName { get; set; } = Strings.AssemblyName;

    public string? FromEmailAddress { get; set; }
}