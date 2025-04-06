namespace BlazorIntellisense.Domain
{
    public enum StylesheetType
    {
        /// <summary>
        /// The stylesheet is used in the entire solution.
        /// </summary>
        Global,
        /// <summary>
        /// The stylesheet is used in a single Razor component.
        /// </summary>
        RazorIsolation
    }
}