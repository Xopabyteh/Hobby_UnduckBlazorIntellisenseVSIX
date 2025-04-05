using EnvDTE80;
using Hobby_BlazorIntellisense.Infrastructure;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hobby_BlazorIntellisense.Domain.CompletionSources.Isolated
{
    /// <summary>
    /// Completion source for it's own file (<see cref="ITextView"/>)
    /// providing intellisense from backing isolated .razor.css file
    /// </summary>
    public class IsolatedClassNameCompletionSource : IAsyncCompletionSource
    {
        private readonly WeakReference<ITextView> _textView;
        private readonly string _activeFilePath;
        /// <summary>
        /// Path of where the isolated .razor.css file could be found.
        /// Does not imply that the file exists.
        /// </summary>
        private readonly string _isolatedRazorCssFilePath;

        private readonly bool _hasCompletions;
        private readonly WeakReference<StylesheetCompletions> _completionSource;

        public IsolatedClassNameCompletionSource(ITextView textView)
        {
            _textView = new WeakReference<ITextView>(textView);

            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _activeFilePath = dte.ActiveDocument.FullName;
            _isolatedRazorCssFilePath = $"{_activeFilePath}.css";

            _hasCompletions = SolutionCssCatalogService.Instance.RazorIsolationCompletions.TryGetValue(_isolatedRazorCssFilePath, out var completions);
            if(_hasCompletions)
            {
                _completionSource = new WeakReference<StylesheetCompletions>(completions);
            }
        }

        public Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
        {
            if(!_hasCompletions || !_completionSource.TryGetTarget(out var completions))
            {
                return Task.FromResult(CompletionContext.Empty);
            }

            return Task.FromResult(new CompletionContext(
                completions.Classes
                    .Select(c => new CompletionItem(
                        c.ClassName,
                        this,
                        IsolatedClassNameCompletionSourceProvider.IsolatedCompletionIcon
                    ))
                    .ToImmutableArray()
            ));
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            if(!_hasCompletions || !_completionSource.TryGetTarget(out var completions))
            {
                return Task.FromResult<object>(null);
            }

            var contains = completions.ClassNameToCompletion.TryGetValue(item.DisplayText, out var completion);
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
                || (char.IsPunctuation(trigger.Character) && trigger.Character != '"') // punctuation (for some reason '"' counts as punctuation as well...)
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