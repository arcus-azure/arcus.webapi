using System;
using System.Security.Cryptography;

namespace Arcus.WebApi.Tests.Unit.Security.Extension
{
    /// <summary>
    /// Extension class with Custom RSA extension methods
    /// </summary>
    public static class RSAKeyExtensions
    {
        /// <summary>
        /// Creates an XML string that contains either the public and private key of the RSA object
        /// </summary>
        /// <param name="rsa">Represents the base class from which all implementations of the RSA algorithm inherit.</param>
        /// <param name="includePrivateParameters">true to include a public and private RSA key; false to include only the public ke</param>
        /// <returns>An XML string containing the key of the RSA object.</returns>
        public static string ToCustomXmlString(this RSA rsa, bool includePrivateParameters)
        {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            return
                $"<RSAKeyValue><Modulus>{(parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null)}</Modulus><Exponent>{(parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null)}</Exponent><P>{(parameters.P != null ? Convert.ToBase64String(parameters.P) : null)}</P><Q>{(parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null)}</Q><DP>{(parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null)}</DP><DQ>{(parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null)}</DQ><InverseQ>{(parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null)}</InverseQ><D>{(parameters.D != null ? Convert.ToBase64String(parameters.D) : null)}</D></RSAKeyValue>";
        }
    }
}
