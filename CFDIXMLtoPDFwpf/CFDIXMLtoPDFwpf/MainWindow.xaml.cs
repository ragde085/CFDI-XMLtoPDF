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
using Spire.Pdf;
using Spire.Pdf.Graphics;
using System.Drawing;
using System.Windows.Media.Animation;

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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Lbx_files_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            bool dropEnabled = true;
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                string[] filenames =
                                 e.Data.GetData(System.Windows.DataFormats.FileDrop, true) as string[];

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


        private void Lbx_files_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string[] droppedFilenames =
                                e.Data.GetData(System.Windows.DataFormats.FileDrop, true) as string[];
            
            foreach (string filename in droppedFilenames)
            {
                lbx_files.Items.Add(filename);
                Debug.WriteLine(filename);
            }

            EnableConvert();
        }

        private void EnableConvert()
        {
            if (txt_target.Text.Length > 0 && lbx_files.Items.Count > 0)
            {
                DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(2));
                btnConvert.BeginAnimation(System.Windows.Controls.Button.OpacityProperty, animation);
            }
        }

        private void Btn_browse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                txt_target.Text = dialog.SelectedPath;
            }

            EnableConvert();
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            lbx_files.IsEnabled = false;
            DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(2));
            pgb_Progress.BeginAnimation(System.Windows.Controls.ProgressBar.OpacityProperty, animation);

            foreach (string file in lbx_files.Items)
            {
                CreatePDFFromCFDI(txt_target.Text, GetFileName(file), new CFDIXML(file).LoadFile());
            }

            animation = new DoubleAnimation(0, TimeSpan.FromSeconds(2));
            pgb_Progress.BeginAnimation(System.Windows.Controls.ProgressBar.OpacityProperty, animation);
            lbx_files.IsEnabled = true;
        }

        private string GetFileName(string file)
        {
            int startIndex = file.LastIndexOf('\\');
            int endIndex = file.LastIndexOf('.');
            int length = endIndex - startIndex - 1;
            return file.Substring(startIndex+1, length);
        }

        private void CreatePDFFromCFDI(string outputFolder, string fileName, CFDIXML cfdi)
        {
            //PdfDocument document = new PdfDocument(PdfConformanceLevel.Pdf_A1B);

            //PdfPageBase page = document.Pages.Add();

            ////save graphics state
            //PdfGraphicsState state = page.Canvas.Save();
            ////Draw text - brush
            //String text = "Text,turn Around! Go! Go! Go!";
            //PdfPen pen = PdfPens.DeepSkyBlue;
            //PdfSolidBrush brush = new PdfSolidBrush(System.Drawing.Color.Blue);
            //PdfStringFormat format = new PdfStringFormat();
            //PdfFont font = new PdfFont(PdfFontFamily.Helvetica, 18f, PdfFontStyle.Italic);
            //SizeF size = font.MeasureString(text, format);
            //RectangleF rctg
            //    = new RectangleF(page.Canvas.ClientSize.Width / 2 + 10, 180,
            //    size.Width / 3 * 2, size.Height * 3);
            //page.Canvas.DrawString(text, font, pen, brush, rctg, format);
            
            ////restore graphics
            //page.Canvas.Restore(state);

            //document.SaveToFile(outputFolder + "\\" + fileName + ".pdf");

        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            HashSet<string> toRemove = new HashSet<string>();
            foreach (string item in lbx_files.SelectedItems)
            {
                toRemove.Add(item);
            }
            foreach (string item in toRemove)
            {
                lbx_files.Items.Remove(item);
            }
        }

        private void btnClean_Click(object sender, RoutedEventArgs e)
        {
            lbx_files.Items.Clear();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.CheckPathExists = true;
            dlg.Filter = "XML Files (*.xml)|*.xml|All Files(*.*)|*.*";
            dlg.Multiselect = true;

            if (dlg.ShowDialog() != true) { return; }

            foreach (string filename in dlg.FileNames)
            {
                lbx_files.Items.Add(filename);
            }
        }
    }
}
