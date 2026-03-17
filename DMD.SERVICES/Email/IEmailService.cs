using DMD.SERVICES.Email.Models;

namespace DMD.SERVICES.Email
{
    public interface IEmailService
    {
        Task SendAsync(PatientEmailJobRequest request, CancellationToken cancellationToken = default);
    }
}
