using System.Text.Json;
using BelpostConnectivityAnalyzer.Configuration;
using BelpostConnectivityAnalyzer.Data;
using BelpostConnectivityAnalyzer.Json;
using BelpostConnectivityAnalyzer.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BelpostConnectivityAnalyzer.Notifications;

public sealed class MailStatusChangeNotificationSender(
    IOptions<SmtpSettings> smtpOptions,
    IConfiguration configuration,
    ILogger<MailStatusChangeNotificationSender> logger)
    : INotificationSender
{
    private const string ReportType = "MailStatusChangeV1";

    public async Task SendStatusChangeAsync(
        List<CountryStatusChange> changes,
        long syncLogId,
        NotificationLogRepository notificationLogRepository,
        CancellationToken ct)
    {
        var reports = LoadReports(configuration["ReportConfigPath"] ?? "reports.json");
        var smtp = smtpOptions.Value;

        foreach (var report in reports)
        {
            if (!report.Enabled || report.Type != ReportType || report.Recipients.Length == 0)
                continue;

            var recipientsStr = string.Join(", ", report.Recipients);

            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(smtp.FromAddress));
                foreach (var r in report.Recipients)
                    message.To.Add(MailboxAddress.Parse(r));

                message.Subject = StatusChangeReport.BuildSubject(changes);
                var messageBody = new BodyBuilder
                {
                    HtmlBody = StatusChangeReport.BuildHtmlBody(changes),
                    TextBody = StatusChangeReport.BuildPlainTextBody(changes)
                };
                message.Body = messageBody.ToMessageBody();

                using var client = new SmtpClient();
                var secureSocketOptions = smtp.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

                await client.ConnectAsync(smtp.Host, smtp.Port, secureSocketOptions, ct);

                if (!string.IsNullOrEmpty(smtp.Username))
                    await client.AuthenticateAsync(smtp.Username, smtp.Password, ct);

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);

                logger.LogInformation("Notification sent to {Recipients}", recipientsStr);
                notificationLogRepository.Insert(syncLogId, ReportType, recipientsStr, "sent", null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to send notification to {Recipients}", recipientsStr);
                notificationLogRepository.Insert(syncLogId, ReportType, recipientsStr, "failed", ex.Message);
            }
        }
    }

    private static ReportConfigEntry[] LoadReports(string path)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize(stream, AppJsonContext.Default.ReportConfigEntryArray) ?? [];
        }
        catch
        {
            return [];
        }
    }
}