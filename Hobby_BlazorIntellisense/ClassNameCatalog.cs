using ExCSS;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Core.Imaging;
using System.IO;

namespace Hobby_BlazorIntellisense
{
    internal class ClassNameCatalog
    {
        private readonly ImageElement _globalCompletionIcon = 
            new ImageElement(KnownMonikers.GlobalVariable.ToImageId(), nameof(_globalCompletionIcon));
        
        public ImmutableArray<CompletionItem> CachedGlobalCompletionItems { get; private set; }
        public ImmutableDictionary<string, CssCompletion> ClassNameToCssCompletion { get; private set; }

        public string[] GetFilePaths()
            => new string[]
            {
            };

        public void BuildCompletionContextCache(IAsyncCompletionSource forCompletionSource)
        {
            var filePaths = GetFilePaths();
            var parser = new StylesheetParser(
                tolerateInvalidSelectors: true,
                tolerateInvalidValues: true,
                preserveComments: true,
                preserveDuplicateProperties: false);

            var allCompletions = new List<CssCompletion>();

            foreach (var filePath in filePaths)
            {
                // Load the file and parse the class names
                using(var fileStream = File.OpenRead(filePath))
                {
                    // Parse the stylesheet
                    var stylesheet = parser.Parse(fileStream);

                    // Mapping function
                    Func<StyleRule, CssCompletion> mapping = x => { 
                        var className = ExtractLastClassName(x.Selector.Text);
                        return new CssCompletion()
                        {
                            ClassName = className,
                            EntireSelector = x.Selector.Text,
                            FullStyleText = x.ToCss()
                        };
                    };

                    // Map
                    var cssCompletions = stylesheet.StyleRules
                            .Cast<StyleRule>()
                            .Select(mapping);

                    // Distinct
                    var distinctCssCompletions = cssCompletions
                        .Where(x => !string.IsNullOrEmpty(x.ClassName))
                        .GroupBy(x => x.ClassName)
                        .Select(x => x.First());

                    // Add the mapped completions (distinct by class name)
                    allCompletions.AddRange(distinctCssCompletions);
                }
            }

            // Create the completion context
            CachedGlobalCompletionItems = allCompletions
                    .Select(x => new CompletionItem(x.ClassName, forCompletionSource, _globalCompletionIcon))
                    .ToImmutableArray();

            // Create the dictionary for fast lookup
            ClassNameToCssCompletion = allCompletions
                .ToImmutableDictionary(x => x.ClassName);

            string ExtractLastClassName(string selector)
            {
                var matches = Regex.Matches(selector, @"\.([a-zA-Z0-9_-]+)");
                return matches.Count > 0
                    ? matches[matches.Count - 1].Groups[1].Value
                    : string.Empty;
            }
        }
    }
}
