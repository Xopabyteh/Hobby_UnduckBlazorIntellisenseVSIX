using System;

namespace Hobby_BlazorIntellisense.Domain.Settings
{
    public class SolutionCompletionSettings
    {
        /// <summary>
        /// Relative paths to global stylesheets that should be whitelisted for completion.
        /// Relative to directory where solution is placed.
        /// </summary>
        public string[] WhitelistGlobalStylesheetRelativePaths { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Relative paths to containing directories of global stylesheets that should
        /// be whitelisted for completion.
        /// Relative to directory where solution is placed.
        /// </summary>
        public string[] WhitelistGlobalStylesheetDirectoryRelativePaths { get; set; } = Array.Empty<string>();
    }
}