using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ScantistSCA
{
    /// <summary>
    /// Interaction logic for ProjectDetailWindow.xaml
    /// </summary>
    public partial class ProjectDetailWindow : DialogWindow
    {
        public String ServerUrl 
        {
            get
            {
                return txtServerUrl.Text;
            }
            set 
            {
                txtServerUrl.Text = value;
            }
        }
        public String Token
        {
            get
            {
                return txtToken.Text;
            }
            set
            {
                txtToken.Text = value;
            }
        }
        public String ProjectName
        {
            get
            {
                return txtProjectName.Text;
            }
            set
            {
                txtProjectName.Text = value;
            }
        }

        Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();

        public ProjectDetailWindow()
        {
            InitializeComponent();
            this.IsCloseButtonEnabled = true;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(openFileDlg.FileName))
            {
                MessageBox.Show("Please select the project solution file!", "Error");
            }
            else
            {
                DialogResult = true;
                this.Close(); 
            }
        }

        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
           
            openFileDlg.Filter = "sln files (*.sln)|*.sln";

            // Launch OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDlg.ShowDialog();
            // Get the selected file name and display in a TextBox.
            // Load content of file in a TextBlock
            if (result == true)
            {
                txtFileName.Text = openFileDlg.FileName;
            }
        }
    }
}
