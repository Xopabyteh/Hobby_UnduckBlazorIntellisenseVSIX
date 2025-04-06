using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;
using System.Threading.Tasks;

namespace BlazorIntellisense.Domain.CompletionSources
{
    public static class SharedCompletionSourceLogic
    {
        public static Task<object> GetDescriptionAsync(CssClassCompletion fromCompletion)
        {
            return Task.FromResult<object>(new ContainerElement(ContainerElementStyle.Stacked, new[]
            {
                new ClassifiedTextElement(new[]
                {
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, fromCompletion.StylesheetFileName),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, ":\n\n"),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, fromCompletion.FullStyleText)
                })
            }));
        } 
    }
}