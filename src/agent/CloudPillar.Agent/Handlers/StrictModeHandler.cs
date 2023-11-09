using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    private readonly StrictModeSettings _strictModeSettings;
    private readonly ILoggerHandler _logger;

    public StrictModeHandler(IOptions<StrictModeSettings> strictModeSettings, ILoggerHandler logger)
    {
        _strictModeSettings = strictModeSettings.Value ?? throw new ArgumentNullException(nameof(strictModeSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ReplaceRootById(TwinActionType actionType, string fileName)
    {
        var pattern = @"\${(.*?)}";

        string replacedString = Regex.Replace(fileName, pattern, match =>
        {
            string key = match.Groups[1].Value;
            ArgumentNullException.ThrowIfNullOrEmpty(key);

            return GetRootById(key, actionType);
        });
        return replacedString;
    }

    public void CheckSizeStrictMode(TwinActionType actionType, long size, string fileName)
    {
        FileRestrictionDetails zoneRestrictions = GetRestrinctionsByZone(fileName, actionType);
        if (zoneRestrictions.Type == StrictModeAction.Upload.ToString() || zoneRestrictions.MaxSize == 0)
        {
            return;
        }
        if (size > zoneRestrictions.MaxSize)
        {
            _logger.Error("The file size is larger than allowed");
            throw new ArgumentException(ResultCode.StrictModeSize.ToString());
        }
    }

    public void CheckFileAccessPermissions(TwinActionType actionType, string fileName)
    {
        if (!_strictModeSettings.StrictMode)
        {
            return;
        }

        string verbatimFileName = @$"{fileName.Replace("\\", "/")}";

        FileRestrictionDetails zoneRestrictions = GetRestrinctionsByZone(fileName, actionType);

        if (zoneRestrictions == null)
        {
            return;
        }

        List<string> allowPatterns = GetAllowRestrictions(zoneRestrictions);
        if (allowPatterns?.Count == 0)
        {
            _logger.Info("No allow patterns were found");
            return;
        }

        foreach (var pattern in allowPatterns)
        {
            var regexPattern = ConvertToRegexPattern(pattern.Replace("\\", "/").Trim());
            var isMatch = IsMatch(verbatimFileName, regexPattern);
            if (isMatch)
            {
                _logger.Info($"{fileName} is match to pattern: {pattern}");
                return;
            }

        }
        _logger.Error("Denied by the lack of local allowance");
        throw new FormatException(ResultCode.StrictModePattern.ToString());
    }

    private bool IsMatch(string filePath, Regex pattern)
    {
        return pattern.IsMatch(filePath);
    }

    private Regex ConvertToRegexPattern(string pattern)
    {
        if (pattern.EndsWith("/"))
        {
            pattern += "*";
        }
        string regexPattern = Regex.Escape(pattern)
                                          .Replace("\\*", ".*")
                                          .Replace("\\?", ".")
                                          .Replace(@"\[\!", "[^")
                                          .Replace(@"\[", "[")
                                          .Replace(@"\]", "]")
                                          .Replace(@"\!", "!")
                                          .Replace("/", "\\/") + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }

    private List<FileRestrictionDetails> GetRestrictionsByActionType(TwinActionType actionType)
    {
        _logger.Info($"Get restrictions for {actionType} action");

        if (actionType == TwinActionType.SingularDownload)
        {
            return _strictModeSettings.FilesRestrictions.Where(x => x.Type == StrictModeAction.Download.ToString()).ToList();
        }
        else
        {
            return _strictModeSettings.FilesRestrictions.Where(x => x.Type == StrictModeAction.Upload.ToString()).ToList();
        }
    }

    private FileRestrictionDetails GetRestrinctionsByZone(string fileName, TwinActionType actionType)
    {
        List<FileRestrictionDetails> actionRestrictions = GetRestrictionsByActionType(actionType);

        actionRestrictions = actionRestrictions.Where(x => fileName.Contains(x.Root)).ToList();
        var bestMatch = actionRestrictions
                   .OrderByDescending(f => fileName.ToLower().StartsWith(f.Root.ToLower()) ? f.Root.Length : 0)
                   .FirstOrDefault();
        return bestMatch;
    }

    private List<string> GetAllowRestrictions(FileRestrictionDetails zoneRestrictions)
    {
        List<string> allowPatterns = zoneRestrictions?.AllowPatterns ?? new List<string>();
        if (_strictModeSettings.GlobalPatterns != null)
        {
            allowPatterns?.AddRange(_strictModeSettings.GlobalPatterns);
        }

        _logger.Info($"{allowPatterns?.Count} allow pattern was found");
        var nonEmptyPatterns = allowPatterns.Where(pattern => !string.IsNullOrWhiteSpace(pattern)).ToList();
        return nonEmptyPatterns;
    }

    private string GetRootById(string id, TwinActionType actionType)
    {
        List<FileRestrictionDetails> fileRestrictionDetails = GetRestrictionsByActionType(actionType);
        FileRestrictionDetails restriction = fileRestrictionDetails.FirstOrDefault(x => x.Id.Equals(id));

        if (string.IsNullOrWhiteSpace(restriction?.Root))
        {
            _logger.Error($"No Root found for Id: {id}");
            throw new KeyNotFoundException(ResultCode.StrictModeRootNotFound.ToString());
        }

        return restriction.Root;
    }
}