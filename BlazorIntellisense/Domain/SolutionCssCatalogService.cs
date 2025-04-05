using ExCSS;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hobby_BlazorIntellisense.Domain
{
    /// <summary>
    /// Control the catalog accoarding to the current solution.
    /// </summary>
    public sealed class SolutionCssCatalogService
    {
        public static SolutionCssCatalogService Instance { get; private set; } = new SolutionCssCatalogService();

        /// <summary>
        /// Completions that are to be used in the entire solution.
        /// </summary>
        public StylesheetCompletions SolutionGlobalCompletions { get; private set; }
        public event Action OnSolutionGlobalCompletionsChangedEvent;

        /// <summary>
        /// Key: file path of the stylesheet, Value: CssClassCompletion
        /// </summary>
        public ConcurrentDictionary<string, StylesheetCompletions> RazorIsolationCompletions { get; private set; } 
            = new ConcurrentDictionary<string, StylesheetCompletions>(StringComparer.OrdinalIgnoreCase);

        private static readonly StylesheetParser s_parser = new StylesheetParser(
            tolerateInvalidSelectors: true,
            tolerateInvalidValues: true,
            preserveComments: true,
            preserveDuplicateProperties: false);

        /// <summary>
        /// Builds (or rebuilds) the global cache of the solution.
        /// </summary>
        /// <param name="stylesheetPaths"></param>
        public void BuildSolutionGlobalCache(IEnumerable<string> stylesheetPaths)
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
                classes: classes
                    .DistinctBy(c => c.ClassName, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray()
            );

            OnSolutionGlobalCompletionsChangedEvent?.Invoke();
        }

        public void BuildIsolatedStylesheetsCaches(IEnumerable<string> isolatedStylesheetPaths)
        {
            Parallel.ForEach(isolatedStylesheetPaths, filePath =>
            {
                BuildIsolatedStylesheetCache(filePath);
            });
        }

        public void BuildIsolatedStylesheetCache(string filePath)
        {
            // Parse
            var completions = ParseStylesheetToCompletions(filePath);
         
            // Set
            RazorIsolationCompletions[filePath] = new StylesheetCompletions(
                classes: completions.classes
                    .DistinctBy(c => c.ClassName, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray()
            );
        }

        # region Stylesheet parsing
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
                    StylesheetFileName = Path.GetFileName(filePath),
                    StylesheetPositionStart = styleRule.StylesheetText.Range.Start
                });

                totalClassCompletions.AddRange(classCompletions);
            }

            return (totalClassCompletions, false);
        }

        // Todo: doesn't extract btn-link from ".top-row ::deep a:hover, .top-row ::deep .btn-link:hover" 👀
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
        # endregion
    }
}