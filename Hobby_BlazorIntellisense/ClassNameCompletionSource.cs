using Hobby_BlazorIntellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorIntellisense
{
    internal class ClassNameCompletionSource : IAsyncCompletionSource
    {
        private readonly ClassNameCatalog _classNameCatalog;
        public ClassNameCompletionSource(ClassNameCatalog classNameCatalog)
        {
            _classNameCatalog = classNameCatalog;
        
            _classNameCatalog.BuildCompletionContextCache(forCompletionSource: this);
        }

        public async Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger, 
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return new CompletionContext(
                _classNameCatalog.CachedGlobalCompletionItems
            ); 
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            var contains = _classNameCatalog.ClassNameToCssCompletion.TryGetValue(item.DisplayText, out var cssCompletion);
            if (!contains)
            {
                return Task.FromResult<object>(item.DisplayText);
            }

            var header = new ClassifiedTextElement(
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "Selector: ", ClassifiedTextRunStyle.Bold),
                new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, cssCompletion.EntireSelector)
            );

            // Format the CSS style text like code
            var styleText = new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.MarkupAttributeValue,
                    cssCompletion.FullStyleText),
            );

            return Task.FromResult<object>(new ContainerElement(
                ContainerElementStyle.Stacked,
                header,
                styleText
            ));
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
