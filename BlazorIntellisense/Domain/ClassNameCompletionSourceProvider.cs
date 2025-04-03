using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace Hobby_BlazorIntellisense.Domain
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType("razor")]
    [ContentType("RazorCSharp")]
    [ContentType("LegacyRazorCSharp")]
    [Order(Before = "default")]
    [Name("Class Name Completion Source")]
    internal class ClassNameCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        public ClassNameCatalog ClassNameCatalog { get; private set; }
        public ClassNameCompletionSource Source { get; private set; }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (Source != null)
            {
                return Source;
            }

            ClassNameCatalog = new ClassNameCatalog();
            Source = new ClassNameCompletionSource(ClassNameCatalog);

            return Source;
        }
    }
}
