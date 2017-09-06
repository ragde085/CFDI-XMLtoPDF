using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using static System.Windows.Forms.FolderBrowserDialog;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Drawing;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Threading;
using System.Collections.ObjectModel;

namespace CFDIXMLtoPDFwpf
{
    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        public IntPtr Handle
        {
            get { return _hwnd; }
        }

        private IntPtr _hwnd;
    }

    public class PDFItem
    {
        public String File { get; set; }
        public String FilePath { get; set; }

        public PDFItem(String file, String filepath)
        {
            this.File = file;
            this.FilePath = filepath;
        }

    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }


        private void LbxXMLFiles_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            bool dropEnabled = true;
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                string[] filenames = e.Data.GetData(System.Windows.DataFormats.FileDrop, true) as string[];

                foreach (string filename in filenames)
                {
                    if (System.IO.Path.GetExtension(filename).ToUpperInvariant() != ".XML")
                    {
                        dropEnabled = false;
                        break;
                    }
                }
            }
            else
            {
                dropEnabled = false;
            }

            if (!dropEnabled)
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void LbxXMLFiles_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] droppedFilenames = e.Data.GetData(System.Windows.DataFormats.FileDrop, true) as string[];
            AddXMLFiles(droppedFilenames);
            EnableConvert();
        }

        private void AddXMLFiles(string[] droppedFilenames)
        {
            foreach (string filename in droppedFilenames)
            {
                if (lbxXMLFiles.Items.Contains(filename))
                {
                    System.Windows.Forms.MessageBox.Show(String.Format("File {0} already exists", GetFileName(filename)),"File Exists",MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }else
                {
                    lbxXMLFiles.Items.Add(new PDFItem(GetFileName(filename), filename));
                }
            }
        }

        private void EnableConvert()
        {
            if (txt_target.Text.Length > 0 && lbxXMLFiles?.Items.Count > 0)
            {
                DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(1));
                btnConvert.BeginAnimation(System.Windows.Controls.Button.OpacityProperty, animation);
                btnConvert.IsEnabled = true;
            }
            else if (txt_target.Text.Length == 0 || lbxXMLFiles?.Items.Count == 0)
            {
                DoubleAnimation animation = new DoubleAnimation(0.2, TimeSpan.FromSeconds(1));
                btnConvert.BeginAnimation(System.Windows.Controls.Button.OpacityProperty, animation);
                lbxPDFFiles.BeginAnimation(System.Windows.Controls.Button.OpacityProperty, animation);
                btnConvert.IsEnabled = false;
                pgb_Progress.Visibility = Visibility.Hidden;
            }
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            lbxXMLFiles.IsEnabled = false;
            lbxPDFFiles.Items.Clear();
            DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(1));
            pgb_Progress.Visibility = Visibility.Visible;
            lbxPDFFiles.BeginAnimation(System.Windows.Controls.Button.OpacityProperty, animation);

            BackgroundWorker worker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);

            List<object> args = new List<object>
            {
                lbxXMLFiles.Items,
                txt_target.Text
            };
            worker.RunWorkerAsync(args);

            pgb_Progress.Value = 0;
            lbxXMLFiles.IsEnabled = true;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (string output in (List<string>)e.Result) {
                lbxPDFFiles.Items.Add(new PDFItem(GetFileName(output), output));
            }

            pgb_Progress.Value = 100;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pgb_Progress.Value += e.ProgressPercentage;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<object> args = e.Argument as List<object>;
            string outputFolder = (string)args[1];
            List<string> results = new List<string>();
            ItemCollection files = (ItemCollection)args[0];
            foreach (var obj in files)
            {
                var file = (PDFItem)obj;
                Thread.Sleep(10);
                CFDI_PDFGenerator pdfGenerator = new CFDI_PDFGenerator(new CFDIXML(file.FilePath).LoadFile(), file.File, outputFolder);
                string pdfFile = pdfGenerator.GeneratePDF();
                if (pdfFile != null)
                {
                    (sender as BackgroundWorker).ReportProgress(100/files.Count);
                    results.Add(pdfFile);
                }
            }
            e.Result = results;
        }

        private string GetFileName(string file)
        {
            int startIndex = file.LastIndexOf('\\');
            int endIndex = file.LastIndexOf('.');
            int length = endIndex - startIndex - 1;
            return file.Substring(startIndex+1, length);
        }

        private void ImgRemove_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            HashSet<string> toRemove = new HashSet<string>();
            foreach (string item in lbxXMLFiles.SelectedItems)
            {
                toRemove.Add(item);
            }
            foreach (string item in toRemove)
            {
                lbxXMLFiles.Items.Remove(item);
            }
            EnableConvert();
        }

        private void ImgClean_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            lbxXMLFiles.Items.Clear();
            lbxPDFFiles.Items.Clear();
            EnableConvert();
        }

        private void Txt_target_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableConvert();
        }

        private void ImgAdd_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                CheckPathExists = true,
                Filter = "XML Files (*.xml)|*.xml|All Files(*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() != true) { return; }
            AddXMLFiles(dlg.FileNames);
            EnableConvert();
        }

        private void ImgBrowse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                txt_target.Text = dialog.SelectedPath;
            }

            EnableConvert();

        }

        private void LbxPDFFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.ListBox list = (System.Windows.Controls.ListBox)sender;
            if(list.SelectedIndex > -1)
            { 
                System.Diagnostics.Process.Start(((PDFItem)list.SelectedItem).FilePath);
            }
        }
    }
}
