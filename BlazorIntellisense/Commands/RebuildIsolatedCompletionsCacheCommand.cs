using System;
using System.ComponentModel.Design;
using System.IO;
using EnvDTE;
using EnvDTE80;
using BlazorIntellisense.Domain;
using BlazorIntellisense.Infrastructure;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace BlazorIntellisense.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RebuildIsolatedCompletionsCacheCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0103;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("9a443e1a-ba81-45fd-8491-394c70a184a5");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RebuildGlobalCompletionsCacheCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RebuildIsolatedCompletionsCacheCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RebuildIsolatedCompletionsCacheCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RebuildIsolatedCompletionsCacheCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RebuildIsolatedCompletionsCacheCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        public void Execute(object sender, EventArgs e)
        {
            _ = package.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

                var vsStatusBar = (IVsStatusbar)await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar));

                var dte = (DTE2)await ServiceProvider.GetServiceAsync(typeof(DTE));
                var solution = dte.Solution;
                var slnFilePath = solution.FullName;

                var solutionDirectory = Path.GetDirectoryName(slnFilePath);

                // Load all isolated stylesheets we can find in the solution

                var excludedDirs = new string[] { "bin", "obj" };
                var foundStylesheets = OmmitiveFileSearch.GetFilesExcludingDirs(
                        solutionDirectory,
                        "*.razor.css",
                        excludedDirs: excludedDirs,
                        excludedExtension: null
                    );


                vsStatusBar.SetText("Rebuilding .razor.css completions cache...");

                SolutionCssCatalogService.Instance.BuildIsolatedStylesheetsCaches(foundStylesheets);

                vsStatusBar.SetText(".razor.css cache rebuilt.");
            });
        }
    }
}
