using ExCSS;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorIntellisense
{
    internal class ClassNameCompletionSource : IAsyncCompletionSource
    {
        private readonly ImageElement _icon = new ImageElement(KnownMonikers.IntellisenseKeyword.ToImageId(), "icon");

        private readonly StylesheetFileProvider _stylesheetFileProvider;
        
        private CompletionContext cachedCompletionContext;
        private ImmutableDictionary<string, CssCompletion> classNameToCssCompletion;
        
        public ClassNameCompletionSource(
            StylesheetFileProvider stylesheetFileProvider)
        {
            _stylesheetFileProvider = stylesheetFileProvider;
            
            BuildCompletionContextCache();
        }

        private void BuildCompletionContextCache()
        {
            var filePaths = _stylesheetFileProvider.GetFilePaths();
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
            cachedCompletionContext = new CompletionContext(
                allCompletions
                    .Select(x => new CompletionItem(x.ClassName, this, _icon))
                    .ToImmutableArray()
            );

            // Create the dictionary for fast lookup
            classNameToCssCompletion = allCompletions
                .ToImmutableDictionary(x => x.ClassName);

            string ExtractLastClassName(string selector)
            {
                var matches = Regex.Matches(selector, @"\.([a-zA-Z0-9_-]+)");
                return matches.Count > 0
                    ? matches[matches.Count - 1].Groups[1].Value
                    : string.Empty;
            }
        }

        public Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger, 
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return Task.FromResult(cachedCompletionContext);
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            var contains = classNameToCssCompletion.TryGetValue(item.DisplayText, out var cssCompletion);
            if (!contains)
            {
                return Task.FromResult<object>(item.DisplayText);
            }

            return Task.FromResult<object>(
                $"Selector: {cssCompletion.EntireSelector}\n\n```{cssCompletion.FullStyleText}```"
            );
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            // We don't trigger completion when user typed
            if (char.IsNumber(trigger.Character)         // a number
                || (char.IsPunctuation(trigger.Character) && trigger.Character != '"') // punctuation (for some reason '"' counts as punctuation as well...)
                || trigger.Character == '\n'             // new line
                || trigger.Character == '='
                || trigger.Reason == CompletionTriggerReason.Backspace
                || trigger.Reason == CompletionTriggerReason.Deletion)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // Check if we are in the class= context
            var lineStart = triggerLocation.GetContainingLine().Start;
            var spanBeforeCaret = new SnapshotSpan(lineStart, triggerLocation);
            var textBeforeCaret = triggerLocation.Snapshot.GetText(spanBeforeCaret);

            if (textBeforeCaret.IndexOf("class=\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // -> In the class= context
            //var items = Regex.Split(textBeforeCaret, "class=\"", RegexOptions.IgnoreCase);
            //if (items?.Length < 2)
            //{
            //    return CompletionStartData.DoesNotParticipateInCompletion;
            //}
            
            // Without FindTokenSpanAtPosition, we cannot accurately determine the token span
            // Assuming simple handling or returning a broad span instead:
            var tokenSpan = new SnapshotSpan(triggerLocation, 0); // Simplified span, not refined
            return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
        }
    }
}
