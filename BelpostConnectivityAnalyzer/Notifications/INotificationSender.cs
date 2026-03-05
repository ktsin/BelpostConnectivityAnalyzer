using BelpostConnectivityAnalyzer.Data;
using BelpostConnectivityAnalyzer.Models;

namespace BelpostConnectivityAnalyzer.Notifications;

public interface INotificationSender
{
    Task SendStatusChangeAsync(
        List<CountryStatusChange> changes,
        long syncLogId,
        NotificationLogRepository notificationLogRepository,
        CancellationToken ct);
}