
using System.Security.Cryptography;
using Shared.Enums;

namespace Shared.Entities.Services;

public class CheckSumService : ICheckSumService
{
    public async Task<string> CalculateCheckSumAsync(Stream stream, CheckSumType checkSumType = CheckSumType.MD5)
    {
        switch (checkSumType)
        {
            case CheckSumType.MD5:
                return await CalculateMdsCheckSumAsync(stream);
            default:
                throw new ArgumentNullException("CheckSum Type not provided");
        }
    }
    public async Task<string> CalculateCheckSumAsync(byte[] data, CheckSumType checkSumType = CheckSumType.MD5)
    {
        switch (checkSumType)
        {
            case CheckSumType.MD5:
                return await CalculateMdsCheckSumAsync(data);
            default:
                throw new ArgumentNullException("CheckSum Type not provided");
        }
    }

    private async Task<string> CalculateMdsCheckSumAsync(Stream stream)
    {
        using (var md5 = MD5.Create())
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] hashBytes = await MD5.Create().ComputeHashAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);
            string checkSum = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return checkSum;
        }
    }

    private async Task<string> CalculateMdsCheckSumAsync(byte[] data)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hashBytes = await md5.ComputeHashAsync(new MemoryStream(data));
            string checksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return checksum;
        }
    }
}