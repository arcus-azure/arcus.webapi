// ReSharper disable once CheckNamespace
namespace System.Security.Cryptography
{
    /// <summary>
    /// Extension class with Custom RSA extension methods
    /// </summary>
    // ReSharper disable once InconsistentNaming
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

            string modulus = TryConvertToBase64String(parameters.Modulus);
            string exponent = TryConvertToBase64String(parameters.Exponent);
            string pValue = TryConvertToBase64String(parameters.P);
            string qValue = TryConvertToBase64String(parameters.Q);
            string dpValue = TryConvertToBase64String(parameters.DP);
            string dqValue = TryConvertToBase64String(parameters.DQ);
            string inverseQ = TryConvertToBase64String(parameters.InverseQ);
            string dValue = TryConvertToBase64String(parameters.D);
            
            return "<RSAKeyValue>" +
                        $"<Modulus>{modulus}</Modulus>" +
                        $"<Exponent>{exponent}</Exponent>" +
                        $"<P>{pValue}</P>" +
                        $"<Q>{qValue}</Q>" +
                        $"<DP>{dpValue}</DP>" +
                        $"<DQ>{dqValue}</DQ>" +
                        $"<InverseQ>{inverseQ}</InverseQ>" +
                        $"<D>{dValue}</D>" +
                   "</RSAKeyValue>";
        }
        
        private static string TryConvertToBase64String(byte[] bytes)
        {
            if (bytes is null)
            {
                return null;
            }

            return Convert.ToBase64String(bytes);
        }
    }
}
