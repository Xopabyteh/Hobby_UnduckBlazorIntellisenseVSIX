using Microsoft.VisualStudio.Text;
using System;

namespace Hobby_BlazorIntellisense.Infrastructure
{
    public class TextNavigationHelpers
    {
        public static bool IsInsideHtmlAttribute(SnapshotPoint triggerLocation, string attributeName="class=\"")
        {
            const int maxLinesToCheck = 5;

            return IsToRightOfClass() && IsToLeftOffQuotation();
        
            bool IsToRightOfClass()
            {
                var snapshot = triggerLocation.Snapshot;
                var caretLineNumber = triggerLocation.GetContainingLine().LineNumber;

                int linesChecked = 0;
                int caretOffset = triggerLocation.Position;

                while (caretLineNumber >= 0 && linesChecked < maxLinesToCheck)
                {
                    var line = snapshot.GetLineFromLineNumber(caretLineNumber);
                    string lineText = line.GetText();

                    // Only look at the part of the text before the caret on the caret line
                    if (caretLineNumber == triggerLocation.GetContainingLine().LineNumber)
                    {
                        int relativeCaret = triggerLocation.Position - line.Start.Position;
                        lineText = lineText.Substring(0, Math.Min(relativeCaret, lineText.Length));
                    }

                    int classIndex = lineText.LastIndexOf(attributeName, StringComparison.OrdinalIgnoreCase);
                    if (classIndex >= 0)
                    {
                        return true;
                    }

                    // Early out if we hit a closing or opening tag boundary — we’re outside the element
                    if (LineHasElementClosingTag(lineText))
                        break;

                    caretLineNumber--;
                    linesChecked++;
                }

                return false;
            }

            bool IsToLeftOffQuotation()
            {
                var snapshot = triggerLocation.Snapshot;
                var caretLineNumber = triggerLocation.GetContainingLine().LineNumber;
                
                int linesChecked = 0;
                int caretOffset = triggerLocation.Position;
                    
                // Check to right and down from trigger location
                while(caretLineNumber < snapshot.LineCount && linesChecked < maxLinesToCheck)
                {
                    var line = snapshot.GetLineFromLineNumber(caretLineNumber);
                    string lineText = line.GetText();

                    // Only look at the part of the text before the caret on the caret line
                    if (caretLineNumber == triggerLocation.GetContainingLine().LineNumber)
                    {
                        int relativeCaret = triggerLocation.Position - line.Start.Position;
                        lineText = lineText.Substring(relativeCaret);
                    }

                    int quoteIndex = lineText.IndexOf('"');
                    if (quoteIndex >= 0)
                    {
                        return true;
                    }
                    // Early out if we hit a closing or opening tag boundary — we’re outside the element
                    if (LineHasElementClosingTag(lineText))
                        break;

                    caretLineNumber++;
                    linesChecked++;
                }

                return false;
            }
        }

        private static bool LineHasElementClosingTag(string lineText)
        {
            return lineText.Contains(">") || lineText.Contains("<");
        }

        /// <summary>
        /// Find the start and end (SnapshotSpan) of the word at the trigger location.
        /// </summary>
        public static SnapshotSpan FindSelectorSpan(SnapshotPoint triggerLocation)
        {
            var snapshot = triggerLocation.Snapshot;
            int position = triggerLocation.Position;

            if (snapshot.Length == 0 || position < 0 || position > snapshot.Length)
                return new SnapshotSpan(triggerLocation, 0);

            bool IsWordChar(char c) 
                => !char.IsWhiteSpace(c) && c != '"';

            int start = position;
            while (start > 0 && IsWordChar(snapshot[start - 1]))
            {
                start--;
            }

            int end = position;
            while (end < snapshot.Length && IsWordChar(snapshot[end]))
            {
                end++;
            }

            if (start == end)
            {
                // No word found, return empty span that will grow as user types
                return new SnapshotSpan(triggerLocation, 0);
            }

            return new SnapshotSpan(snapshot, start, end - start);
        }
    }
}