using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace BlazorIntellisense.Domain.CompletionSources.Global
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType("razor")]
    [ContentType("html")]
    [ContentType("RazorCSharp")]
    [ContentType("LegacyRazorCSharp")]
    [Order(Before = "default")]
    [Name(nameof(GlobalClassNameCompletionSourceProvider))]
    public class GlobalClassNameCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        public static readonly ImageElement GlobalCompletionIcon = 
            new ImageElement(KnownMonikers.GlobalVariable.ToImageId(), "G");

        public GlobalClassNameCompletionSource Source { get; private set; }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (Source != null)
            {
                return Source;
            }
            
            Source = new GlobalClassNameCompletionSource();

            return Source;
        }
    }
}
