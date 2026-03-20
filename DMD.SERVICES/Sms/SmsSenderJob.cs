using DMD.SERVICES.Sms.Models;
using Microsoft.Extensions.Logging;

namespace DMD.SERVICES.Sms
{
    public class SmsSenderJob : ISmsSenderJob
    {
        private readonly ISmsService smsService;
        private readonly ILogger<SmsSenderJob> logger;

        public SmsSenderJob(ISmsService smsService, ILogger<SmsSenderJob> logger)
        {
            this.smsService = smsService;
            this.logger = logger;
        }

        public async Task SendAsync(PatientSmsJobRequest request, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Sending queued patient SMS. PatientId: {PatientId}, RecipientNumber: {RecipientNumber}, Priority: {UsePriority}",
                request.PatientId,
                request.RecipientNumber,
                request.UsePriority);

            await smsService.SendAsync(request, cancellationToken);
        }
    }
}
