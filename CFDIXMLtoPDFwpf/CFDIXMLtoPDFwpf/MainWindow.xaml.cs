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
            float midPage = pageWidth / 2;
            float topWritingArea = 0;
            int sectionSpacing = 15;

            //Fonts
            string fontName = "Arial Condensed";
            PdfTrueTypeFont font7Bold = new PdfTrueTypeFont(new Font(fontName, 7f, System.Drawing.FontStyle.Bold));
            PdfTrueTypeFont font7 = new PdfTrueTypeFont(new Font(fontName, 7f, System.Drawing.FontStyle.Regular));
            PdfTrueTypeFont font6Italic = new PdfTrueTypeFont(new Font(fontName, 6f, System.Drawing.FontStyle.Italic));

            //Pen
            PdfPen penLightGray1p = new PdfPen(System.Drawing.Color.LightGray, 1f);
            PdfPen penBlack10p = new PdfPen(System.Drawing.Color.Black, 10f);

            //Brushes
            PdfBrush brushBlack = new PdfSolidBrush(System.Drawing.Color.Black);
            PdfBrush brushLightGray = new PdfSolidBrush(System.Drawing.Color.LightGray);

            //Format Alignments
            PdfStringFormat formatRight = new PdfStringFormat(PdfTextAlignment.Right);
            PdfStringFormat formatLeft = new PdfStringFormat(PdfTextAlignment.Left)
            {
                CharacterSpacing = 0.4f
            };

            //Page header
            String text = "Factura version " + cfdi.Version;
            page.Canvas.DrawString(text, font6Italic, brushLightGray, pageWidth, y, formatRight);
            SizeF size = font6Italic.MeasureString(text, formatRight);
            y = y + size.Height + 1;
            page.Canvas.DrawLine(penLightGray1p, 0, y, pageWidth, y);

            y = y + 5;
            topWritingArea = y;

            //Issuerinfo
            //Name
            DrawToPage(page, cfdi.Emisor.Nombre, font7Bold, brushBlack, leftMargin, y, out y, formatLeft);
            //RFC
            DrawToPage(page, cfdi.Emisor.Rfc, font7Bold, brushBlack, leftMargin, y, out y, formatLeft);
            //Fiscal Regime
            DrawToPage(page, cfdi.Emisor.RegimenFiscal, font7, brushBlack, leftMargin, y, out y, formatLeft);
            //Address
            text = "Calle:" + cfdi.Emisor.Domicilio.Calle + " No:" + cfdi.Emisor.Domicilio.NoExterior;
            if(cfdi.Emisor.Domicilio.NoInterior != null)
            {
                text += "-" + cfdi.Emisor.Domicilio.NoInterior;
            }
            text += " Col:" + cfdi.Emisor.Domicilio.Colonia;
            DrawToPage(page, text , font7, brushBlack, leftMargin, y, out y, formatLeft);
            text = "Localidad:" + cfdi.Emisor.Domicilio.Localidad + ", Municipio:" + cfdi.Emisor.Domicilio.Municipio + ", Estado:" + cfdi.Emisor.Domicilio.Estado + ", CP:" + cfdi.Emisor.Domicilio.Cp;
            DrawToPage(page, text, font7, brushBlack, leftMargin, y, out y, formatLeft);
            DrawToPage(page, "Pais:" + cfdi.Emisor.Domicilio.Pais, font7, brushBlack, leftMargin, y, out y, formatLeft);

            //Issue place
            y += 5;
            DrawToPageWithHeader(page, "Lugar de expedición:", cfdi.LugarExpedicion, font7Bold, font7, brushBlack, formatLeft, leftMargin, y, out y);

            //Invoice data
            y = topWritingArea;
            //Invoice header
            y += 5;
            page.Canvas.DrawLine(penBlack10p, midPage, y, pageWidth, y);
            text = "Factura";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, pageWidth-size.Width -10, y, out y, formatLeft);
            //Invoice number
            DrawToPageWithHeader(page, "Folio:", cfdi.Folio, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Serie:", cfdi.Serie, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Folio Fiscal:", cfdi.TimbreFiscal.UUID, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Serie CSD del SAT:", cfdi.TimbreFiscal.NoCertificadoSAT, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "No. Certificado:", cfdi.NoCertificado, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Fecha emsión:", cfdi.Fecha, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Fecha certificación:", cfdi.TimbreFiscal.FechaTimbrado, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);

            //Reciever data
            //Reciever header
            y += sectionSpacing;
            page.Canvas.DrawLine(penBlack10p, leftMargin, y, pageWidth, y);
            text = "Receptor";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, midPage - (size.Width/2), y, out y, formatLeft);
            //Reciever name
            DrawToPageWithHeader(page, "Receptor:", cfdi.Receptor.Nombre, font7Bold, font7, brushBlack, formatLeft, leftMargin, y, out y);
            //Reciever address
            text = "Calle:" + cfdi.Receptor.Domicilio.Calle + " No:" + cfdi.Receptor.Domicilio.NoExterior;
            if (cfdi.Receptor.Domicilio.NoInterior != null)
            {
                text += "-" + cfdi.Receptor.Domicilio.NoInterior;
            }
            text += " Col:" + cfdi.Receptor.Domicilio.Colonia;
            DrawToPageWithHeader(page, "Domicilio", text, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y, true);
            //RFC
            DrawToPageWithHeader(page, "R.F.C.:", cfdi.Receptor.Rfc, font7Bold, font7, brushBlack, formatLeft, leftMargin, y, out y);
            //Next line address
            text = "Localidad:" + cfdi.Receptor.Domicilio.Localidad + ", Municipio:" + cfdi.Receptor.Domicilio.Municipio + ", Estado:" + cfdi.Receptor.Domicilio.Estado + ", CP:" + cfdi.Receptor.Domicilio.Cp;
            DrawToPage(page, text, font7, brushBlack, midPage, y, out y, formatLeft);
            DrawToPage(page, "Pais:" + cfdi.Receptor.Domicilio.Pais, font7, brushBlack, midPage, y, out y, formatLeft);

            //Products
            y += sectionSpacing;

        }

        private static void DrawToPage(PdfPageBase page, string text, PdfTrueTypeFont font, PdfBrush brushColor, float leftMargin, float y, out float yout, PdfStringFormat formatLeft)
        {
            page.Canvas.DrawString(text, font, brushColor, leftMargin, y, formatLeft);
            SizeF size = font.MeasureString(text, formatLeft);
            yout = y + size.Height + 2;
        }

        private static void DrawToPageWithHeader(PdfPageBase page, string header, string text, PdfTrueTypeFont fontHeader, PdfTrueTypeFont font, PdfBrush brushColor, PdfStringFormat formatLeft, float leftMargin, float y, out float yout, bool ignoreOut)
        {
            page.Canvas.DrawString(header, fontHeader, brushColor, leftMargin, y, formatLeft);
            SizeF size = font.MeasureString(header, formatLeft);
            page.Canvas.DrawString(text, font, brushColor, leftMargin + size.Width + 1, y, formatLeft);
            yout = (ignoreOut)?y : y + size.Height + 2;
        }

        private static void DrawToPageWithHeader(PdfPageBase page, string header, string text, PdfTrueTypeFont fontHeader, PdfTrueTypeFont font, PdfBrush brushColor, PdfStringFormat formatLeft, float leftMargin, float y, out float yout)
        {
            DrawToPageWithHeader(page, header, text, fontHeader, font, brushColor, formatLeft, leftMargin, y, out yout, false);
        }

        private void ImgRemove_MouseLeftButtonUp(object sender, RoutedEventArgs e)
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

        private void ImgClean_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            lbx_files.Items.Clear();
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

            foreach (string filename in dlg.FileNames)
            {
                lbx_files.Items.Add(filename);
            }
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
    }
}
