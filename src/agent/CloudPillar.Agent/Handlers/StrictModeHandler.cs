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
    public void CheckAuthentucationMethodValue()
    {
        if (_appSettings.StrictMode == false) { return; }

        if (!_appSettings.PermanentAuthentucationMethods.Equals(AUTHENTICATION_X509))
        {
            HandleError($"PermanentAuthentucationMethods value in appSettings.json must be X509, The value {_appSettings.PermanentAuthentucationMethods} is not valid");
        }
        if (!_appSettings.ProvisionalAuthentucationMethods.Equals(AUTHENTICATION_SAS))
        {
            HandleError($"ProvisionalAuthentucationMethods value in appSettings.json must be SAS, The value {_appSettings.ProvisionalAuthentucationMethods} is not valid");
        }
    }

    public void CheckRestrictedZones(TwinActionType actionType, string fileName)
    {
        if (!_appSettings.StrictMode)
        {
            return;
        }

        string verbatimFileName = @$"{fileName.Replace("\\", "/")}";

        List<string> allowPatterns = GetAllowRestrictions(actionType, verbatimFileName);
        if (allowPatterns.Count == 0)
        {
            return;
        }

        foreach (var pattern in allowPatterns)
        {
            if (!string.IsNullOrWhiteSpace(pattern) && !pattern.StartsWith("#"))
            {
                var regexPattern = ConvertToRegexPattern(pattern.Replace("\\", "/").Trim());
                var isMatch = IsMatch(verbatimFileName, regexPattern);
                if (isMatch)
                {
                    _logger.Info($"{fileName} is match to pattern: {pattern}");
                    return;
                }
            }
        }
        HandleError("Denied by the lack of local allowance");
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
                                 .Replace(@"\*", ".*")
                                 .Replace(@"\?", ".")
                                 .Replace(@"\[\!", "[^")
                                 .Replace(@"\[", "[") + "$";


        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }

    private List<FileRestrictionDetails> GetRestrinctionsByAction(TwinActionType actionType)
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

    private FileRestrictionDetails GetRestrinctionsByZone(List<FileRestrictionDetails> restrictions, string fileName)
    {
        return restrictions.FirstOrDefault(x => fileName.Contains(x.Root));
    }

    private List<string> GetAllowRestrictions(TwinActionType actionType, string fileName)
    {
        List<string> allowPatterns = new List<string>();
        List<FileRestrictionDetails> actionRestrictions = GetRestrinctionsByAction(actionType);
        FileRestrictionDetails zoneRestrictions = GetRestrinctionsByZone(actionRestrictions, fileName);
        if (zoneRestrictions != null)
        {
            allowPatterns = zoneRestrictions.AllowPatterns;
            if (_appSettings.GlobalPatterns != null)
            {
                allowPatterns.AddRange(_appSettings.GlobalPatterns);
            }
        }
        _logger.Info($"{allowPatterns.Count} allow pattern was found");
        return allowPatterns;
    }

    private void HandleError(string message)
    {
        _logger.Error(message);
        throw new Exception(message);
    }
}