using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Authentication;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;

public static class MockHelper
{

    private const int KEY_SIZE_IN_BITS = 4096;
    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    public const string CHANGE_SPEC_ID = "123";
    public const string PATCH_KEY = "transitPackage";

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



    public static Twin CreateTwinMock(Dictionary<string, TwinChangeSpec> changeSpecDesired, Dictionary<string, TwinReportedChangeSpec> changeSpecReported
    , List<TwinReportedCustomProp>? twinReportedCustomProps = null, Dictionary<string, string>? changeSign = null, List<KnownIdentities>? knownIdentities = null)
    {
        var desiredJson = JObject.Parse(_baseDesierd);

        var desired = new TwinDesired()
        {
            ChangeSpec = changeSpecDesired,
            ChangeSign = changeSign,
        };
        desiredJson.Merge(JObject.Parse(JsonConvert.SerializeObject(desired.ConvertToJObject())));

        var reportedJson = JObject.Parse(_baseReported);
        var reported = new TwinReported()
        {
            ChangeSpec = changeSpecReported,
            Custom = twinReportedCustomProps,
            KnownIdentities = knownIdentities
        };
        reportedJson.Merge(JObject.Parse(JsonConvert.SerializeObject(reported.ConvertToJObject())));
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        };


        var twinProp = new TwinProperties()
        {
            Desired = new TwinCollection(JsonConvert.SerializeObject(desiredJson, settings)),
            Reported = new TwinCollection(JsonConvert.SerializeObject(reportedJson, settings))
        };
        return new Twin(twinProp);
    }

    public static X509Certificate2 GenerateCertificate(string deviceId, string secretKey, int expiredDays, string certificatePrefix)
    {
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{certificatePrefix}{deviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(secretKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid(ProvisioningConstants.ONE_MD_EXTENTION_KEY, ONE_MD_EXTENTION_NAME),
                oneMDKeyValue, false
               );


            request.CertificateExtensions.Add(OneMDKeyExtension);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(expiredDays));

            return certificate;

        }
    }
}