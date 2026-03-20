using DMD.SERVICES.Sms.Models;

namespace DMD.SERVICES.Sms
{
    public interface ISmsService
    {
        Task<IReadOnlyList<SemaphoreSmsMessageResponse>> SendAsync(
            PatientSmsJobRequest request,
            CancellationToken cancellationToken = default);
    }
}
