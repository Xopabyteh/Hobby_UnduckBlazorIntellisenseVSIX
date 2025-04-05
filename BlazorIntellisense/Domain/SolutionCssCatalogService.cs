using ExCSS;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Hobby_BlazorIntellisense.Domain
{
    /// <summary>
    /// Control the catalog accoarding to the current solution.
    /// </summary>
    public partial class SolutionCssCatalogService
    {
        public static SolutionCssCatalogService Instance { get; private set; } = new SolutionCssCatalogService();

        /// <summary>
        /// Completions that are to be used in the entire solution.
        /// </summary>
        public StylesheetCompletions SolutionGlobalCompletions { get; private set; } = new StylesheetCompletions();

        /// <summary>
        /// Key: file path of the stylesheet, Value: CssClassCompletion
        /// </summary>
        public Dictionary<string, StylesheetCompletions> RazorIsolationCompletions { get; private set; } = new Dictionary<string, StylesheetCompletions>(StringComparer.OrdinalIgnoreCase);
        
        private static readonly StylesheetParser s_parser = new StylesheetParser(
            tolerateInvalidSelectors: true,
            tolerateInvalidValues: true,
            preserveComments: true,
            preserveDuplicateProperties: false);
        
        public SolutionCssCatalogService()
        {
        }

        public void BuildSolutionGlobalCache(string[] stylesheetPaths)
        {
            // This will happen only once and doesn't necessarily need to be that fast.

            // Prepare
            SolutionGlobalCompletions = new StylesheetCompletions();
            
            // Parse
            List<CssClassCompletion> classes = new List<CssClassCompletion>(1000);
            foreach (var filePath in stylesheetPaths)
            {
                var completions = ParseStylesheetToCompletions(filePath);
                classes.AddRange(completions.classes);
            }

            // Set
            SolutionGlobalCompletions.Classes = classes.GroupBy(c => c.ClassName)
                .Select(g => g.First())
                .ToImmutableArray();
        }

        private (List<CssClassCompletion> classes, bool _) ParseStylesheetToCompletions(string filePath)
        {
            var stylesheet = s_parser.Parse(File.ReadAllText(filePath));
            var styleRulesArr = stylesheet.StyleRules.ToArray();
            var totalClassCompletions = new List<CssClassCompletion>(styleRulesArr.Length);

            foreach (var styleRule in styleRulesArr)
            {
                var classSelectors = styleRule.Children.OfType<ClassSelector>();
                var classCompletions = classSelectors.Select(s => new CssClassCompletion()
                {
                    ClassName = s.Class,
                    EntireSelector = s.Text,
                    FullStyleText = styleRule.StylesheetText.Text,
                    StylesheetFilePath = filePath,
                    StylesheetPositionStart = styleRule.StylesheetText.Range.Start
                });

                totalClassCompletions.AddRange(classCompletions);
            }

            return (totalClassCompletions, false);
        }
    }
}