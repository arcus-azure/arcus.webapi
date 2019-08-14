using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using GuardNet;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Arcus.WebApi.Unit.Security.Doubles
{
    /// <summary>
    /// Exposes several certificate generation function to control the OCSP and/or CRL information of the certificates.
    /// </summary>
    public static class SelfSignedCertificate
    {
        /// <summary>
        /// Generates a self-signed certificate.
        /// </summary>
        public static X509Certificate2 Create()
        {
            return CreateWithSubject("TestSubject");
        }

        /// <summary>
        /// Generates a self-signed certificate with a specified <paramref name="subjectName"/>.
        /// </summary>
        /// <param name="subjectName">The subject name of the self-signed certificate.</param>
        /// <exception cref="ArgumentException">When the <paramref name="subjectName"/> is <c>null</c>.</exception>
        public static X509Certificate2 CreateWithSubject(string subjectName)
        {
            Guard.NotNullOrWhitespace(subjectName, nameof(subjectName), "Subject name should not be blank");

            return CreateWithIssuerAndSubjectName("TestCA", subjectName);
        }

        /// <summary>
        /// Generates a self-signed certificate with a specified <paramref name="issuerName"/> and <paramref name="subjectName"/>.
        /// </summary>
        /// <param name="subjectName">The subject name of the self-signed certificate.</param>
        /// <param name="issuerName">The issuer name of the self-signed certificate.</param>
        /// <exception cref="ArgumentException">When the <paramref name="subjectName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="issuerName"/> is <c>null</c>.</exception>
        public static X509Certificate2 CreateWithIssuerAndSubjectName(string issuerName, string subjectName)
        {
            Guard.NotNullOrWhitespace(subjectName, nameof(subjectName), "Subject name should not be blank");
            Guard.NotNullOrWhitespace(issuerName, nameof(issuerName), "Issuer name should not be blank");

            issuerName = issuerName.StartsWith("CN=") ? issuerName : "CN=" + issuerName;
            subjectName = subjectName.StartsWith("CN=") ? subjectName : "CN=" + subjectName;

            SecureRandom random = GetSecureRandom();
            AsymmetricCipherKeyPair subjectKeyPair = GenerateKeyPair(random, 2048);
            BigInteger serialNumber = GenerateSerialNumber(random);

            using (X509Certificate2 issuerCert = GenerateCA(issuerName))
            {
                AsymmetricCipherKeyPair issuerKeyPair = DotNetUtilities.GetKeyPair(issuerCert.PrivateKey);
                var issuerSerialNumber = new BigInteger(issuerCert.GetSerialNumber());

                var certificateGenerator = new X509V3CertificateGenerator();
            
                certificateGenerator.AddIssuer(issuerName, issuerKeyPair, issuerSerialNumber);
                certificateGenerator.SetSubjectDN(new X509Name(subjectName));

                certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
                certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(30));
                certificateGenerator.SetPublicKey(subjectKeyPair.Public);
                certificateGenerator.SetSerialNumber(serialNumber);

                certificateGenerator.AddSubjectKeyIdentifier(subjectKeyPair);
                certificateGenerator.AddBasicConstraints(isCertificateAuthority: false);

                X509Certificate certificate = certificateGenerator.GenerateCertificateAsn1(issuerKeyPair, random);
                X509Certificate2 convertCertificate = ConvertCertificate(certificate, subjectKeyPair, random);
                return convertCertificate;
            }
        }

        private static X509Certificate2 GenerateCA(string subjectName)
        {
            SecureRandom random = GetSecureRandom();
            AsymmetricCipherKeyPair subjectKeyPair = GenerateKeyPair(random, 2048);
            BigInteger serialNumber = GenerateSerialNumber(random);

            var certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.AddIssuer(subjectName, subjectKeyPair, serialNumber);
            certificateGenerator.SetSubjectDN(new X509Name(subjectName));

            certificateGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Date.AddYears(30));
            certificateGenerator.SetPublicKey(subjectKeyPair.Public);
            certificateGenerator.SetSerialNumber(serialNumber);

            certificateGenerator.AddSubjectKeyIdentifier(subjectKeyPair);
            certificateGenerator.AddBasicConstraints(isCertificateAuthority: true);

            X509Certificate certificate = certificateGenerator.GenerateCertificateAsn1(subjectKeyPair, random);
            return ConvertCertificate(certificate, subjectKeyPair, random);
        }

        private static AsymmetricCipherKeyPair GenerateKeyPair(SecureRandom random, int strength)
        {
            var keyGenerationParameters = new KeyGenerationParameters(random, strength);
            var keyPairGenerator = new RsaKeyPairGenerator();

            keyPairGenerator.Init(keyGenerationParameters);
            return keyPairGenerator.GenerateKeyPair();
        }

        private static SecureRandom GetSecureRandom()
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            return new SecureRandom(randomGenerator);
        }

        private static void AddIssuer(
            this X509V3CertificateGenerator certificateGenerator,
            string issuerName,
            AsymmetricCipherKeyPair issuerKeyPair,
            BigInteger issuerSerialNumber)
        {
            var issuerDistributionName = new X509Name(issuerName);
            certificateGenerator.SetIssuerDN(issuerDistributionName);
            var authorityKeyIdentifierExtension =
                new AuthorityKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(issuerKeyPair.Public),
                    new GeneralNames(new GeneralName(issuerDistributionName)),
                    issuerSerialNumber);

            certificateGenerator.AddExtension(
                oid: X509Extensions.AuthorityKeyIdentifier.Id, 
                critical: false, 
                extensionValue: authorityKeyIdentifierExtension);
        }

        private static X509Certificate GenerateCertificateAsn1(
            this X509V3CertificateGenerator certificateGenerator,
            AsymmetricCipherKeyPair issuerKeyPair,
            SecureRandom random)
        {
            return certificateGenerator.Generate(new Asn1SignatureFactory("SHA256WithRSA", issuerKeyPair.Private, random));
        }

        private static BigInteger GenerateSerialNumber(SecureRandom random)
        {
            return BigIntegers.CreateRandomInRange(
                min: BigInteger.One, 
                max: BigInteger.ValueOf(long.MaxValue), 
                random: random);
        }

        private static void AddBasicConstraints(
            this X509V3CertificateGenerator certificateGenerator,
            bool isCertificateAuthority)
        {
            certificateGenerator.AddExtension(
                X509Extensions.BasicConstraints.Id, true, new BasicConstraints(isCertificateAuthority));
        }

        private static void AddSubjectKeyIdentifier(
            this X509V3CertificateGenerator certificateGenerator,
            AsymmetricCipherKeyPair subjectKeyPair)
        {
            var subjectKeyIdentifierExtension =
                new SubjectKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(subjectKeyPair.Public));

            certificateGenerator.AddExtension(
                oid: X509Extensions.SubjectKeyIdentifier.Id, 
                critical: false, 
                extensionValue: subjectKeyIdentifierExtension);
        }

        private static X509Certificate2 ConvertCertificate(
            X509Certificate certificate,
            AsymmetricCipherKeyPair subjectKeyPair,
            SecureRandom random)
        {
            var store = new Pkcs12Store();

            string friendlyName = certificate.SubjectDN.ToString();

            var certificateEntry = new X509CertificateEntry(certificate);
            store.SetCertificateEntry(friendlyName, certificateEntry);

            store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { certificateEntry });

            const string password = "password";
            var stream = new MemoryStream();
            store.Save(stream, password.ToCharArray(), random);

            return new X509Certificate2(
                stream.ToArray(),
                password,
                X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
        }
    }
}
