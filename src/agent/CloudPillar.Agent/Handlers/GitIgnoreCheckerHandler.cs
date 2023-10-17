using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using CloudPillar.Agent.Handlers;

public class GitIgnoreCheckerHandler : IGitIgnoreCheckerHandler
{
    private readonly string[] _gitIgnoreRules;

    private List<Regex> ignorePatterns = new List<Regex>();
   
    // # Ignore all .txt files
    // *.txt

    // # Ignore all files in the temp directory
    // /temp/*

    // # Ignore all .csv files in any directory
    // **/*.csv

    // # Ignore a specific file
    // path/to/specificFile.txt

    // # Ignore all files with a specific prefix
    // prefix_*

    // # Ignore all files in a specific directory
    // path/to/directory/
    public GitIgnoreCheckerHandler()//string gitIgnoreFilePath
    {
        // _gitIgnoreRules = File.ReadAllLines(gitIgnoreFilePath)
        //     .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
        //     .Select(line => line.Trim())
        //     .ToArray();
        _gitIgnoreRules = new string[2]{
            "*.txt","/temp/*"
        };
        foreach (var line in _gitIgnoreRules)
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            {
                var regexPattern = ConvertToRegexPattern(line.Trim());
                if (regexPattern != null)
                {
                    ignorePatterns.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
                }
            }
        }
    }

    private string ConvertToRegexPattern(string pattern)
    {
        pattern = pattern.Replace(".", "\\.");
        pattern = pattern.Replace("*", ".*");
        pattern = pattern.Replace("?", ".");
        return pattern;
    }

    public bool IsPathIgnored(string filePath)
    {
        foreach (var pattern in ignorePatterns)
        {
            if (pattern.IsMatch(filePath))
            {
                return true;
            }
        }
        return false;
    }
    public bool IsIgnored(string path)
    {
        foreach (var rule in _gitIgnoreRules)
        {
            var isMatch = IsPathIgnoredByRule(path, rule);
            if (isMatch)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPathIgnoredByRule(string path, string rule)
    {
        var regexPattern = "^" + Regex.Escape(rule).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(path);
    }

    // public GitIgnoreFileChecker(string ignoreFilePath)
    // {
    //     ignorePatterns = new List<Regex>();
    //     LoadIgnorePatterns(ignoreFilePath);
    // }

    // private void LoadIgnorePatterns(string ignoreFilePath)
    // {
    //     if (File.Exists(ignoreFilePath))
    //     {
    //         var lines = File.ReadAllLines(ignoreFilePath);
    //         foreach (var line in lines)
    //         {
    //             if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
    //             {
    //                 var regexPattern = ConvertToRegexPattern(line.Trim());
    //                 if (regexPattern != null)
    //                 {
    //                     ignorePatterns.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
    //                 }
    //             }
    //         }
    //     }
    //     else
    //     {
    //         Console.WriteLine("Ignore file not found.");
    //     }
    // }
}
