using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace BlazorIntellisense.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("7290ec18-704e-4b60-8db8-e5132d856344")]
    public class ManageCssCatalogToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManageCssCatalogToolWindow"/> class.
        /// </summary>
        public ManageCssCatalogToolWindow() : base(null)
        {
            Caption = "ManageCssCatalogToolWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new ManageCssCatalogToolWindowControl();
        }
    }
}
