using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Hobby_BlazorIntellisense.Domain
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType("razor")]
    [ContentType("RazorCSharp")]
    [ContentType("LegacyRazorCSharp")]
    [Order(Before = "default")]
    [Name("Class Name Completion Source")]
    public class ClassNameCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        public static readonly ImageElement GlobalCompletionIcon = 
            new ImageElement(KnownMonikers.GlobalVariable.ToImageId(), "G");

        public ClassNameCompletionSource Source { get; private set; }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (Source != null)
            {
                return Source;
            }

            Source = new ClassNameCompletionSource();

            return Source;
        }
    }
}
