using System.Collections.Immutable;

namespace Hobby_BlazorIntellisense.Domain
{
    public class StylesheetCompletions
    {
        public ImmutableArray<CssClassCompletion> Classes { get; set; }
    }
}