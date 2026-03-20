using DMD.SERVICES.Sms.Models;

namespace DMD.SERVICES.Sms
{
    public interface ISmsSenderJob
    {
        Task SendAsync(PatientSmsJobRequest request, CancellationToken cancellationToken = default);
    }
}
