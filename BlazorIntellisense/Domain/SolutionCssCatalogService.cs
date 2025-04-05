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
        public StylesheetCompletions SolutionGlobalCompletions { get; private set; }
        public event Action OnSolutionGlobalCompletionsChangedEvent;

        ///// <summary>
        ///// Key: file path of the stylesheet, Value: CssClassCompletion
        ///// </summary>
        //public Dictionary<string, StylesheetCompletions> RazorIsolationCompletions { get; private set; } = new Dictionary<string, StylesheetCompletions>(StringComparer.OrdinalIgnoreCase);
        
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
            // This will happen very seldom and doesn't necessarily need to be that fast.

            // Parse
            List<CssClassCompletion> classes = new List<CssClassCompletion>(1000);
            foreach (var filePath in stylesheetPaths)
            {
                var completions = ParseStylesheetToCompletions(filePath);
                classes.AddRange(completions.classes);
            }

            // Set
            SolutionGlobalCompletions = new StylesheetCompletions(
                classes: classes.GroupBy(c => c.ClassName)
                                .Select(g => g.First())
                                .ToImmutableArray()
            );

            OnSolutionGlobalCompletionsChangedEvent?.Invoke();
        }

        private (List<CssClassCompletion> classes, bool _) ParseStylesheetToCompletions(string filePath)
        {
            var stylesheet = s_parser.Parse(File.ReadAllText(filePath));
            var styleRulesArr = stylesheet.StyleRules.ToArray();
            var totalClassCompletions = new List<CssClassCompletion>(styleRulesArr.Length);

            foreach (var styleRule in styleRulesArr)
            {
                var classSelectors = AllSelectorsFrom<ClassSelector>(styleRule).ToArray();

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

        /// <summary>
        /// Manages to get all selectors (like <see cref="ClassSelector"/>)
        /// from a style rule. The method handles scenarios where selectors are deeply nested,
        /// such as StyleRule -> k * ListSelector -> n * CompoundSelector ->  m * ClassSelector.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="styleRule"></param>
        /// <returns></returns>
        private IEnumerable<T> AllSelectorsFrom<T>(IStyleRule styleRule)
            where T : ISelector
        {
            // Direct class selectors (".a")
            var directSelectors = styleRule.Children.OfType<T>();

            // Compound selectors (".a:active")
            var compoundSelectors = styleRule.Children.OfType<CompoundSelector>();
            var fromCompoundSelectors = AllSelectorsFromListSelector<T>(compoundSelectors);

            // List selectors (".a, .b, .c")
            var listSelectors = styleRule.Children.OfType<ListSelector>();
            var fromListSelectors = AllSelectorsFromListSelector<T>(listSelectors);

            //-> No list selectors, so we can ignore them

            var allSelectors = directSelectors
                .Concat(fromCompoundSelectors)
                .Concat(fromListSelectors);

            return allSelectors;
        }

        private IEnumerable<T> AllSelectorsFromListSelector<T>(IEnumerable<Selectors> selector)
            where T : ISelector
        {
            // List selectors (".a, .b, .c")
            var listSelectors = selector
                .SelectMany(s => s)
                .OfType<T>();

            // Compound selectors (".a:active")
            var compoundSelectors = selector
                .SelectMany(s => s)
                .OfType<CompoundSelector>();

            if(!compoundSelectors.Any())
            {
                // No compound selectors, so we can ignore them
                return listSelectors;
            }

            var fromCompoundSelectors = AllSelectorsFromListSelector<T>(compoundSelectors);
            return listSelectors
                .Concat(fromCompoundSelectors);
        }
    }
}