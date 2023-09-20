using Shared.Enums;

namespace Shared.Entities.Services;

public interface ICheckSumService
{
    Task<string> CalculateCheckSumAsync(Stream stream, CheckSumType checkSumType = CheckSumType.MD5);
}