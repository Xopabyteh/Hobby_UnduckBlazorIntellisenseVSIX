using BlazorIntellisense.Domain;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BlazorIntellisense.Domain
{
    public class StylesheetCompletions
    {
        public ImmutableArray<CssClassCompletion> Classes { get; private set; }
        public ImmutableDictionary<string, CssClassCompletion> ClassNameToCompletion { get; private set; }

        /// <inheritdoc cref="Update(ICollection{CssClassCompletion})"/>
        public StylesheetCompletions(ICollection<CssClassCompletion> allClasses)
        {
            Update(allClasses);
        }

        /// <summary>
        /// The method wil filter and remove duplicates from provided data
        /// </summary>
        public void Update(ICollection<CssClassCompletion> allClasses)
        {
            Classes = allClasses
                .DistinctBy(c => c.ClassName, StringComparer.OrdinalIgnoreCase)
                .ToImmutableArray();

            ClassNameToCompletion = Classes.ToImmutableDictionary(
                k => k.ClassName,
                v => v
            );
        }
    }
}