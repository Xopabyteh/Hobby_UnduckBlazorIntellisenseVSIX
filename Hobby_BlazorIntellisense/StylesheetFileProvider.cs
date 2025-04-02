namespace BlazorIntellisense
{
    public class StylesheetFileProvider
    {
        public static StylesheetFileProvider Instance { get; private set; } = new StylesheetFileProvider();

        public string[] GetFilePaths()
            => new string[]
            {
                "C:\\VProjects\\HAVIT\\196.BTC\\Havit.Btc.Blazor\\wwwroot\\css\\bootstrap.css"
            };
    }
}
