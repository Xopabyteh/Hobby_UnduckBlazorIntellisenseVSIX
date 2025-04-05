using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Hobby_BlazorIntellisense.Domain;
using Hobby_BlazorIntellisense.Domain.Settings;
using Hobby_BlazorIntellisense.Infrastructure;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Hobby_BlazorIntellisense
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RebuildGlobalCompletionsCacheCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0102;

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
        private RebuildGlobalCompletionsCacheCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RebuildGlobalCompletionsCacheCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RebuildGlobalCompletionsCacheCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RebuildGlobalCompletionsCacheCommand(package, commandService);
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

                // Load settings
                var settingsService = SolutionCompletionSettingsService.Instance;
                var settings = settingsService.EnsureLoadSettingsForSolution(slnFilePath);

                if(settings == null)
                {
                    return;
                }

                // -> Settings loaded
                // Load global completions from:
                //  1. Whitelisted global stylesheets
                var stronglyDefinedGlobalStylesheets = settingsService.WhitelistGlobalStylesheetPaths;

                //  2. Whitelisted global stylesheets directories (recursively)
                var excludedDirs = new string[] { "bin", "obj" };
                var foundStylesheets = settingsService.WhitelistGlobalStylesheetDirectoryPaths
                    .SelectMany(dir => OmmitiveFileSearch.GetFilesExcludingDirs(
                        dir,
                        "*.css",
                        excludedExtension: ".razor.css",
                        excludedDirs: excludedDirs
                    ));


                vsStatusBar.SetText("Rebuilding global completions cache...");

                SolutionCssCatalogService.Instance.BuildSolutionGlobalCache(
                    stronglyDefinedGlobalStylesheets
                        .Concat(foundStylesheets)
                );

                vsStatusBar.SetText("Global completions cache rebuilt.");
            });
        }
    }
}
