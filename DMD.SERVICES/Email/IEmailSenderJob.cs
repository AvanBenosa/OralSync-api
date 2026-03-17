using DMD.SERVICES.Email.Models;

namespace DMD.SERVICES.Email
{
    public interface IEmailSenderJob
    {
        Task SendAsync(PatientEmailJobRequest request, CancellationToken cancellationToken = default);
    }
}
