using DMD.SERVICES.ProtectionProvider.Model;

namespace DMD.SERVICES.ProtectionProvider
{
    public interface IProtectionProvider
    {
        Task<string> Decrypt(string item, string purpose);
        Task<T> DecryptJson<T>(string item, string purpose);
        Task<string> Encrypt(string item, string purpose);
        Task<string> EncryptJson(object item, string purpose);
        string GeneratePin(int length);
        Task<EncryptionModel> GetItemEncryption(List<EncryptionModel> model, int item, string purpose, string itemPhrase = "");
        EncryptionModel GetItemRC2Encryption(List<EncryptionModel> model, int item, string itemPhrase = "");
        //string DmdAcctsKeyEncrypt();
        Task<T> KeyDecrypt<T>(string key);
        Task<string> KeyEncrypt(string key);
        string RC2Decrypt(string phrase);
        string RC2Encrypt(string phrase);
        bool ValidatePassword(string password);


        Task<string> Key2Encrypt(string key);
        Task<string> Key2Decrypt(string key);
    }
}
