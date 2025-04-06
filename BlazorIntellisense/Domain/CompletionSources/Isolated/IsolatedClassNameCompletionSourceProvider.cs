using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;

namespace BlazorIntellisense.Domain.CompletionSources.Isolated
{
    /// <summary>
    /// Provides intellisense for isolated stylesheet class names.
    /// Maintains a cache of completion sources per file (file = <see cref="ITextView"/>)
    /// </summary>
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType("razor")]
    [ContentType("html")]
    [ContentType("RazorCSharp")]
    [ContentType("LegacyRazorCSharp")]
    [Order(Before = "default")]
    [Name(nameof(IsolatedClassNameCompletionSourceProvider))]
    public class IsolatedClassNameCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        public static readonly ImageElement IsolatedCompletionIcon = 
            new ImageElement(KnownMonikers.Blazor.ToImageId(), "Bl");

        private readonly ConditionalWeakTable<ITextView, IsolatedClassNameCompletionSource> _cachedCompletionSources 
            = new ConditionalWeakTable<ITextView, IsolatedClassNameCompletionSource>();

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            // Try get from cache
            if (_cachedCompletionSources.TryGetValue(textView, out var source))
            {
                return source;
            }

            // Create new
            source = new IsolatedClassNameCompletionSource(textView);
            
            // Add to cache
            _cachedCompletionSources.Add(textView, source);
            textView.Closed += (sender, args) =>
            {
                // Remove the source when the text view is closed
                _cachedCompletionSources.Remove(textView);
            };

            // Return the source
            return source;
        }
    }
}