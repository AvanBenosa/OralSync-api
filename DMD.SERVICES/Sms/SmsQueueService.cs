using DMD.SERVICES.Sms.Models;
using Hangfire;

namespace DMD.SERVICES.Sms
{
    public class SmsQueueService : ISmsQueueService
    {
        private readonly IBackgroundJobClient backgroundJobClient;

        public SmsQueueService(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        public Task QueueAsync(PatientSmsJobRequest request, CancellationToken cancellationToken = default)
        {
            backgroundJobClient.Enqueue<ISmsSenderJob>(job =>
                job.SendAsync(request, cancellationToken));

            return Task.CompletedTask;
        }
    }
}
