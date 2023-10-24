using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    public const string AUTHENTICATION_SAS = "SAS";
    public const string AUTHENTICATION_X509 = "X509";
    public const string UPLOAD_ACTION = "Upload";
    public const string DOWNLOAD_ACTION = "Download";
    private readonly AppSettings _appSettings;
    private readonly ILoggerHandler _logger;

    public StrictModeHandler(IOptions<AppSettings> appSettings, ILoggerHandler logger)
    {
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ReplaceRootById(string fileName, TwinActionType actionType)
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

    public void CheckSizeStrictMode(long size, string fileName, TwinActionType actionType)
    {
        FileRestrictionDetails zoneRestrictions = GetRestrinctionsByZone(fileName, actionType);
        if (zoneRestrictions.Type == UPLOAD_ACTION || zoneRestrictions.MaxSize == 0)
        {
            return;
        }
        if (size > zoneRestrictions.MaxSize)
        {
            _logger.Error("The file size is larger than allowed");
            throw new Exception(ResultCode.StrictModeSize.ToString());
        }
    }

    public void CheckFileAccessPermissions(TwinActionType actionType, string fileName)
    {
        if (!_appSettings.StrictMode)
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
            return;
        }

        foreach (var pattern in allowPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return;
            }
            var regexPattern = ConvertToRegexPattern(pattern.Replace("\\", "/").Trim());
            var isMatch = IsMatch(verbatimFileName, regexPattern);
            if (isMatch)
            {
                _logger.Info($"{fileName} is match to pattern: {pattern}");
                return;
            }

        }
        _logger.Error("Denied by the lack of local allowance");
        throw new Exception(ResultCode.StrictModePattern.ToString());
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

        string regexPattern = "^" + Regex.Escape(pattern)
                                    .Replace("\\*", ".*")
                                    .Replace("\\?", ".")
                                    .Replace(@"\[\!", "[^")
                                    .Replace(@"\[", "[")
                                    .Replace(@"\]", "]")
                                    .Replace(@"\!", "!")
                                    .Replace("/", "\\/")
                                    .Replace("\\.\\*", ".*") + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }

    private List<FileRestrictionDetails> GetRestrictionsByActionType(TwinActionType actionType)
    {
        _logger.Info($"Get restrictions for {actionType} action");

        switch (actionType)
        {
            case TwinActionType.SingularDownload:
                return _appSettings.FilesRestrictions.Where(x => x.Type == DOWNLOAD_ACTION).ToList();
            case TwinActionType.SingularUpload:
            case TwinActionType.PeriodicUpload:
                return _appSettings.FilesRestrictions.Where(x => x.Type == UPLOAD_ACTION).ToList();
            default: return _appSettings.FilesRestrictions;
        }
    }

    private FileRestrictionDetails GetRestrinctionsByZone(string fileName, TwinActionType actionType)
    {
        List<FileRestrictionDetails> actionRestrictions = GetRestrictionsByActionType(actionType);
        
        actionRestrictions = actionRestrictions.Where(x=>fileName.Contains(x.Root)).ToList();
        var bestMatch = actionRestrictions
                   .OrderByDescending(f => fileName.StartsWith(f.Root) ? f.Root.Length : 0)
                   .FirstOrDefault();
        return bestMatch;
    }
 
    private List<string> GetAllowRestrictions(FileRestrictionDetails zoneRestrictions)
    {
        List<string> allowPatterns = zoneRestrictions?.AllowPatterns ?? new List<string>();
        if (_appSettings.GlobalPatterns != null)
        {
            allowPatterns?.AddRange(_appSettings.GlobalPatterns);
        }

        _logger.Info($"{allowPatterns?.Count} allow pattern was found");
        return allowPatterns;
    }

    private string GetRootById(string id, TwinActionType actionType)
    {
        List<FileRestrictionDetails> fileRestrictionDetails = GetRestrictionsByActionType(actionType);
        FileRestrictionDetails restriction = fileRestrictionDetails.FirstOrDefault(x => x.Id.Equals(id));

        if (string.IsNullOrWhiteSpace(restriction?.Root))
        {
            _logger.Error($"No Root found for Id: {id}");
            throw new Exception(ResultCode.StrictModeRootNotFound.ToString());
        }

        return restriction.Root;
    }
}