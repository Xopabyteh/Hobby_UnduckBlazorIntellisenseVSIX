using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace Hobby_BlazorIntellisense.Infrastructure
{
    public static class OmmitiveFileSearch
    {
        public static IEnumerable<string> GetFilesExcludingDirs(
            string rootDirectory,
            string searchPattern,
            string excludedExtension,
            string[] excludedDirs)
        {
            var filesInDirectory = Directory.EnumerateFiles(rootDirectory, searchPattern, SearchOption.TopDirectoryOnly)
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

                foreach (var file in GetFilesExcludingDirs(directory, searchPattern, excludedExtension, excludedDirs))
                {
                    yield return file;
                }
            }
        }
    }
}