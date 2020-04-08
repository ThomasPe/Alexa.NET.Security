using Alexa.NET.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Alexa.NET.Security.Functions
{
    public static class AlexaRequestValidationFunctionsExtension
    {
        private static KeyValuePair<Uri, X509Certificate2> certificateCache;

        /// <summary>
        /// Validates an incoming request against Amazon security guidelines.
        /// </summary>
        /// <param name="skillRequest">The current skill request.</param>
        /// <param name="httpRequest">The current http request.</param>
        /// <param name="log">The current logger.</param>
        /// <returns></returns>
        public static async Task<bool> ValidateRequestAsync(this SkillRequest skillRequest, HttpRequest httpRequest, ILogger log)
        {
            // get signature certification chain url
            var signatureCertChainUrl = GetSignatureCertChainUrlFromRequest(httpRequest);
            if (signatureCertChainUrl == null)
            {
                log.LogError("Validation failed, because of incorrect SignatureCertChainUrl");
                return false;
            }

            // get signature header
            var signature = GetSignatureFromRequest(httpRequest);
            if (string.IsNullOrWhiteSpace(signature))
            {
                log.LogError("Validation failed, because of empty signature");
                return false;
            }

            // get body
            var body = await GetBodyFromRequestAsync(httpRequest);
            if (string.IsNullOrWhiteSpace(body))
            {
                log.LogError("Validation failed, because of empty body");
                return false;
            }

            // validate timestamp
            if (!IsTimestampValid(skillRequest))
            {
                log.LogError("Validation failed, because timestamp is not valid");
                return false;
            }

            // validate signature, signaturecertchainurl and body
            if (!await IsRequestValid(signature, signatureCertChainUrl, body))
            {
                log.LogError("Validation failed, because verification of request failed");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the SignatureCertChainUrl value from http request headers.
        /// </summary>
        /// <param name="httpRequest">The current http request.</param>
        private static Uri GetSignatureCertChainUrlFromRequest(HttpRequest httpRequest)
        {
            httpRequest.Headers.TryGetValue("SignatureCertChainUrl", out var signatureCertChainUrlAsString);

            if (string.IsNullOrWhiteSpace(signatureCertChainUrlAsString))
                return null;

            Uri signatureCertChainUrl;
            try
            {
                signatureCertChainUrl = new Uri(signatureCertChainUrlAsString);
            }
            catch
            {
                return null;
            }

            return signatureCertChainUrl;
        }

        private static async Task<X509Certificate2> GetCertificate(Uri signatureCertChainUrl)
        {
            if (signatureCertChainUrl == null)
                return null;

            if (certificateCache.Key == null || certificateCache.Key.ToString().ToLowerInvariant() != signatureCertChainUrl.ToString().ToLowerInvariant())
            {
                try
                {
                    X509Certificate2 certificate = await RequestVerification.GetCertificate(signatureCertChainUrl);
                    if (certificate != null)
                        certificateCache = new KeyValuePair<Uri, X509Certificate2>(signatureCertChainUrl, certificate);

                    return certificate;
                }
                catch
                {
                    return null;
                }
            }
            else
                return certificateCache.Value;
        }

        /// <summary>
        /// Gets the Signature value from http request headers.
        /// </summary>
        /// <param name="httpRequest">The current http request.</param>
        private static string GetSignatureFromRequest(HttpRequest httpRequest)
        {
            httpRequest.Headers.TryGetValue("Signature", out var signature);
            return signature;
        }

        /// <summary>
        /// Gets the current body from http request.
        /// </summary>
        /// <param name="httpRequest">The current http request.</param>
        private static async Task<string> GetBodyFromRequestAsync(HttpRequest httpRequest)
        {
            httpRequest.Body.Position = 0;
            var body = await httpRequest.ReadAsStringAsync();
            httpRequest.Body.Position = 0;

            return body;
        }

        /// <summary>
        /// Validates the timestamp.
        /// </summary>
        /// <param name="skillRequest">The current skill request.</param>
        private static bool IsTimestampValid(SkillRequest skillRequest)
        {
            return RequestVerification.RequestTimestampWithinTolerance(skillRequest);
        }

        /// <summary>
        /// Validates the request.
        /// </summary>
        /// <param name="signature">The Signature value.</param>
        /// <param name="signatureCertChainUrl">The SignatureCertChainUrl value.</param>
        /// <param name="body">The body value.</param>
        /// <returns></returns>
        private static async Task<bool> IsRequestValid(string signature, Uri signatureCertChainUrl, string body)
        {
            return await RequestVerification.Verify(signature, signatureCertChainUrl, body, GetCertificate);
        }
    }
}