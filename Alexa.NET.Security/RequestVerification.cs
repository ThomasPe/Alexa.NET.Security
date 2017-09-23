using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Alexa.NET.Security
{
    /// <summary>
    /// This class holds all verification methods needed to authorize requests to an Alexa backend
    /// </summary>
    public static class RequestVerification
    {
        /// <summary>
        /// Verfiy runs through all verification steps
        /// </summary>
        /// <param name="encodedSignature"></param>
        /// <param name="certificatePath"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static async Task<bool> Verify(string encodedSignature, Uri certificatePath, string body)
        {
            if (!VerifyCertificateUrl(certificatePath))
            {
                return false;
            }

            var certificate = await GetCertificate(certificatePath);
            if (!ValidSigningCertificate(certificate) || !VerifyChain(certificate))
            {
                return false;
            }

            if (!AssertHashMatch(certificate, encodedSignature, body))
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Checks if the body has been modified making the signature invalid
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="encodedSignature"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static bool AssertHashMatch(X509Certificate2 certificate, string encodedSignature, string body)
        {
            byte[] signature;
            try
            {
                signature = Convert.FromBase64String(encodedSignature);
            }
            catch
            {
                return false;
            }
            var rsa = certificate.GetRSAPublicKey();

            return rsa.VerifyData(Encoding.UTF8.GetBytes(body), signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        }

        /// <summary>
        /// Download the certificate from the server
        /// </summary>
        /// <param name="certificatePath"></param>
        /// <returns></returns>
        public static async Task<X509Certificate2> GetCertificate(Uri certificatePath)
        {
            var response = await new HttpClient().GetAsync(certificatePath);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return new X509Certificate2(bytes);
        }

        /// <summary>
        /// Verify the certificate chain
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool VerifyChain(X509Certificate2 certificate)
        {
            X509Chain certificateChain = new X509Chain();
            certificateChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return certificateChain.Build(certificate);
        }

        /// <summary>
        /// Check if certificate is valid for this point in time
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        private static bool ValidSigningCertificate(X509Certificate2 certificate)
        {
            return DateTime.Now < certificate.NotAfter && DateTime.Now > certificate.NotBefore &&
                   certificate.GetNameInfo(X509NameType.SimpleName, false) == "echo-api.amazon.com";
        }

        /// <summary>
        /// Verify that the certificate is stored in the right place
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static bool VerifyCertificateUrl(Uri certificate)
        {
            return certificate.Scheme == "https" &&
                certificate.Host == "s3.amazonaws.com" &&
                certificate.LocalPath.StartsWith("/echo.api") &&
                certificate.IsDefaultPort;
        }
    }
}