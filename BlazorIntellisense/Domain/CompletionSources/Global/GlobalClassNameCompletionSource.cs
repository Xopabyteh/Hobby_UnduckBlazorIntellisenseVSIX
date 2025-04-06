using BlazorIntellisense.Domain;
using BlazorIntellisense.Domain.CompletionSources;
using BlazorIntellisense.Infrastructure;
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

namespace BlazorIntellisense.Domain.CompletionSources.Global
{
    /// <summary>
    /// Provides global solution scoped intellisense for class names
    /// </summary>
    public class GlobalClassNameCompletionSource : IAsyncCompletionSource
    {
        private CompletionContext _cachedCompletionContext;

        public GlobalClassNameCompletionSource()
        {
            SolutionCssCatalogService.Instance.OnSolutionGlobalCompletionsChanged += BuildCompletionCache;
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

            return SharedCompletionSourceLogic.GetDescriptionAsync(completion);
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
            if(!TextNavigationHelpers.IsInsideHtmlAttribute(triggerLocation, attributeOpening: "class=\""))
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // -> In the class= context
            var tokenSpan = TextNavigationHelpers.FindSelectorSpan(triggerLocation);
            return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
        }
    }
}
