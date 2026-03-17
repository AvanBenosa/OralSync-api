using DMD.SERVICES.Email.Models;

namespace DMD.SERVICES.Email
{
    public interface IEmailQueueService
    {
        Task QueueAsync(PatientEmailJobRequest request, CancellationToken cancellationToken = default);
    }
}
