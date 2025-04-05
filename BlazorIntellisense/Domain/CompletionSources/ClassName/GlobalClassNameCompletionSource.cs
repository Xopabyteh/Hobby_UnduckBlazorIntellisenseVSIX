using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hobby_BlazorIntellisense.Domain
{
    /// <summary>
    /// Provides global solution scoped intellisense for class names
    /// </summary>
    public class GlobalClassNameCompletionSource : IAsyncCompletionSource
    {
        private CompletionContext _cachedCompletionContext;

        public GlobalClassNameCompletionSource()
        {
            SolutionCssCatalogService.Instance.OnSolutionGlobalCompletionsChangedEvent += BuildCompletionCache;
            BuildCompletionCache();
        }

        private void BuildCompletionCache()
        {
            var globalCompletions = SolutionCssCatalogService.Instance.SolutionGlobalCompletions;
            _cachedCompletionContext = new CompletionContext(
                globalCompletions.Classes.Select(c => new CompletionItem(
                    c.ClassName,
                    this,
                    GlobalClassNameCompletionSourceProvider.GlobalCompletionIcon
                )).ToImmutableArray()
            );
        }
        public Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger, 
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return Task.FromResult(_cachedCompletionContext);
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            // DisplayText = ClassName
            var contains = SolutionCssCatalogService.Instance.SolutionGlobalCompletions.ClassNameToCompletion.TryGetValue(item.DisplayText, out var completion);
            if(!contains)
            {
                return Task.FromResult<object>(null);
            }

            return Task.FromResult<object>(new ContainerElement(ContainerElementStyle.Stacked, new[]
            {
                new ClassifiedTextElement(new[]
                {
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, completion.ClassName),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "->\n"),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, completion.FullStyleText)
                })
            }));
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

            if(!IsInsideClassAttribute(triggerLocation))
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // -> In the class= context
            var tokenSpan = FindClassSelectorSpan(triggerLocation);
            return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
        }

        private static bool IsInsideClassAttribute(SnapshotPoint triggerLocation)
        {
            const int maxLinesToCheck = 5;

            return IsToRightOfClass() && IsToLeftOffQuotation();
        
            bool IsToRightOfClass()
            {
                var snapshot = triggerLocation.Snapshot;
                var caretLineNumber = triggerLocation.GetContainingLine().LineNumber;

                int linesChecked = 0;
                int caretOffset = triggerLocation.Position;

                while (caretLineNumber >= 0 && linesChecked < maxLinesToCheck)
                {
                    var line = snapshot.GetLineFromLineNumber(caretLineNumber);
                    string lineText = line.GetText();

                    // Only look at the part of the text before the caret on the caret line
                    if (caretLineNumber == triggerLocation.GetContainingLine().LineNumber)
                    {
                        int relativeCaret = triggerLocation.Position - line.Start.Position;
                        lineText = lineText.Substring(0, Math.Min(relativeCaret, lineText.Length));
                    }

                    int classIndex = lineText.LastIndexOf("class=\"", StringComparison.OrdinalIgnoreCase);
                    if (classIndex >= 0)
                    {
                        return true;
                    }

                    // Early out if we hit a closing or opening tag boundary — we’re outside the element
                    if (lineText.Contains('>') || lineText.Contains('<'))
                        break;

                    caretLineNumber--;
                    linesChecked++;
                }

                return false;
            }

            bool IsToLeftOffQuotation()
            {
                var snapshot = triggerLocation.Snapshot;
                var caretLineNumber = triggerLocation.GetContainingLine().LineNumber;
                
                int linesChecked = 0;
                int caretOffset = triggerLocation.Position;
                    
                // Check to right and down from trigger location
                while(caretLineNumber < snapshot.LineCount && linesChecked < maxLinesToCheck)
                {
                    var line = snapshot.GetLineFromLineNumber(caretLineNumber);
                    string lineText = line.GetText();

                    // Only look at the part of the text before the caret on the caret line
                    if (caretLineNumber == triggerLocation.GetContainingLine().LineNumber)
                    {
                        int relativeCaret = triggerLocation.Position - line.Start.Position;
                        lineText = lineText.Substring(relativeCaret);
                    }

                    int quoteIndex = lineText.IndexOf('"');
                    if (quoteIndex >= 0)
                    {
                        return true;
                    }
                    // Early out if we hit a closing or opening tag boundary — we’re outside the element
                    if (lineText.Contains('>') || lineText.Contains('<'))
                        break;
                    caretLineNumber++;
                    linesChecked++;
                }

                return false;
            }
        }
        
        private static SnapshotSpan FindClassSelectorSpan(SnapshotPoint triggerLocation)
        {
            var snapshot = triggerLocation.Snapshot;
            int position = triggerLocation.Position;

            if (snapshot.Length == 0 || position < 0 || position > snapshot.Length)
                return new SnapshotSpan(triggerLocation, 0);

            bool IsWordChar(char c) => !char.IsWhiteSpace(c) && c != '"';

            int start = position;
            while (start > 0 && IsWordChar(snapshot[start - 1]))
            {
                start--;
            }

            int end = position;
            while (end < snapshot.Length && IsWordChar(snapshot[end]))
            {
                end++;
            }

            if (start == end)
            {
                // No word found, return empty span that will grow as user types
                return new SnapshotSpan(triggerLocation, 0);
            }

            return new SnapshotSpan(snapshot, start, end - start);
        }
    }
}
