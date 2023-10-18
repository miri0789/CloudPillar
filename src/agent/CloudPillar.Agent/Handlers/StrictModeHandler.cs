using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    public const string AUTHENTICATION_SAS = "SAS";
    public const string AUTHENTICATION_X509 = "X509";
    public const string UPLOAD_ACTION = "Upload";
    public const string DOWNLOAD_ACTION = "Download";
    private readonly AppSettings _appSettings;

    public StrictModeHandler(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }
    public void CheckAuthentucationMethodValue()
    {
        if (_appSettings.StrictMode == false) { return; }

        if (!_appSettings.PermanentAuthentucationMethods.Equals(AUTHENTICATION_X509))
        {
            throw new InvalidOperationException($"PermanentAuthentucationMethods value in appSettings.json must be X509, The value {_appSettings.PermanentAuthentucationMethods} is not valid");
        }
        if (!_appSettings.ProvisionalAuthentucationMethods.Equals(AUTHENTICATION_SAS))
        {
            throw new InvalidOperationException($"ProvisionalAuthentucationMethods value in appSettings.json must be SAS, The value {_appSettings.ProvisionalAuthentucationMethods} is not valid");
        }
    }

    public void CheckRestrictedZones(TwinActionType actionType, string fileName)
    {
        var verbatimFileName = @$"{fileName.Replace("\\", "/")}";
        var list = _appSettings.FilesRestrictions;
        // KeyValuePair<string, FileRestrictionDetails> fileRestrictions = GetRestrinctionsByZone(verbatimFileName);
        // List<string> allowPatterns = fileRestrictions.Value.AllowPatterns;
        // foreach (var pattern in allowPatterns)
        // {
        //     if (!string.IsNullOrWhiteSpace(pattern) && !pattern.StartsWith("#"))
        //     {
        //         var regexPattern = ConvertToRegexPattern(pattern.Replace("\\", "/").Trim());
        //         var isMatch = IsMatch(verbatimFileName, regexPattern);
        //         if (isMatch)
        //         {
        //             return;
        //         }
        //     }
        //     throw new Exception("Denied by the lack of local allowance");
        // }
    }

    public bool IsMatch(string filePath, Regex pattern)
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
    // private Dictionary<string, FileRestrictionDetails> GetRestrinctionsByAction(TwinActionType actionType)
    // {
    // switch (actionType)
    // {
    //     case TwinActionType.SingularDownload:
    //         return (Dictionary<string, FileRestrictionDetails>)_appSettings.FilesRestrictions.Values.Where(x => x.Type == DOWNLOAD_ACTION);
    //     case TwinActionType.SingularUpload:
    //     case TwinActionType.PeriodicUpload:
    //         return (Dictionary<string, FileRestrictionDetails>)_appSettings.FilesRestrictions.Values.Where(x => x.Type == UPLOAD_ACTION);
    //     default: return _appSettings.FilesRestrictions;
    // }
    // }
    // private KeyValuePair<string, FileRestrictionDetails> GetRestrinctionsByZone(string fileName)
    // {
    //     return _appSettings.FilesRestrictions.FirstOrDefault(x => fileName.Contains(x.Value.Root));
    // }
}