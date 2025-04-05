using System;

namespace Hobby_BlazorIntellisense.Infrastructure
{
    public class PathExtensions
    {
        public static string GetRelativePath(string fullPath, string basePath) 
        {
            if(!basePath.EndsWith("\\"))
            {
                basePath += "\\";
            }

            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(basePath))
            {
                throw new ArgumentException("Full path and base path cannot be null or empty.");
            }

            Uri fullPathUri = new Uri(fullPath);
            Uri basePathUri = new Uri(basePath);
            
            if (basePathUri.Scheme != fullPathUri.Scheme)
            {
                return fullPath; // Different schemes (e.g., file vs. http), return full path
            }
            
            Uri relativeUri = basePathUri.MakeRelativeUri(fullPathUri);

            return Uri.UnescapeDataString(relativeUri.ToString());
        }
    }
}