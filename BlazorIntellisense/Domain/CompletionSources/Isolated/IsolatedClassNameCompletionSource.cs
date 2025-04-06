using BlazorIntellisense.Infrastructure;
using EnvDTE80;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorIntellisense.Domain.CompletionSources.Isolated
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

        private bool hasCompletions;
        private WeakReference<StylesheetCompletions> completionSource;

        public IsolatedClassNameCompletionSource(ITextView textView)
        {
            _textView = new WeakReference<ITextView>(textView);

            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _activeFilePath = dte.ActiveDocument.FullName;
            _isolatedRazorCssFilePath = $"{_activeFilePath}.css";

            SolutionCssCatalogService.Instance.OnIsolatedCompletionUpdated += HandleCompletionUpdated;
            TryGetCompletionsFromCatalog();
        }

        private void TryGetCompletionsFromCatalog()
        {
            hasCompletions = SolutionCssCatalogService.Instance.RazorIsolationCompletions.TryGetValue(_isolatedRazorCssFilePath, out var completions);
            if (hasCompletions)
            {
                completionSource = new WeakReference<StylesheetCompletions>(completions);
            }
        }

        /// <summary>
        /// Sets reference to proper completions when updated
        /// if the source doesn't already have them.
        /// This is done, because the sources are cached and when a new stylesheet is added (and saved),
        /// the sources have to get the reference to the completions of that stylesheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleCompletionUpdated(object sender, string e)
        {
            if(hasCompletions)
            {
                // We already have reference to our completions, don't bother
                return;
            }

            // Check if the completion is for our isolated file
            if (!string.Equals(e, _isolatedRazorCssFilePath, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            // Get the completions
            TryGetCompletionsFromCatalog();
        }

        public Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
        {
            if(!hasCompletions || !completionSource.TryGetTarget(out var completions))
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
            if(!hasCompletions || !completionSource.TryGetTarget(out var completions))
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