namespace DMD.HANGFIRE.ClinicAutoLocks
{
    public interface IClinicAutoLockJob
    {
        Task AutoLockExpiredClinicsAsync(CancellationToken cancellationToken = default);
    }
}
