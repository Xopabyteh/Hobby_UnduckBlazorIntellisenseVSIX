using ExCSS;

namespace Hobby_BlazorIntellisense.Domain
{
    public class CssClassCompletion
    {
        /// <summary>
        /// The name of the class, like "accordion"
        /// </summary>
        public string ClassName { get; set; }
        
        /// <summary>
        /// Like ".a > .b,.c"
        /// </summary>
        public string EntireSelector { get; set; }

        /// <summary>
        /// The CSS style text that is used to describe the class.
        /// .a { foo: bar; }
        /// </summary>
        public string FullStyleText { get; set; }

        public string StylesheetFilePath { get; set; }
        public TextPosition StylesheetPositionStart { get; set; }
    }
}