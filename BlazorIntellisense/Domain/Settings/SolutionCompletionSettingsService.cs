using BlazorIntellisense.Infrastructure;
using BlazorIntellisense.Infrastructure;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlazorIntellisense.Domain.Settings
{
    public class SolutionCompletionSettingsService
    {
        public static SolutionCompletionSettingsService Instance { get; private set; } = new SolutionCompletionSettingsService();

        public SolutionCompletionSettings Settings { get; private set; }
        public string SolutionDirectory { get; private set; }
        
        private const string SettingsFileName = "BlazorIntellisenseExtensionSettings.json.user";

        /// <summary>
        /// Tries to load the settings for the solution.
        /// If they don't exist but we think they should, they get created.
        /// If they don't exist and we don't think they should, they are not created -> null
        /// <br/>
        /// </summary>
        /// <returns></returns>
        public SolutionCompletionSettings EnsureLoadSettingsForSolution(string slnFilePath)
        {
            // Reset settings
            Settings = null;
            SolutionDirectory = null;

            // Load the settings from the solution file (or create it if it doesn't exist)
            // Only create them, if we find a css file in the solution
            
            var solutionDirectory = Path.GetDirectoryName(slnFilePath);
            SolutionDirectory = solutionDirectory;

            var settingsFilePath = Path.Combine(solutionDirectory, SettingsFileName);

            // If already exists, load the settings
            if (File.Exists(settingsFilePath))
            {
                var json = File.ReadAllText(settingsFilePath);
                Settings = JsonConvert.DeserializeObject<SolutionCompletionSettings>(json);
                return Settings;
            }

            // -> Don't exist yet

            // Find first css file in the solution
            var firstCssFile = OmmitiveFileSearch.GetFilesExcludingDirs(
                solutionDirectory,
                "*.css",
                excludedExtension: ".razor.css",
                excludedDirs: new string[] { "bin", "obj" }
            )
                .FirstOrDefault();

            if(string.IsNullOrEmpty(firstCssFile))
            {   
                // No css file found, return empty settings
                return null;
            }

            var relativePath = PathExtensions.GetRelativePath(fullPath: firstCssFile, basePath: solutionDirectory);
            var defaultSettings = new SolutionCompletionSettings()
            {
                WhitelistGlobalStylesheetRelativePaths = new string[] 
                {
                    relativePath
                },
                WhitelistGlobalStylesheetDirectoryRelativePaths = new string[]
                { 
                
                },
            };

            // Save the settings to the solution file
            SaveSettingsForSolution(solutionDirectory, defaultSettings);

            Settings = defaultSettings;
            return Settings;
        }

        private void SaveSettingsForSolution(string solutionDirectory, SolutionCompletionSettings settings)
        {
            var settingsFilePath = Path.Combine(solutionDirectory, SettingsFileName);
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

            File.WriteAllText(settingsFilePath, json);
        }

        public IEnumerable<string> WhitelistGlobalStylesheetPaths =>
            Settings.WhitelistGlobalStylesheetRelativePaths
                .Select(relativePath => Path.Combine(SolutionDirectory, relativePath))
                .ToArray();

        public IEnumerable<string> WhitelistGlobalStylesheetDirectoryPaths =>
            Settings.WhitelistGlobalStylesheetDirectoryRelativePaths
                .Select(relativePath => Path.Combine(SolutionDirectory, relativePath))
                .ToArray();
    }
}