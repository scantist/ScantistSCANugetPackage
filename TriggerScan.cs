using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using com.scantist.ci;
using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using System.IO;
using Microsoft.VisualStudio;

namespace ScantistSCA
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TriggerScan
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7ed9729a-680f-4825-b693-9c44d95886f3");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerScan"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TriggerScan(AsyncPackage package, OleMenuCommandService commandService)
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
        public static TriggerScan Instance
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
            // Switch to the main thread - the call to AddCommand in TriggerScan's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TriggerScan(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            SettingsManager settingsManager = new ShellSettingsManager((IServiceProvider)ServiceProvider);
            WritableSettingsStore userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            String serverUrl = userSettingsStore.GetString("ScantistSCA", "ServerURL", "https://api.scantist.io/");
            String token = userSettingsStore.GetString("ScantistSCA", "Token", "");
            String projectName = userSettingsStore.GetString("ScantistSCA", "ProjectName", "");

            ProjectDetailWindow projectDetailWindow = new ProjectDetailWindow();
            projectDetailWindow.ServerUrl = serverUrl;
            projectDetailWindow.Token = token;
            projectDetailWindow.ProjectName = projectName;
            projectDetailWindow.ShowModal();

            String ServerUrl = projectDetailWindow.ServerUrl;
            String Token = projectDetailWindow.Token;
            String ProjectName = projectDetailWindow.ProjectName;
            String FilePath = projectDetailWindow.txtFileName.Text;

            if (projectDetailWindow.DialogResult.GetValueOrDefault(false))
            {
                var dialogFactory = await ServiceProvider.GetServiceAsync(typeof(SVsThreadedWaitDialogFactory)).ConfigureAwait(false) as IVsThreadedWaitDialogFactory;
                IVsThreadedWaitDialog2 dialog = null;
                if (dialogFactory != null)
                {
                    dialogFactory.CreateInstance(out dialog);
                }

                if (dialog != null && dialog.StartWaitDialog(
                "Scantist SCA", "Retrieving the project dependency",
                "We are getting there soon...", null,
                "ScantistSCA is running...",
                0, false,
                true) == VSConstants.S_OK)
                {
                    if (!userSettingsStore.CollectionExists("ScantistSCA"))
                    {
                        userSettingsStore.CreateCollection("ScantistSCA");
                    }

                    userSettingsStore.SetString("ScantistSCA", "ServerURL", ServerUrl);
                    userSettingsStore.SetString("ScantistSCA", "Token", Token);
                    userSettingsStore.SetString("ScantistSCA", "ProjectName", ProjectName);

                    CommandParameters commandParameters = new CommandParameters();
                    commandParameters.parseCommandLine(new string[11] { "-t", Token, "-serverUrl",
                    ServerUrl, "-scanType", "source_code", "-f", Path.GetDirectoryName(FilePath),
                    "-project_name", ProjectName, "--debug"});
                    int scanID = new Application().run(commandParameters);

                    if (scanID == 0)
                    {
                        VsShellUtilities.ShowMessageBox(
                       this.package,
                        "Cannot trigger SCA scan. Please check log file for detail.",
                       "Error",
                       OLEMSGICON.OLEMSGICON_INFO,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    else
                    {
                        VsShellUtilities.ShowMessageBox(
                       this.package,
                        "Scan " + scanID + " is successfully created.",
                       "Completed",
                       OLEMSGICON.OLEMSGICON_INFO,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                        userSettingsStore.SetString("ScantistSCA", "ScanID", scanID.ToString());

                        await this.package.JoinableTaskFactory.RunAsync(async delegate
                        {
                            ToolWindowPane window = await this.package.ShowToolWindowAsync(typeof(ComponentResult), 0, true, this.package.DisposalToken);
                            if ((null == window) || (null == window.Frame))
                            {
                                throw new NotSupportedException("Cannot create tool window");
                            }
                        });

                    }
                }
                int usercancel;
                dialog.EndWaitDialog(out usercancel);
            }
        }
    }
}
