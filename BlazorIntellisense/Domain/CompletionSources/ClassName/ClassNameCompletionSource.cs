using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hobby_BlazorIntellisense.Domain
{
    public class ClassNameCompletionSource : IAsyncCompletionSource
    {
        public async Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger, 
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            var globalCompletions = SolutionCssCatalogService.Instance.SolutionGlobalCompletions;
            return new CompletionContext(
                globalCompletions.Classes.Select(c => new CompletionItem(
                    c.ClassName,
                    this,
                    ClassNameCompletionSourceProvider.GlobalCompletionIcon
                )).ToImmutableArray()
            );
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            return Task.FromResult<object>(null);
            //var contains = _classNameCatalog.ClassNameToCssCompletion.TryGetValue(item.DisplayText, out var cssCompletion);
            //if (!contains)
            //{
            //    return Task.FromResult<object>(item.DisplayText);
            //}

            //// Format the CSS style text like code
            //var styleText = new ClassifiedTextElement(
            //    new ClassifiedTextRun(
            //        PredefinedClassificationTypeNames.MarkupAttributeValue,
            //        cssCompletion.FullStyleText)
            //);

            //return Task.FromResult<object>(new ContainerElement(
            //    ContainerElementStyle.Wrapped,
            //    styleText
            //));
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            // We don't trigger completion when user typed
            if (char.IsNumber(trigger.Character)         // a number
                || char.IsPunctuation(trigger.Character) && trigger.Character != '"' // punctuation (for some reason '"' counts as punctuation as well...)
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
            
            // Without FindTokenSpanAtPosition, we cannot accurately determine the token span
            // Assuming simple handling or returning a broad span instead:
            var tokenSpan = new SnapshotSpan(triggerLocation, 0); // Simplified span, not refined
            return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
        }
    }
}
