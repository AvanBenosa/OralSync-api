using DMD.SERVICES.Email.Models;
using Hangfire;

namespace DMD.SERVICES.Email
{
    public class EmailQueueService : IEmailQueueService
    {
        private readonly IBackgroundJobClient backgroundJobClient;

        public EmailQueueService(IBackgroundJobClient backgroundJobClient)
        {
            this.backgroundJobClient = backgroundJobClient;
        }

        public Task QueueAsync(PatientEmailJobRequest request, CancellationToken cancellationToken = default)
        {
            backgroundJobClient.Enqueue<IEmailSenderJob>(job =>
                job.SendAsync(request, CancellationToken.None));

            return Task.CompletedTask;
        }
    }
}
