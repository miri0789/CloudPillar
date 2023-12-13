using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    private const string SEPARATOR = "/";
    private const string DOUBLE_SEPARATOR = "\\";
    private readonly StrictModeSettings _strictModeSettings;
    private readonly IMatcherWrapper _matchWrapper;
    private readonly ILoggerHandler _logger;

    public StrictModeHandler(IOptions<StrictModeSettings> strictModeSettings, IMatcherWrapper matchWrapper, ILoggerHandler logger)
    {
        _strictModeSettings = strictModeSettings.Value ?? throw new ArgumentNullException(nameof(strictModeSettings));
        _matchWrapper = matchWrapper ?? throw new ArgumentNullException(nameof(matchWrapper));
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
        if (!_strictModeSettings.StrictMode)
        {
            return;
        }
        FileRestrictionDetails zoneRestrictions = GetRestrinctionsByZone(fileName, actionType);
        if (zoneRestrictions == null || zoneRestrictions.Type == StrictModeAction.Upload.ToString() || !zoneRestrictions.MaxSize.HasValue || zoneRestrictions.MaxSize == 0)
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

        FileRestrictionDetails zoneRestrictions = GetRestrinctionsByZone(verbatimFileName, actionType);

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

        bool isMatch = IsMatch(zoneRestrictions.Root, verbatimFileName, allowPatterns.ToArray());
        if (!isMatch)
        {
            _logger.Error("Denied by the lack of local allowance");
            throw new FormatException(ResultCode.StrictModePattern.ToString());
        }
        _logger.Info($"{verbatimFileName} is match to strict mode allow patterns");
    }

    private List<FileRestrictionDetails> GetRestrictionsByActionType(TwinActionType actionType)
    {
        _logger.Info($"Get restrictions for {actionType} action");

        if (actionType == TwinActionType.SingularDownload)
        {
            return _strictModeSettings.FilesRestrictions.Where(x => x.Type?.ToLower() == StrictModeAction.Download.ToString().ToLower()).ToList();
        }
        else
        {
            return _strictModeSettings.FilesRestrictions.Where(x => x.Type?.ToLower() == StrictModeAction.Upload.ToString().ToLower()).ToList();
        }
    }

    private FileRestrictionDetails GetRestrinctionsByZone(string fileName, TwinActionType actionType)
    {
        List<FileRestrictionDetails> actionRestrictions = GetRestrictionsByActionType(actionType);

        actionRestrictions = actionRestrictions.Where(x => fileName.ToLower().Contains(x.Root.ToLower())).ToList();
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
        ArgumentNullException.ThrowIfNull(id);

        List<FileRestrictionDetails> fileRestrictionDetails = GetRestrictionsByActionType(actionType);
        FileRestrictionDetails restriction = fileRestrictionDetails.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Id) && x.Id.ToLower().Equals(id.ToLower()));

        if (string.IsNullOrWhiteSpace(restriction?.Root))
        {
            _logger.Error($"No Root found for Id: {id}");
            throw new KeyNotFoundException(ResultCode.StrictModeRootNotFound.ToString());
        }

        return restriction.Root;
    }

    private bool IsMatch(string rootPath, string filePath, string[] patterns)
    {
        var result = _matchWrapper.IsMatch(patterns, rootPath.ToLower(), filePath.ToLower());
        var fileMatch = _matchWrapper.DoesFileMatchPattern(result, rootPath, filePath);
        return fileMatch;
    }
}
