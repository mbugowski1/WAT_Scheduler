using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Security;

namespace WAT_Planner
{
    class Password : IDisposable
    {
        static readonly string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + '/' + Data.homeName + '/';
        static SecureString password = new SecureString();
        static SecureString login = new SecureString();
        readonly Aes cypher = AesCryptoServiceProvider.Create();
        public Password()
        {
            string keyPath = home + Data.keyName;
            string passPath = home + Data.passwordName; 
            if (!Directory.Exists(home))
                Directory.CreateDirectory(home);
            if(!File.Exists(keyPath))
            {
                if (File.Exists(passPath))
                    File.Delete(passPath);
                File.Create(keyPath).Close();
            }

            byte[] stream = File.ReadAllBytes(keyPath);
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
                File.WriteAllBytes(keyPath, stream);
            }
        }
        public static byte[] Load(string fileName)
        {
            string file = home + fileName;
            if (!File.Exists(file)) return null;
            return File.ReadAllBytes(file);
        }
        public static byte[] LoadFile(string fileName)
        {
            string file = fileName;
            if (!File.Exists(file)) return null;
            return File.ReadAllBytes(file);
        }
        public static void Write(byte[] stream, string fileName)
        {
            string file = home + fileName;
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
