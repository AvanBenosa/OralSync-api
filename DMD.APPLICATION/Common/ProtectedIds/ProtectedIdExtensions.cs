using DMD.SERVICES.ProtectionProvider;

namespace DMD.APPLICATION.Common.ProtectedIds
{
    public static class ProtectedIdExtensions
    {
        public static async Task<string> EncryptIntIdAsync(
            this IProtectionProvider protectionProvider,
            int id,
            string purpose)
        {
            return await protectionProvider.Encrypt(id.ToString(), purpose);
        }

        public static async Task<string?> EncryptNullableIntIdAsync(
            this IProtectionProvider protectionProvider,
            int? id,
            string purpose)
        {
            if (!id.HasValue)
            {
                return null;
            }

            return await protectionProvider.Encrypt(id.Value.ToString(), purpose);
        }

        public static async Task<int?> DecryptNullableIntIdAsync(
            this IProtectionProvider protectionProvider,
            string? encryptedId,
            string purpose)
        {
            if (string.IsNullOrWhiteSpace(encryptedId))
            {
                return null;
            }

            var decryptedId = await protectionProvider.Decrypt(encryptedId.Trim(), purpose);
            return int.TryParse(decryptedId, out var parsedId) ? parsedId : null;
        }

        public static async Task<int> DecryptIntIdAsync(
            this IProtectionProvider protectionProvider,
            string encryptedId,
            string purpose)
        {
            var decryptedId = await protectionProvider.Decrypt(encryptedId.Trim(), purpose);
            if (!int.TryParse(decryptedId, out var parsedId))
            {
                throw new InvalidOperationException("Invalid protected identifier.");
            }

            return parsedId;
        }

        public static async Task<string?> EncryptStringIdAsync(
            this IProtectionProvider protectionProvider,
            string? id,
            string purpose)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return await protectionProvider.Encrypt(id.Trim(), purpose);
        }

        public static async Task<string?> DecryptStringIdAsync(
            this IProtectionProvider protectionProvider,
            string? encryptedId,
            string purpose)
        {
            if (string.IsNullOrWhiteSpace(encryptedId))
            {
                return null;
            }

            return await protectionProvider.Decrypt(encryptedId.Trim(), purpose);
        }
    }
}
