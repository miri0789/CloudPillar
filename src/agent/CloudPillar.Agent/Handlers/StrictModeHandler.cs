using System.Text.RegularExpressions;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    private readonly AppSettings _appSettings;
    private readonly ILoggerHandler _logger;

    public StrictModeHandler(IOptions<AppSettings> appSettings, ILoggerHandler logger)
    {
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ReplaceRootById(TwinActionType actionType, string fileName)
    {
        var pattern = @"\${(.*?)}";

        string replacedString = Regex.Replace(fileName, pattern, match =>
        {
            string key = match.Groups[1].Value;
            ArgumentException.ThrowIfNullOrEmpty(key);

            return GetRootById(key, actionType);
        });
        return replacedString;
    }

    public void CheckSizeStrictMode(TwinActionType actionType, long size, string fileName)
    {
        FileRestrictionDetails zoneRestrictions = GetRestrinctionsByZone(fileName, actionType);
        if (zoneRestrictions.Type == StrictModeAction.Upload.ToString() || !zoneRestrictions.MaxSize.HasValue || zoneRestrictions.MaxSize == 0)
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
            _logger.Info("No allow patterns were found");
            return;
        }

        foreach (var pattern in allowPatterns)
        {
            var regexPattern = ConvertToRegexPattern(pattern.Replace("\\", "/").Trim());
            var isMatch = regexPattern.IsMatch(verbatimFileName);
            if (isMatch)
            {
                _logger.Info($"{fileName} is match to pattern: {pattern}");
                return;
            }

        }
        _logger.Error("Denied by the lack of local allowance");
        throw new FormatException(ResultCode.StrictModePattern.ToString());
    }

    private Regex ConvertToRegexPattern(string pattern)
    {
        if (pattern.EndsWith("/"))
        {
            pattern += "*";
        }
        if (pattern.StartsWith("*/"))
        {
            pattern = ".+/".TrimEnd('/') + pattern.TrimStart('*');
        }
        if (pattern.StartsWith("**/"))
        {
            pattern = pattern.Replace("**/", ".+/.*?/");// ".+/.*?/" + pattern.TrimStart('*');
        }
        string regexPattern =  Regex.Escape(pattern)
                                          .Replace("\\*", ".*")
                                          .Replace("\\?", ".")
                                          .Replace(@"\[\!", "[^")
                                          .Replace(@"\[", "[")
                                          .Replace(@"\]", "]")
                                          .Replace(@"\!", "!")+ "$";
                                          //.Replace("/", "\\/") 

        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }

    private List<FileRestrictionDetails> GetRestrictionsByActionType(TwinActionType actionType)
    {
        _logger.Info($"Get restrictions for {actionType} action");

        switch (actionType)
        {
            case TwinActionType.SingularDownload:
                return _appSettings.FilesRestrictions.Where(x => x.Type == StrictModeAction.Dwonload.ToString()).ToList();
            case TwinActionType.SingularUpload:
                return _appSettings.FilesRestrictions.Where(x => x.Type == StrictModeAction.Upload.ToString()).ToList();
            default: return _appSettings.FilesRestrictions;
        }
    }

    private FileRestrictionDetails GetRestrinctionsByZone(string fileName, TwinActionType actionType)
    {
        List<FileRestrictionDetails> actionRestrictions = GetRestrictionsByActionType(actionType);

        actionRestrictions = actionRestrictions.Where(x => fileName.Contains(x.Root)).ToList();
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