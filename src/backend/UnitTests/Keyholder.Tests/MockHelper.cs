using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

public static class MockHelper
{

    private const int KEY_SIZE_IN_BITS = 4096;
    private const string DEVICE_SECRET_NAME = "DeviceSecret";
    public const string CHANGE_SPEC_ID = "123";
    public const string PATCH_KEY = "transitPackage";
    public const string CERTIFICATE_SUBJECT = "CN=";
    public const string DEVICE_SECRET_EXTENSION_KEY = "1.1.1.1";

    public static string _baseDesierd { get; } = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";
    public static string _baseReported { get; } = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";


    public static X509Certificate2 GenerateCertificate(string subjectName)
    {
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"CN={subjectName}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] deviceSecretKeyValue = Encoding.UTF8.GetBytes("NNN");
            var deviceSecretKeyExtension = new X509Extension(
                new Oid(DEVICE_SECRET_EXTENSION_KEY, DEVICE_SECRET_NAME),
                deviceSecretKeyValue, false
               );


            request.CertificateExtensions.Add(deviceSecretKeyExtension);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(60));

            return certificate;

        }
    }
}