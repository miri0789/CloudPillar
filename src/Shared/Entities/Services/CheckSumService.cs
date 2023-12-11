
using System.Security.Cryptography;
using Shared.Enums;

namespace Shared.Entities.Services;

public class CheckSumService : ICheckSumService
{
    public async Task<string> CalculateCheckSumAsync(byte[] data, CheckSumType checkSumType = CheckSumType.SHA256)
    {
        Stream stream = new MemoryStream(data);
        return await CalculateCheckSumAsync(stream, checkSumType);
    }
    public async Task<string> CalculateCheckSumAsync(Stream stream, CheckSumType checkSumType = CheckSumType.SHA256)
    {

        switch (checkSumType)
        {
            case CheckSumType.SHA256:
                return await CalculateMdsCheckSumAsync(stream);
            default:
                throw new ArgumentNullException("CheckSum Type not provided");
        }
    }

    private async Task<string> CalculateMdsCheckSumAsync(Stream stream)
    {
        using (var md5 = SHA256.Create())
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] hashBytes = await md5.ComputeHashAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);
            string checkSum = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return checkSum;
        }
    }
}