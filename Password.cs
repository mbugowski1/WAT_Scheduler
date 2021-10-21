using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Security;

namespace WAT_Planner
{
    class Password : IDisposable
    {
        static SecureString password = new SecureString();
        static SecureString login = new SecureString();
        readonly Aes cypher = AesCryptoServiceProvider.Create();
        public Password()
        {
            if (!Directory.Exists(Data.HomePath))
                Directory.CreateDirectory(Data.HomePath);
            if(!File.Exists(Data.KeyPath))
            {
                if (File.Exists(Data.PasswordPath))
                    File.Delete(Data.PasswordPath);
                File.Create(Data.PasswordPath).Close();
            }

            byte[] stream = File.ReadAllBytes(Data.KeyPath);
            if(stream.Length == 48)
            {
                byte[] key = new byte[32];
                byte[] iv = new byte[16];
                for (int i = 0; i < 32; i++)
                    key[i] = stream[i];
                for (int i = 0; i < 16; i++)
                    iv[i] = stream[i + 32];
                cypher.Key = key;
                cypher.IV = iv;
            }
            else
            {
                cypher.KeySize = 256;
                cypher.GenerateKey();
                stream = new byte[48];
                for (int i = 0; i < 32; i++)
                    stream[i] = cypher.Key[i];
                for (int i = 0; i < 16; i++)
                    stream[i + 32] = cypher.IV[i];
                File.WriteAllBytes(Data.KeyPath, stream);
            }
        }
        public static byte[] Load(string file)
        {
            if (!File.Exists(file)) return null;
            return File.ReadAllBytes(file);
        }
        public static void Write(byte[] stream, string file)
        {
            if (!File.Exists(file))
                File.Create(file).Close();
            File.WriteAllBytes(file, stream);
        }
        public byte[] encrypt(String data)
        {
            ICryptoTransform encryptor = cypher.CreateEncryptor();
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(data);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
        public string decrypt(byte[] stream)
        {
            ICryptoTransform decryptor = cypher.CreateDecryptor();
            using (MemoryStream msDecrypt = new MemoryStream(stream))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
        public void Dispose()
        {
            cypher.Clear();
        }
    }
}
