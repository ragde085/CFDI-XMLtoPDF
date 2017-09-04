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
            if (txt_target.Text.Length > 0 && lbx_files?.Items.Count > 0)
            {
                DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(1));
                btnConvert.BeginAnimation(System.Windows.Controls.Button.OpacityProperty, animation);
            }
            else if (txt_target.Text.Length == 0 || lbx_files?.Items.Count == 0)
            {
                DoubleAnimation animation = new DoubleAnimation(0.2, TimeSpan.FromSeconds(2));
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
            pgb_Progress.Maximum = lbx_files.Items.Count;
            foreach (string file in lbx_files.Items)
            {
                CreatePDFFromCFDI(txt_target.Text, GetFileName(file), new CFDIXML(file).LoadFile());
                pgb_Progress.Value = pgb_Progress.Value+1;
            }

            animation = new DoubleAnimation(0.2, TimeSpan.FromSeconds(2));
            pgb_Progress.BeginAnimation(System.Windows.Controls.ProgressBar.OpacityProperty, animation);
            pgb_Progress.Maximum = 0;
            pgb_Progress.Value = 0;
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
            PdfDocument document = new PdfDocument();
            PdfPageBase page = document.Pages.Add();

            // Draw the page
            DrawPage(page, cfdi);

            //set document info
            document.DocumentInformation.Creator = "CFDI XML to PDF - Silverwolf Development";
            document.DocumentInformation.Keywords = "pdf, cfdi, " + cfdi.TimbreFiscal.NoCertificadoSAT;
            document.DocumentInformation.Producer = "Spire.Pdf";
            string title = "Factura " + cfdi.Folio;
            if (cfdi.Serie != null)
            {
                title += " " + cfdi.Serie;
            }
            document.DocumentInformation.Title = title;
            document.DocumentInformation.Author = cfdi.Emisor.Nombre;


            ////save graphics state
            //PdfGraphicsState state = page.Canvas.Save();
            ////Draw text - brush
            //string text = "Text,turn Around! Go! Go! Go!";
            //PdfPen pen = PdfPens.DeepSkyBlue;
            //PdfSolidBrush brush = new PdfSolidBrush(System.Drawing.Color.Blue);
            //PdfStringFormat format = new PdfStringFormat();
            //PdfFont font = new PdfFont(PdfFontFamily.Helvetica, 18f, PdfFontStyle.Italic);
            //SizeF size = font.MeasureString(text, format);
            ////RectangleF rctg
            ////    = new RectangleF(page.Canvas.ClientSize.Width / 2 + 10, 180,
            ////    size.Width / 3 * 2, size.Height * 3);
            ////page.Canvas.DrawString(text, font, pen, brush, rctg, format);

            ////restore graphics
            //page.Canvas.Restore(state);

            string outputFile = outputFolder + "\\" + fileName + ".pdf";
            try {
                document.SaveToFile(outputFile);
            }catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
            document.Close();

            System.Diagnostics.Process.Start(outputFile);
        }

        private static void DrawPage(PdfPageBase page, CFDIXML cfdi)
        {
            float pageWidth = page.Canvas.ClientSize.Width;
            float y = 0;
            float leftMargin = 10;
 
            //page header
            PdfPen pen1 = new PdfPen(System.Drawing.Color.LightGray, 1f);
            PdfBrush brush1 = new PdfSolidBrush(System.Drawing.Color.LightGray);
            PdfTrueTypeFont font6Italic = new PdfTrueTypeFont(new Font("Arial", 6f, System.Drawing.FontStyle.Italic));
            PdfStringFormat format1 = new PdfStringFormat(PdfTextAlignment.Right);
            String text = "Factura version " + cfdi.Version;
            page.Canvas.DrawString(text, font6Italic, brush1, pageWidth, y, format1);
            SizeF size = font6Italic.MeasureString(text, format1);
            y = y + size.Height + 1;
            page.Canvas.DrawLine(pen1, 0, y, pageWidth, y);

            //Fonts
            PdfTrueTypeFont font7Bold = new PdfTrueTypeFont(new Font("Arial Condensed", 7f, System.Drawing.FontStyle.Bold));
            PdfTrueTypeFont font7 = new PdfTrueTypeFont(new Font("Arial Condensed", 7f, System.Drawing.FontStyle.Regular));
            //Emiter info
            y = y + 5;
            PdfBrush brushBlack = new PdfSolidBrush(System.Drawing.Color.Black);
            PdfStringFormat formatLeft = new PdfStringFormat(PdfTextAlignment.Left)
            {
                CharacterSpacing = 0.4f
            };

            //Name
            DrawToPage(page, cfdi.Emisor.Nombre, font7Bold, brushBlack, leftMargin, y, out y, formatLeft);
            //RFC
            DrawToPage(page, cfdi.Emisor.Rfc, font7Bold, brushBlack, leftMargin, y, out y, formatLeft);
            //Regimen Fiscal
            DrawToPage(page, cfdi.Emisor.RegimenFiscal, font7, brushBlack, leftMargin, y, out y, formatLeft);
            //Address
            text = "Calle:" + cfdi.Emisor.Domicilio.Calle + " No: " + cfdi.Emisor.Domicilio.NoExterior;
            if(cfdi.Emisor.Domicilio.NoInterior != null)
            {
                text += "-" + cfdi.Emisor.Domicilio.NoInterior;
            }
            text += " Col: " + cfdi.Emisor.Domicilio.Colonia;
            DrawToPage(page, text , font7, brushBlack, leftMargin, y, out y, formatLeft);
            text = "Localidad:" + cfdi.Emisor.Domicilio.Localidad + ", Municipio: " + cfdi.Emisor.Domicilio.Municipio + ", Estado: " + cfdi.Emisor.Domicilio.Estado + ", C.P.: " + cfdi.Emisor.Domicilio.Cp;
            DrawToPage(page, text, font7, brushBlack, leftMargin, y, out y, formatLeft);
            DrawToPage(page, "Pais: " + cfdi.Emisor.Domicilio.Pais, font7, brushBlack, leftMargin, y, out y, formatLeft);

            ////icon
            ////PdfImage image = PdfImage.FromFile(@"Wikipedia_Science.png");
            ////page.Canvas.DrawImage(image, new PointF(pageWidth - image.PhysicalDimension.Width, y));
            ////float imageLeftSpace = pageWidth - image.PhysicalDimension.Width - 2;
            ////float imageBottom = image.PhysicalDimension.Height + y;
            //float imageLeftSpace = pageWidth -2 ;
            //float imageBottom = y;

            ////refenrence content
            //PdfTrueTypeFont font3 = new PdfTrueTypeFont(new Font("Arial", 9f));
            //PdfStringFormat format3 = new PdfStringFormat
            //{
            //    ParagraphIndent = font3.Size * 2,
            //    MeasureTrailingSpaces = true,
            //    LineSpacing = font3.Size * 1.5f
            //};
            //String text1 = "(All text and picture from ";
            //String text2 = "Wikipedia";
            //String text3 = ", the free encyclopedia)";
            //page.Canvas.DrawString(text1, font3, brushBlack, 0, y, format3);
 
            //size = font3.MeasureString(text1, format3);
            //float x1 = size.Width;
            //format3.ParagraphIndent = 0;
            //PdfTrueTypeFont font4 = new PdfTrueTypeFont(new Font("Arial", 9f, System.Drawing.FontStyle.Underline));
            //PdfBrush brush3 = PdfBrushes.Blue;
            //page.Canvas.DrawString(text2, font4, brush3, x1, y, format3);
            //size = font4.MeasureString(text2, format3);
            //x1 = x1 + size.Width;
 
            //page.Canvas.DrawString(text3, font3, brushBlack, x1, y, format3);
            //y = y + size.Height;
 
            ////content
            //PdfStringFormat format4 = new PdfStringFormat();
            //text = "test text now found from file"; //System.IO.File.ReadAllText(@"Summary_of_Science.txt");
            //PdfTrueTypeFont font5 = new PdfTrueTypeFont(new Font("Arial", 10f));
            //format4.LineSpacing = font5.Size* 1.5f;
            //PdfStringLayouter textLayouter = new PdfStringLayouter();
            //float imageLeftBlockHeight = imageBottom - y;
            //PdfStringLayoutResult result
            //    = textLayouter.Layout(text, font5, format4, new SizeF(imageLeftSpace, imageLeftBlockHeight));
            //if (result.ActualSize.Height<imageBottom - y)
            //{
            //    imageLeftBlockHeight = imageLeftBlockHeight + result.LineHeight;
            //    result = textLayouter.Layout(text, font5, format4, new SizeF(imageLeftSpace, imageLeftBlockHeight));
            //}
            //foreach (LineInfo line in result.Lines)
            //{
            //    page.Canvas.DrawString(line.Text, font5, brushBlack, 0, y, format4);
            //    y = y + result.LineHeight;
            //}
            //PdfTextWidget textWidget = new PdfTextWidget("Test text", font5, brushBlack);
            //PdfTextLayout textLayout = new PdfTextLayout
            //{
            //    Break = PdfLayoutBreakType.FitPage,
            //    Layout = PdfLayoutType.Paginate
            //};
            //RectangleF bounds = new RectangleF(new PointF(0, y), page.Canvas.ClientSize);
            //textWidget.StringFormat = format4;
            //textWidget.Draw(page, bounds, textLayout);
        }

        private static void DrawToPage(PdfPageBase page, string text, PdfTrueTypeFont font14Bold, PdfBrush brushBlack, float leftMargin, float y, out float yout, PdfStringFormat formatLeft)
        {
            page.Canvas.DrawString(text, font14Bold, brushBlack, leftMargin, y, formatLeft);
            SizeF size = font14Bold.MeasureString(text, formatLeft);
            yout = y + size.Height + 2;
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
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
            EnableConvert();
        }

        private void BtnClean_Click(object sender, RoutedEventArgs e)
        {
            lbx_files.Items.Clear();
            EnableConvert();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                CheckPathExists = true,
                Filter = "XML Files (*.xml)|*.xml|All Files(*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() != true) { return; }

            foreach (string filename in dlg.FileNames)
            {
                lbx_files.Items.Add(filename);
            }
            EnableConvert();
        }

        private void Txt_target_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableConvert();
        }
    }
}
