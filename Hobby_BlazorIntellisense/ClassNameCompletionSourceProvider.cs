using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace BlazorIntellisense
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [ContentType("razor")]
    [ContentType("RazorCSharp")]
    [ContentType("LegacyRazorCSharp")]
    [Order(Before = "default")]
    [Name("Class Name Completion Source")]
    internal class ClassNameCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        private readonly Lazy<ClassNameCompletionSource> _source = new Lazy<ClassNameCompletionSource>(
            valueFactory: () => new ClassNameCompletionSource(
                    StylesheetFileProvider.Instance
                )
        );

        public IAsyncCompletionSource GetOrCreate(ITextView textView) => _source.Value;
    }
}
