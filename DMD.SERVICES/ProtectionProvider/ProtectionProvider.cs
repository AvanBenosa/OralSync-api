using AutoMapper;
using DMD.SERVICES.ProtectionProvider.Enums;
using DMD.SERVICES.ProtectionProvider.Model;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace DMD.SERVICES.ProtectionProvider
{
    public class ProtectionProvider : IProtectionProvider
    {
        private readonly IMapper mapper;
        private readonly IConfiguration appConfig;
        private readonly IDataProtectionProvider provider;


        private readonly RC2CryptoServiceProvider rc2CSP = new RC2CryptoServiceProvider();
        private readonly byte[] Algo;
        private readonly byte[] AlgoVector;


        public ProtectionProvider(IDataProtectionProvider provider, IMapper mapper, IConfiguration appConfig)
        {

            this.mapper = mapper;
            this.provider = provider;
            this.appConfig = appConfig;

            Algo = rc2CSP.Key;
            AlgoVector = rc2CSP.IV;
        }

        public async Task<string> Encrypt(string item, string purpose)
        {
            return await Task.Run(() =>
            {
                var protector = provider.CreateProtector(purpose);
                return protector.Protect(item);
            });
        }

        public async Task<string> EncryptJson(object item, string purpose)
        {
            return await Task.Run(() =>
            {
                var protector = provider.CreateProtector(purpose);
                var json = JsonConvert.SerializeObject(item);
                return protector.Protect(json);
            });
        }

        public async Task<string> Decrypt(string item, string purpose)
        {
            return await Task.Run(() =>
            {
                var protector = provider.CreateProtector(purpose);
                return protector.Unprotect(item);
            });
        }

        public async Task<T> DecryptJson<T>(string item, string purpose)
        {
            return await Task.Run(() =>
            {
                var protector = provider.CreateProtector(purpose);
                var jsonString = protector.Unprotect(item);
                var json = JsonConvert.DeserializeObject(jsonString);
                return mapper.Map<T>(json);
            });
        }

        private string GetStringEnv(EnvVariables env)
        {
            return env switch
            {
                EnvVariables.Test => "Test",
                EnvVariables.Production => "Production",
                EnvVariables.Development => "Development",
                _ => "Local",
            };
        }

        public async Task<EncryptionModel> GetItemEncryption(List<EncryptionModel> model, int item, string purpose, string itemPhrase = "")
        {

            var encryptedModel = model.FirstOrDefault(x => x.IntItem == item);

            if (encryptedModel == null)
            {
                encryptedModel = new EncryptionModel
                {
                    IntItem = item,
                    Encrypted = await Encrypt(item.ToString(), purpose),
                    ItemPhrase = itemPhrase
                };
                model.Add(encryptedModel);
            }

            return encryptedModel;
        }

        public string RC2Encrypt(string phrase)
        {
            try
            {
                //Temporary for developer and QA testing 
                var env = appConfig.GetSection("ASPNETCORE_ENVIRONMENT").Value;
                if (!string.IsNullOrEmpty(env) && env.ToLower() == GetStringEnv(EnvVariables.Development).ToLower()) return phrase;

                ICryptoTransform encryptor = rc2CSP.CreateEncryptor(Algo, AlgoVector);
                MemoryStream msEncrypt = new MemoryStream();
                CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                byte[] toEncrypt = Encoding.ASCII.GetBytes(phrase);
                csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
                csEncrypt.FlushFinalBlock();
                byte[] encrypted = msEncrypt.ToArray();
                char[] padding = { '=' };
                string tobase64 = Convert.ToBase64String(encrypted)
                .TrimEnd(padding).Replace('+', '-').Replace('/', '_');

                return HttpUtility.UrlEncode(tobase64);
            }
            catch (Exception error) { throw new Exception(error.GetBaseException().Message); }

        }

        public string RC2Decrypt(string phrase)
        {
            try
            {
                var result = "";
                if (phrase != null)
                {
                    //Temporary for developer and QA testing 
                    var env = appConfig.GetSection("ASPNETCORE_ENVIRONMENT").Value;
                    if (!string.IsNullOrEmpty(env) && env.ToLower() == GetStringEnv(EnvVariables.Development).ToLower()) return phrase;

                    int b = 0;
                    string toBase64 = HttpUtility.UrlDecode(phrase);
                    string base64 = toBase64.Replace('_', '/').Replace('-', '+');

                    switch (phrase.Length % 4)
                    {
                        case 2: base64 += "=="; break;
                        case 3: base64 += "="; break;
                    }

                    var p0 = Convert.FromBase64String(base64);
                    ICryptoTransform decryptor = rc2CSP.CreateDecryptor(Algo, AlgoVector);
                    MemoryStream msDecrypt = new MemoryStream(p0);
                    CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                    StringBuilder roundtrip = new StringBuilder();

                    do
                    {
                        b = csDecrypt.ReadByte();
                        if (b != -1) { roundtrip.Append((char)b); }
                    } while (b != -1);

                    result = roundtrip.ToString();
                }

                return result;

            }
            catch (Exception error)
            {
                throw new Exception(error.GetBaseException().Message);
            }

        }

        public EncryptionModel GetItemRC2Encryption(List<EncryptionModel> model, int item, string itemPhrase = "")
        {
            var encryptedModel = model.FirstOrDefault(x => x.IntItem == item);

            if (encryptedModel == null)
            {
                encryptedModel = new EncryptionModel
                {
                    IntItem = item,
                    Encrypted = RC2Encrypt(item.ToString()),
                    ItemPhrase = itemPhrase
                };
                model.Add(encryptedModel);
            }

            return encryptedModel;
        }

        public bool ValidatePassword(string password)
        {
            int validConditions = 0;
            if (password.Length <= 7) return false;

            char[] special = { '@', '#', '$', '%', '^', '&', '+', '=' };
            if (password.IndexOfAny(special) == -1) return false;

            foreach (char c in password)
            {
                if (c >= 'a' && c <= 'z')
                {
                    validConditions++;
                    break;
                }
            }

            foreach (char c in password)
            {
                if (c >= 'A' && c <= 'Z')
                {
                    validConditions++;
                    break;
                }
            }

            if (validConditions == 0) return false;

            foreach (char c in password)
            {
                if (c >= '0' && c <= '9')
                {
                    validConditions++;
                    break;
                }
            }

            if (validConditions == 2) return false;

            return true;
        }

        ////Decrytion method found in accounts services
        //public string DMDAcctsKeyEncrypt()
        //{
        //    var userAccount = new UserAccount();
        //    var random = new Random((int)DateTime.Now.Ticks);
        //    var role = IdentityHelper.Role;
        //    var toEncryp = $"{IdentityHelper.UserId}";
        //    role.ForEach(x =>
        //    {
        //        var randomValue = random.Next(1, 9);
        //        toEncryp += $"_{(int)userAccount.GetEnumRole(x)}_{randomValue}";
        //    });

        //    //encrypt 1
        //    var refByte1 = Encoding.UTF8.GetBytes(toEncryp);
        //    var refString1 = Convert.ToBase64String(refByte1);

        //    //encrypt 2
        //    var refByte2 = Encoding.Unicode.GetBytes(refString1);
        //    var refString2 = Convert.ToBase64String(refByte2);

        //    //encrypt 3
        //    var refByte3 = Encoding.ASCII.GetBytes(refString2);
        //    var refString3 = Convert.ToBase64String(refByte3)
        //        .Replace('+', '-')
        //        .Replace('/', '_')
        //        .Replace("W", "w");

        //    return HttpUtility.UrlEncode(refString3);
        //}

        public string GeneratePin(int length)
        {
            var result = "";
            do { result += Guid.NewGuid().ToString().Replace("-", ""); }
            while (length > result.Length);
            return result.Substring(0, length);
        }

        public async Task<string> KeyEncrypt(string key)
        {
            return await Task.Run(() =>
            {
                var refByte1 = Encoding.UTF8.GetBytes(key);
                var refString1 = Convert.ToBase64String(refByte1);

                var refByte2 = Encoding.Unicode.GetBytes(refString1);
                var refString2 = Convert.ToBase64String(refByte2);

                var refByte3 = Encoding.ASCII.GetBytes(refString2);
                var refString3 = Convert.ToBase64String(refByte3)
                    .Replace('+', '-').Replace('/', '_').Replace("W", "w");

                return HttpUtility.UrlEncode(refString3);
            });
        }

        public async Task<T> KeyDecrypt<T>(string key)
        {
            return await Task.Run(() =>
            {
                key = HttpUtility.UrlDecode(key).Replace('_', '/').Replace('-', '+').Replace("w", "W");

                var refByte3 = Convert.FromBase64String(key);
                var refString3 = Encoding.ASCII.GetString(refByte3).Replace("", "0");

                var refByte2 = Convert.FromBase64String(refString3);
                var refString2 = Encoding.Unicode.GetString(refByte2).Replace("", "0");

                var refByte1 = Convert.FromBase64String(refString2);
                var refString1 = Encoding.UTF8.GetString(refByte1).Replace("", "0");

                return mapper.Map<T>(refString1);
            });
        }

        /// <summary> 
        /// PUPrefix = _Nc2 = V0hkQ1QwRkhUVUZOwjBFOQ==
        /// Password = admin
        /// Pct = @V0hkQ1QwRkhUVUZOwjBFOQadmin!
        /// PHash = VVVGQ1YwRkVRVUZoUVVKeVFVwkZRVTFSUwxKQlNHTkJwV2RDY2tGSFowRldVVUpYUVVaVlFwZG5RbEJCU0dOQllXZENRMEZGV1VGVwQwSlNRVwRGUVZwQlFuUkJSMnRCww1kQmFFRkJQVDA9

        /// UserName = Admin
        /// PUPrefix = _Nc2 = V0hkQ1QwRkhUVUZOwjBFOQ==
        /// Pct = +V0hkQ1QwRkhUVUZOwjBFOQAdmin
        /// UserNameHash = UzNkQ1YwRkVRVUZoUVVKeVFVwkZRVTFSUwxKQlNHTkJwV2RDY2tGSFowRldVVUpYUVVaVlFwZG5RbEJCU0dOQllXZENRMEZGV1VGVwQwSlNRVVZGUVZwQlFuUkJSMnRCww1kQlBRPT0 =

        /// </summary>
        /// <param name="key">Key to encrypt</param>
        /// <returns>Encrypted string</returns>
        public async Task<string> Key2Encrypt(string key)
        {
            return await Task.Run(() =>
            {
                return Convert.ToBase64String(Encoding.
                    ASCII.GetBytes(Convert.ToBase64String(Encoding.
                    UTF8.GetBytes(Convert.ToBase64String(Encoding.
                    Unicode.GetBytes(key))))))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("W", "w");
            });
        }

        public async Task<string> Key2Decrypt(string key)
        {
            return await Task.Run(() =>
            {
                return Encoding
                .Unicode.GetString(Convert.FromBase64String(Encoding
                .UTF8.GetString(Convert.FromBase64String(Encoding
                .ASCII.GetString(Convert.FromBase64String(key
                    .Replace('_', '/').Replace('-', '+')
                    .Replace("w", "W"))).Replace("", "0")))
                    .Replace("", "0"))).Replace("", "0");
            });
        }
    }
}
