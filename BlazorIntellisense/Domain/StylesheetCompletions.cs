using System.Collections.Generic;
using System.Collections.Immutable;

namespace Hobby_BlazorIntellisense.Domain
{
    public class StylesheetCompletions
    {
        public ImmutableArray<CssClassCompletion> Classes { get; private set; }
        public ImmutableDictionary<string, CssClassCompletion> ClassNameToCompletion { get; private set; }

        public StylesheetCompletions(ICollection<CssClassCompletion> classes)
        {
            Classes = classes.ToImmutableArray();
            ClassNameToCompletion = Classes.ToImmutableDictionary(
                k => k.ClassName,
                v => v
            );
        }
    }
}