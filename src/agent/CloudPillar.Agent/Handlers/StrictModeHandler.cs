using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CloudPillar.Agent.Handlers;

public class StrictModeHandler : IStrictModeHandler
{
    private readonly StrictModeSettings _strictModeSettings;
    private readonly IMatcherWrapper _matchWrapper;
    private readonly IFileStreamerWrapper _fileStreamer;
    private readonly ILoggerHandler _logger;

    public StrictModeHandler(IOptions<StrictModeSettings> strictModeSettings, IMatcherWrapper matchWrapper, IFileStreamerWrapper fileStreamer, ILoggerHandler logger)
    {
        _strictModeSettings = strictModeSettings.Value ?? throw new ArgumentNullException(nameof(strictModeSettings));
        _matchWrapper = matchWrapper ?? throw new ArgumentNullException(nameof(matchWrapper));
        _fileStreamer = fileStreamer ?? throw new ArgumentNullException(nameof(fileStreamer));
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
        FileRestrictionDetails? zoneRestrictions = GetRestrinctionsByZone(fileName, actionType);
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

        string verbatimFileName = @$"{replaceSlashString(fileName)}";

        var zone = HandleRestictionWithGlobal(verbatimFileName, actionType);
        if (zone is null)
        {
            return;
        }

        bool isMatch = IsMatch(zone?.Root?.ToLower(), verbatimFileName.ToLower(), zone?.AllowPatterns?.ToArray());
        if (!isMatch)
        {
            _logger.Error("Denied by the lack of local allowance");
            throw new FormatException(ResultCode.StrictModePattern.ToString());
        }
        _logger.Info($"{verbatimFileName} is match to strict mode allow patterns");
    }

    private FileRestrictionDetails? HandleRestictionWithGlobal(string verbatimFileName, TwinActionType actionType)
    {
        var restrictions = GetRestrictionsByActionType(actionType);
        var globalRestrictions = ConvertGlobalPatternsToRestrictions(verbatimFileName);

        if (restrictions is null && globalRestrictions is null)
        {
            _logger.Info("No restrictions were found");
            return null;
        }
        var zoneRestrictions = GetRestrinctionsByZone(verbatimFileName, restrictions);
        var globalZoneRestrictions = GetRestrinctionsByZone(verbatimFileName, globalRestrictions);

        if (zoneRestrictions is null && globalZoneRestrictions is null)
        {
            _logger.Info("No restrictions were found");
            return null;
        }
        List<string> allowPatterns = GetAllowRestrictions(zoneRestrictions);
        List<string> globalAllowPatterns = GetAllowRestrictions(globalZoneRestrictions);
        if (allowPatterns.Count == 0 && globalAllowPatterns.Count == 0)
        {
            _logger.Info("No allow patterns were found");
            return null;
        }
        var zone = new FileRestrictionDetails();
        zone.Root = zoneRestrictions?.Root ?? globalZoneRestrictions?.Root;
        zone.AllowPatterns = allowPatterns?.Concat(globalAllowPatterns).ToList();
        return zone;
    }
    private List<FileRestrictionDetails>? GetRestrictionsByActionType(TwinActionType actionType)
    {
        _logger.Info($"Get restrictions for {actionType} action");

        if (actionType == TwinActionType.SingularDownload)
        {
            return _strictModeSettings.FilesRestrictions?.Where(x => x.Type?.ToLower() == StrictModeAction.Download.ToString().ToLower()).ToList();
        }
        else
        {
            return _strictModeSettings.FilesRestrictions?.Where(x => x.Type?.ToLower() == StrictModeAction.Upload.ToString().ToLower()).ToList();
        }
    }

    private FileRestrictionDetails? GetRestrinctionsByZone(string fileName, TwinActionType actionType)
    {
        var bestMatch = GetRestrictionsByActionType(actionType)?
                                    .Where(x => fileName.ToLower().Contains((x.Root ?? "").ToLower()))
                                    .OrderByDescending(f => fileName.ToLower().StartsWith((f.Root ?? "").ToLower()) ? f.Root?.Length : 0).ToList()
                                    .FirstOrDefault();
        return bestMatch;
    }
    private FileRestrictionDetails? GetRestrinctionsByZone(string fileName, List<FileRestrictionDetails> fileRestrictions)
    {
        var bestMatch = fileRestrictions?
                                    .Where(x => fileName.ToLower().Contains((x.Root ?? "").ToLower()))
                                    .OrderByDescending(f => fileName.ToLower().StartsWith((f.Root ?? "").ToLower()) ? f.Root?.Length : 0).ToList()
                                    .FirstOrDefault();
        return bestMatch;
    }

    private List<string> GetAllowRestrictions(FileRestrictionDetails zoneRestrictions)
    {
        var allowPatterns = zoneRestrictions?.AllowPatterns ?? new List<string>();

        _logger.Info($"{allowPatterns.Count} allow pattern was found");
        var nonEmptyPatterns = allowPatterns.Where(pattern => !string.IsNullOrWhiteSpace(pattern)).ToList();
        return nonEmptyPatterns;
    }

    private string GetRootById(string id, TwinActionType actionType)
    {
        ArgumentNullException.ThrowIfNull(id);

        var fileRestrictionDetails = GetRestrictionsByActionType(actionType);
        var restrictions = fileRestrictionDetails?.Where(x => !string.IsNullOrWhiteSpace(x.Id) && x.Id.ToLower().Equals(id.ToLower()));

        if (restrictions.Count() > 1)
        {
            _logger.Warn($"Multiple restrictions with the same Id: {id}, only first will be used");
        }
        var restriction = restrictions.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(restriction?.Root))
        {
            _logger.Error($"No Root found for Id: {id}");
            throw new KeyNotFoundException(ResultCode.StrictModeRootNotFound.ToString());
        }

        return restriction.Root;
    }

    private bool IsMatch(string rootPath, string filePath, string[] patterns)
    {
        var result = _matchWrapper.IsMatch(patterns, rootPath, filePath);
        var fileMatch = DoesFileMatchPattern(result, rootPath, filePath);
        return fileMatch;
    }

    private bool DoesFileMatchPattern(PatternMatchingResult matchingResult, string rootPath, string filePath)
    {
        return matchingResult?.Files.Any(file => replaceSlashString(filePath)?.ToLower() ==
       replaceSlashString(Path.Combine(rootPath, file.Path))?.ToLower()) ?? false;
    }

    private List<FileRestrictionDetails> ConvertGlobalPatternsToRestrictions(string filePath)
    {
        List<FileRestrictionDetails> zoneRestrictionsList = new List<FileRestrictionDetails>();
        if (_strictModeSettings.GlobalPatterns == null)
        {
            _logger.Info($"Global patterns were not found");
            return zoneRestrictionsList;
        }
        var patterns = _strictModeSettings.GlobalPatterns.ToList();
        var rootPath = string.Empty;
        var pattern = string.Empty;
        patterns.ForEach(p =>
        {
            if (p.Contains(FileConstants.DOUEBLE_ASTERISK))
            {
                var parts = p.Split(FileConstants.DOUEBLE_ASTERISK);
                rootPath = parts[0];
                pattern = $"{FileConstants.DOUEBLE_ASTERISK}{parts[1]}";
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    rootPath = _fileStreamer.GetPathRoot(filePath);
                }
            }
            else
            {
                rootPath = replaceSlashString(_fileStreamer.GetDirectoryName(p));
                pattern = p.Replace($"{rootPath}/", "");
            }

            rootPath = replaceSlashString(rootPath);
            pattern = replaceSlashString(pattern);
            var existsRoot = zoneRestrictionsList.FirstOrDefault(x => x.Root?.ToLower() == rootPath?.ToLower());
            if (existsRoot is null)
            {
                FileRestrictionDetails fileRestrictionDetails = new FileRestrictionDetails
                {
                    Root = rootPath,
                    AllowPatterns = new List<string> { pattern }
                };
                zoneRestrictionsList.Add(fileRestrictionDetails);
            }
            else
            {
                existsRoot.AllowPatterns.Add(pattern);
            }
        });

        return zoneRestrictionsList;
    }

    private string replaceSlashString(string str)
    {
        return str.Replace(FileConstants.DOUBLE_SEPARATOR, FileConstants.SEPARATOR)
            .Replace(FileConstants.DOUBLE_FORWARD_SLASH_SEPARATOR, FileConstants.SEPARATOR);
    }
}
