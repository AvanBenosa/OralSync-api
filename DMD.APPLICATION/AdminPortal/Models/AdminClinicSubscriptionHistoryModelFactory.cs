using DMD.APPLICATION.Common.ProtectedIds;
using DMD.DOMAIN.Entities.UserProfile;
using DMD.SERVICES.ProtectionProvider;

namespace DMD.APPLICATION.AdminPortal.Models
{
    public static class AdminClinicSubscriptionHistoryModelFactory
    {
        public static async Task<AdminClinicSubscriptionHistoryModel> CreateAsync(
            ClinicSubsciptionHistory item,
            IProtectionProvider protectionProvider)
        {
            return new AdminClinicSubscriptionHistoryModel
            {
                Id = await protectionProvider.EncryptIntIdAsync(
                    item.Id,
                    ProtectedIdPurpose.ClinicSubscriptionHistory) ?? string.Empty,
                ClinicId = await protectionProvider.EncryptIntIdAsync(
                    item.ClinicProfileId,
                    ProtectedIdPurpose.Clinic) ?? string.Empty,
                PaymentDate = item.PaymentDate,
                SubscriptionType = item.Subsciption.ToString(),
                TotalAmount = item.TotalAmount
            };
        }
    }
}
