using Shared.Enums;

namespace Shared.Entities.Services;

public interface ICheckSumService
{
    Task<string> CalculateCheckSumAsync(Stream stream, CheckSumType checkSumType = CheckSumType.MD5);

    Task<string> CalculateCheckSumAsync(byte[] data, CheckSumType checkSumType = CheckSumType.MD5);
}