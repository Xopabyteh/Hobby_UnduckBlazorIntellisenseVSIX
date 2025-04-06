using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace BlazorIntellisense.Infrastructure
{
    public static class OmmitiveFileSearch
    {
        private const int k_MaxSearchDepth = 10; // I hope noone has holy moly deep solutions

        public static IEnumerable<string> GetFilesExcludingDirs(
            string rootDirectory,
            string searchPattern,
            string[] excludedDirs,
            string excludedExtension = null,
            int currentDepth = 0)
        {
            if (currentDepth > k_MaxSearchDepth)
            {
                yield break; // Prevent infinite recursion
            }

            var filesInDirectory = excludedExtension is null
                ? Directory.EnumerateFiles(rootDirectory, searchPattern, SearchOption.TopDirectoryOnly)
                : Directory.EnumerateFiles(rootDirectory, searchPattern, SearchOption.TopDirectoryOnly)
                    .Where(file => !file.EndsWith(excludedExtension, StringComparison.OrdinalIgnoreCase));
            
            foreach (var file in filesInDirectory)
            {
                yield return file;
            }

            foreach (var directory in Directory.EnumerateDirectories(rootDirectory))
            {
                if (excludedDirs.Contains(Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var file in GetFilesExcludingDirs(directory, searchPattern, excludedDirs, excludedExtension, currentDepth + 1))
                {
                    yield return file;
                }
            }
        }
    }
}