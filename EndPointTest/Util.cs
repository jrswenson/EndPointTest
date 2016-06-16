using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EndPointTest
{
    public sealed class Util
    {

        //<summary>
        //The query string variable that indicates the IdentityProvider to ServiceProvider binding.
        //</summary>
        public const string BindingVarName = "binding";

        //<summary>
        //The query string parameter that contains error description for the login failure. 
        //</summary>
        public const string ErrorVarName = "error";

        public enum EncryptTypes
        {
            TripleDesCbc = 0,
            KwTripleDes = 1,
            Aes128Cbc = 2,
            KwAes128 = 3,
            Aes192Cbc = 4,
            KwAes192 = 5,
            Aes256Cbc = 6,
            KwAes256 = 7,
        }

        public static X509Certificate2 LoadSignKeyAndCertificate(string pfxLocatiuon, string pwd)
        {

            if (File.Exists(pfxLocatiuon) == false)
            {
                throw new ArgumentException("The certificate file " + Path.GetFileName(pfxLocatiuon) + " doesn't exist.");
            }

            return new X509Certificate2(pfxLocatiuon, pwd);
        }

        public static string DecryptUsingPublic(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("dataEncryptedBase64");

            var blah = Encoding.UTF8.GetString(data);
            var blah2 = DecodeBase64(blah);
            try
            {
                byte[] decrypted;
                using (var sp = new RSACryptoServiceProvider(2048))
                {
                    decrypted = sp.Decrypt(Encoding.UTF8.GetBytes(blah), true);
                }

                // I assume here that the decrypted data is intended to be a
                // human-readable string, and that it was UTF8 encoded.
                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string DecodeBase64(string strBase64)
        {
            var bytes = Convert.FromBase64String(strBase64);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string EncodeToBase64(string strBase)
        {
            var byt = Encoding.UTF8.GetBytes(strBase);
            return Convert.ToBase64String(byt);
        }

        private static X509Certificate2 LoadIdPCertKey()
        {
            var cert = new X509Certificate2(@"c:\dev\IdPCertKey", "password", X509KeyStorageFlags.MachineKeySet);
            return cert;
        }

        private static X509Certificate2 LoadSPCertKey()
        {
            var cert = new X509Certificate2(@"c:\dev\SPCertKey", "", X509KeyStorageFlags.MachineKeySet);
            return cert;
        }

        public static XmlDocument LoadXmlDocument(string file)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            xmlDocument.LoadXml(file);

            return xmlDocument;
        }

        public static XmlDocument LoadXmlDocumentFromPath(string fileName)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            xmlDocument.Load(fileName);

            return xmlDocument;

        }

        //public string GetAbsoluteUrl(Page page, String relativeUrl)
        //{
        //    return new Uri(page.Request.Url, page.ResolveUrl(relativeUrl)).ToString();
        //}

        public static string ShowError(Exception exc)
        {

            string str;
            if (exc.InnerException != null)
            {
                str = string.Format("An error occurred: {0}", exc.InnerException.Message);
            }
            else
            {
                str = string.Format("An error occurred: {0}", exc.Message);
            }

            return str;
        }

        public static string ShowError(Exception exc, string msg)
        {
            string str;
            if (exc.InnerException != null)
            {
                str = String.Format("{0}. An error occurred: {1}", msg, exc.InnerException.Message);
            }
            else
            {
                str = String.Format("{0}. An error occurred: {1}", msg, exc.Message);
            }

            return str;
        }
    }
}
