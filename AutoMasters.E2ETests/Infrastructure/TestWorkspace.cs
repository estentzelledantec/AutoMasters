using System;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace AutoMasters.E2ETests.Infrastructure;

public static class TestWorkspace
{
    private static string? _workspaceRoot;

    public static string WorkspaceRoot
    {
        get
        {
            if (_workspaceRoot != null) return _workspaceRoot;

            string current = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                if (File.Exists(Path.Combine(current, "AutoMasters.slnx")) ||
                    File.Exists(Path.Combine(current, "ORIGINAL_REQUEST.md")) ||
                    Directory.Exists(Path.Combine(current, "AutoMasters.E2ETests")))
                {
                    _workspaceRoot = current;
                    return _workspaceRoot;
                }

                DirectoryInfo? parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            // Fallback default
            _workspaceRoot = @"c:\Users\stent\source\repos\AutoMasters";
            return _workspaceRoot;
        }
    }

    public static string AutoMastersDir => Path.Combine(WorkspaceRoot, "AutoMasters");
    public static string TestsDir => Path.Combine(WorkspaceRoot, "AutoMasters.Tests");
    public static string CarsDatabasePath => Path.Combine(WorkspaceRoot, "cars_database.json");
    public static string SolutionPath => Path.Combine(WorkspaceRoot, "AutoMasters.slnx");

    public static bool FileExists(string relativePath)
    {
        return File.Exists(Path.Combine(WorkspaceRoot, relativePath));
    }

    public static bool DirectoryExists(string relativePath)
    {
        return Directory.Exists(Path.Combine(WorkspaceRoot, relativePath));
    }

    public static string ReadAllText(string relativePath)
    {
        string fullPath = Path.Combine(WorkspaceRoot, relativePath);
        return File.ReadAllText(fullPath);
    }

    public static bool ValidateRegionDiscipline(string codeContent, out string failureReason)
    {
        failureReason = string.Empty;
        bool hasAttributs = codeContent.Contains("#region Attributs");
        bool hasConstructeurs = codeContent.Contains("#region Constructeurs");
        bool hasEndRegion = codeContent.Contains("#endregion");

        if (!hasEndRegion)
        {
            failureReason = "Missing #endregion tags";
            return false;
        }

        if (!hasAttributs && !hasConstructeurs)
        {
            failureReason = "Missing either #region Attributs or #region Constructeurs";
            return false;
        }

        return true;
    }

    public static bool ValidateCodeBehindPurity(string codeContent, out string violationDetails)
    {
        violationDetails = string.Empty;

        // Code-behind should NOT contain HttpClient, ServiceApi calls, or market business logic
        string[] forbiddenPatterns = new[]
        {
            "HttpClient",
            "ServiceAPI.Instance",
            "ServiceApi.Instance",
            "new ServiceAPI",
            "new ServiceApi",
            "RecupererCatalogueAsync",
            "AcheterOffreAsync",
            "OuvrirBoosterAsync",
            "Task.Run",
            "JsonConvert.Deserialize"
        };

        foreach (var pattern in forbiddenPatterns)
        {
            if (codeContent.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                violationDetails = $"Forbidden business logic / HTTP call found: '{pattern}'";
                return false;
            }
        }

        return true;
    }

    public static bool ValidateXml(string xmlContent, out string error)
    {
        try
        {
            XDocument.Parse(xmlContent);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
