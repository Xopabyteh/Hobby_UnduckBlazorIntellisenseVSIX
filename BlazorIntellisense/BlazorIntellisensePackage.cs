﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Microsoft.VisualStudio.Threading;
using EnvDTE80;
using BlazorIntellisense.Domain.Settings;
using BlazorIntellisense.Commands;
using BlazorIntellisense.Domain.CompletionSources.Global;
using BlazorIntellisense.ToolWindows;
using BlazorIntellisense.Domain;
using Microsoft.VisualStudio.Shell.Interop;
namespace BlazorIntellisense
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(BlazorIntellisensePackage.PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideLanguageService(typeof(GlobalClassNameCompletionSource), "C#", 106)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ManageCssCatalogToolWindow))]
    public sealed class BlazorIntellisensePackage : AsyncPackage
    {
         /// <summary>
        /// BlazorIntellisensePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "1c0f6cb8-e764-4566-8a43-128da27e08b0";

        // We need to keep a reference to these "*EventsRef", because
        // there is some wizard shit that prevents it from working, unless we have the reference...
        private SolutionEvents solutionEventsRef;
        private DocumentEvents documentEventsRef;
        private ProjectItemsEvents solutionItemsEventsRef;
        
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await ManageCssCatalogToolWindowCommand.InitializeAsync(this);
            await RebuildGlobalCompletionsCacheCommand.InitializeAsync(this);
            await RebuildIsolatedCompletionsCacheCommand.InitializeAsync(this);

            // Subscribe events
            var dte = (DTE2)await GetServiceAsync(typeof(DTE));
            
            // Sln
            solutionEventsRef = dte.Events.SolutionEvents;
            solutionEventsRef.Opened += HandleSolutionOpened;
            solutionEventsRef.AfterClosing += HandleSolutionClosed;
            
            // Documents
            documentEventsRef = dte.Events.DocumentEvents;
            documentEventsRef.DocumentSaved += HandleDocumentSaved;

            // On removed
            var trackProjectDocuments = (IVsTrackProjectDocuments2) await GetServiceAsync(typeof(SVsTrackProjectDocuments));
            var trackProjectDocumentsEvents = new UpdateCatalogOnFileRemovedService();
            trackProjectDocuments.AdviseTrackProjectDocumentsEvents(trackProjectDocumentsEvents, out _);
        }

        /// <summary>
        /// Invokes rebuilding of isolated completions cache
        /// </summary>
        /// <param name="document"></param>
        private void HandleDocumentSaved(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var filePath = document.FullName;

            _= Task.Run(() =>
            {
                // If it was a .razor.css file, rebuild it's isolated completions cache
                if (!filePath.EndsWith(".razor.css", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Rebuild isolated completions cache
                SolutionCssCatalogService.Instance.BuildIsolatedStylesheetCache(filePath);
            });
        }

        private void HandleSolutionClosed()
        {
            _ = Task.Run(() => { 
                SolutionCssCatalogService.Instance.ClearAllCaches();
            });
        }

        /// <summary>
        /// Invokes loading of solution css settings
        /// - global solution files - <see cref="SolutionCompletionSettings"/>
        /// </summary>
        private void HandleSolutionOpened()
        {
            _ = Task.Run(async () =>
            {
                // Get solution file path
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                
                var dte = (DTE2)await GetServiceAsync(typeof(DTE));
                
                var solution = dte.Solution;
                var slnFilePath = solution.FullName;

                // Switch to any thread
                await Task.CompletedTask.ConfigureAwait(false);

                // Rebuild isolated completions cache
                RebuildIsolatedCompletionsCacheCommand.Instance.Execute(this, e: null);

                // Load settings
                var settingsService = SolutionCompletionSettingsService.Instance;
                var settings = settingsService.EnsureLoadSettingsForSolution(slnFilePath);

                if(settings == null)
                {
                    return;
                }

                // -> Settings loaded
                // Run build command
                RebuildGlobalCompletionsCacheCommand.Instance.Execute(this, e: null);
            });
        }
    }
}
