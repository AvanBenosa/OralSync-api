using DMD.SERVICES.Sms.Models;

namespace DMD.SERVICES.Sms
{
    public interface ISmsQueueService
    {
        Task QueueAsync(PatientSmsJobRequest request, CancellationToken cancellationToken = default);
    }
}
