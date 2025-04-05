using EnvDTE80;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hobby_BlazorIntellisense.Domain.CompletionSources
{
    public static class SharedCompletionSourceLogic
    {
        public static Task<object> GetDescriptionAsync(CssClassCompletion fromCompletion)
        {
            // Create the navigation action to open the file in Visual Studio
            Action openFileAction = () =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                
                // Todo: maybe make the dte an external dependency for simpler caching 👀
                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

                string fullPath = Path.Combine(fromCompletion.StylesheetFilePath);
                dte.ItemOperations.OpenFile(fullPath);
            };

            return Task.FromResult<object>(new ContainerElement(ContainerElementStyle.Stacked, new[]
            {
                new ClassifiedTextElement(new[]
                {
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Identifier,
                        fromCompletion.StylesheetFileName,
                        navigationAction: openFileAction,
                        style: ClassifiedTextRunStyle.Underline),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, ":\n\n"),
                    new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, fromCompletion.FullStyleText)
                })
            }));
        }
    }
}