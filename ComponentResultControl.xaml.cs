namespace ScantistSCA
{
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Settings;
    using Newtonsoft.Json;
    using System;
    using System.Net.Http;
    using System.Windows;
    using System.Windows.Controls;
    using ScantistSCA.models;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio;

    /// <summary>
    /// Interaction logic for ComponentResultControl.
    /// </summary>
    public partial class ComponentResultControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentResultControl"/> class.
        /// </summary>
        public ComponentResultControl()
        {
            this.InitializeComponent();
        }

        private void btnRefreshResult_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SettingsManager settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            SettingsStore userSettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);
            String scanID = userSettingsStore.GetString("ScantistSCA", "ScanID", "");
            String serverUrl = userSettingsStore.GetString("ScantistSCA", "ServerURL", "https://api.scantist.io/");
            String token = userSettingsStore.GetString("ScantistSCA", "Token", "");
            if (!String.IsNullOrEmpty(scanID) && !String.IsNullOrEmpty(token))
            {
                var dialogFactory = ServiceProvider.GlobalProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
                IVsThreadedWaitDialog2 dialog = null;
                if (dialogFactory != null)
                {
                    dialogFactory.CreateInstance(out dialog);
                }

                if (dialog != null && dialog.StartWaitDialog(
                "Scantist SCA", "Retrieving the scan result for scan id " + scanID,
                "We are getting there soon...", null,
                "ScantistSCA is running...",
                0, false,
                true) == VSConstants.S_OK)
                {
                    var componentList = GetScanResult(scanID, serverUrl, token, 0);
                    ComponentDataGrid.ItemsSource = componentList;
                }
                int usercancel;
                dialog.EndWaitDialog(out usercancel);
            }
        }

        private ObservableCollection<ComponentModel> GetScanResult(string scanID, string serverUrl, string token, int count)
        {
            ObservableCollection<ComponentModel> componentList = new ObservableCollection<ComponentModel>();
            if (count == 30)
            {
                return componentList;
            }
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", token);
            HttpResponseMessage response = client.GetAsync(serverUrl + "ci-scan-results/?scan_id=" + scanID).GetAwaiter().GetResult();
            var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var responseData = JsonConvert.DeserializeObject<ScanResultResponse>(result);
            if (responseData.status.Equals("finished"))
            {
                foreach (ScanResultComponent c in responseData.results.components)
                {
                    ComponentModel component = new ComponentModel(c.library, c.version, c.license, c.vulnerabilities);
                    componentList.Add(component);
                }

            }
            else if (responseData.status.Equals("failed"))
            {
                MessageBox.Show("Scan failed! Please visit Scantist SCA Website for more info.", "Error");
                return componentList;
            }
            else
            {
                System.Threading.Thread.Sleep(10000);
                count++;
                return GetScanResult(scanID, serverUrl, token, count);
            }


            return componentList;
        }
    }
}