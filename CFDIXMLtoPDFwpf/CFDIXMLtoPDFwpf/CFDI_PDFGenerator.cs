using QRCoder;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Tables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFDIXMLtoPDFwpf
{
    class CFDI_PDFGenerator
    {
        string outputFolder;
        string fileName;
        CFDIXML cfdi;

        public CFDI_PDFGenerator(CFDIXML cfdi, string fileName, string outputFolder)
        {
            this.outputFolder = outputFolder;
            this.fileName = fileName;
            this.cfdi = cfdi;
        }

        public string GeneratePDF()
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

            string outputFile = outputFolder + "\\" + fileName + ".pdf";
            try
            {
                document.SaveToFile(outputFile);
                document.Close();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                return null;
            }

            return outputFile;
        }

        private static void DrawPage(PdfPageBase page, CFDIXML cfdi)
        {
            float pageWidth = page.Canvas.ClientSize.Width;
            float y = 0;
            float leftMargin = 5;
            float midPage = pageWidth / 2;
            float topWritingArea = 0;
            int sectionSpacing = 15;
            float qrSize = 100;

            //Fonts
            string fontName = "Arial Condensed";
            PdfTrueTypeFont font7Bold = new PdfTrueTypeFont(new Font(fontName, 7f, System.Drawing.FontStyle.Bold));
            PdfTrueTypeFont font7 = new PdfTrueTypeFont(new Font(fontName, 7f, System.Drawing.FontStyle.Regular));
            PdfTrueTypeFont font6Italic = new PdfTrueTypeFont(new Font(fontName, 6f, System.Drawing.FontStyle.Italic));

            //Colors
            PdfRGBColor lightBlack = new PdfRGBColor(17, 17, 17);

            //Pen
            PdfPen penLightGray1p = new PdfPen(System.Drawing.Color.LightGray, 1f);
            PdfPen penLightGray05p = new PdfPen(System.Drawing.Color.LightGray, 0.5f);
            PdfPen penLightBlack10p = new PdfPen(lightBlack, 10f);

            //Brushes
            PdfBrush brushBlack = new PdfSolidBrush(System.Drawing.Color.Black);
            PdfBrush brushLightGray = new PdfSolidBrush(System.Drawing.Color.LightGray);

            //Format Alignments
            PdfStringFormat formatRight = new PdfStringFormat(PdfTextAlignment.Right);
            PdfStringFormat formatMiddle = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
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
            DrawToPage(page, cfdi.Emisor.Nombre, font7Bold, brushBlack, formatLeft, leftMargin, y, out y);
            //RFC
            DrawToPage(page, cfdi.Emisor.Rfc, font7Bold, brushBlack, formatLeft, leftMargin, y, out y);
            //Fiscal Regime
            DrawToPage(page, cfdi.Emisor.RegimenFiscal, font7, brushBlack, formatLeft, leftMargin, y, out y);
            //Address
            text = "Calle:" + cfdi.Emisor.Domicilio.Calle + " No:" + cfdi.Emisor.Domicilio.NoExterior;
            if (cfdi.Emisor.Domicilio.NoInterior != null)
            {
                text += "-" + cfdi.Emisor.Domicilio.NoInterior;
            }
            text += " Col:" + cfdi.Emisor.Domicilio.Colonia + ", Localidad:" + cfdi.Emisor.Domicilio.Localidad + ", Municipio:" + cfdi.Emisor.Domicilio.Municipio + ", Estado:" + cfdi.Emisor.Domicilio.Estado + ", CP:" + cfdi.Emisor.Domicilio.Cp;
            RectangleF area = new RectangleF(leftMargin, y, midPage - 10, 30);
            DrawToPage(page, text, font7, brushBlack, formatLeft, area, y, out y, false);
            DrawToPage(page, "Pais:" + cfdi.Emisor.Domicilio.Pais, font7, brushBlack, formatLeft, leftMargin, y, out y);

            //Invoice data
            y = topWritingArea;
            //Invoice header
            y += 5;
            page.Canvas.DrawLine(penLightBlack10p, midPage, y, pageWidth, y);
            text = "Factura";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, formatLeft, pageWidth - size.Width - 10, y, out y);
            //Invoice number
            DrawToPageWithHeader(page, "Folio:", cfdi.Folio, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Serie:", cfdi.Serie ?? "", font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Folio Fiscal:", cfdi.TimbreFiscal.UUID, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Serie CSD del SAT:", cfdi.TimbreFiscal.NoCertificadoSAT, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "No. Certificado:", cfdi.NoCertificado, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Fecha emsión:", cfdi.Fecha, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPageWithHeader(page, "Fecha certificación:", cfdi.TimbreFiscal.FechaTimbrado, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);

            //Issue place
            y += 5;
            DrawToPageWithHeader(page, "Lugar de expedición:", cfdi.LugarExpedicion, font7Bold, font7, brushBlack, formatLeft, leftMargin, y, out y);

            //Reciever data
            //Reciever header
            y += sectionSpacing;
            page.Canvas.DrawLine(penLightBlack10p, leftMargin, y, pageWidth, y);
            text = "Receptor";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, formatLeft, midPage - (size.Width / 2), y, out y);
            //Reciever name
            DrawToPageWithHeader(page, "Receptor:  ", cfdi.Receptor.Nombre, font7Bold, font7, brushBlack, formatLeft, leftMargin, y, out y, true);
            //Reciever address
            text = "Calle:  " + cfdi.Receptor.Domicilio.Calle + " No:" + cfdi.Receptor.Domicilio.NoExterior;
            if (cfdi.Receptor.Domicilio.NoInterior != null)
            {
                text += "-" + cfdi.Receptor.Domicilio.NoInterior;
            }
            DrawToPageWithHeader(page, "Domicilio:   ", text, font7Bold, font7, brushBlack, formatLeft, midPage, y, out y);
            //RFC
            DrawToPageWithHeader(page, "R.F.C.:", cfdi.Receptor.Rfc, font7Bold, font7, brushBlack, formatLeft, leftMargin, y, out y, true);
            //Next line address
            text = " Col:" + cfdi.Receptor.Domicilio.Colonia + ", Localidad:" + cfdi.Receptor.Domicilio.Localidad + ", Municipio:" + cfdi.Receptor.Domicilio.Municipio;
            area = new RectangleF(midPage, y, midPage - 10, 20);
            DrawToPage(page, text, font7, brushBlack, formatLeft, area, y, out y, false);
            text = " Estado:" + cfdi.Receptor.Domicilio.Estado + ", CP:" + cfdi.Receptor.Domicilio.Cp;
            DrawToPage(page, text, font7, brushBlack, formatLeft, midPage, y, out y);
            DrawToPage(page, "Pais:" + cfdi.Receptor.Domicilio.Pais, font7, brushBlack, formatLeft, midPage, y, out y);

            //Products
            y += sectionSpacing;

            //Prepare data
            String[][] dataSource = new String[cfdi.Conceptos.Count + 1][];
            String headers = "Cant.;Unidad;Clave;Descripción;Valor unitario;Importe";
            int i = 0;
            dataSource[i] = headers.Split(';');
            foreach (Concepto product in cfdi.Conceptos)
            {
                i++;
                String[] content = new String[6];
                content[0] = product.Cantidad.ToString();
                content[1] = product.Unidad;
                content[2] = product.NoIdentificacion;
                content[3] = product.Descripcion;
                content[4] = String.Format("{0:N1}", product.ValorUnitario);
                content[5] = String.Format("{0:C2}", product.Importe);
                dataSource[i] = content;
            }

            //Generate table
            PdfTable productsTable = new PdfTable();
            PdfTableStyle style = new PdfTableStyle()
            {
                BorderPen = new PdfPen(lightBlack, 0.5f),
                CellPadding = 2,
                HeaderSource = PdfHeaderSource.Rows,
                HeaderRowCount = 1,
                ShowHeader = true,
                HeaderStyle = new PdfCellStyle()
                {
                    BackgroundBrush = new PdfSolidBrush(System.Drawing.Color.Black),
                    TextBrush = brushLightGray,
                    StringFormat = formatMiddle
                }
            };
            productsTable.Style = style;

            productsTable.DataSource = dataSource;
            productsTable.Columns[0].Width = 8;
            productsTable.Columns[3].Width = 30;
            foreach (PdfColumn column in productsTable.Columns)
            {
                column.StringFormat = formatLeft;
            }

            PdfLayoutResult result = productsTable.Draw(page, new PointF(leftMargin, y));
            y = y + result.Bounds.Height + 5;

            //Total in letter and number
            page.Canvas.DrawLine(penLightBlack10p, leftMargin, y, pageWidth, y);
            text = "Total con Letra";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, formatLeft, leftMargin, y, out y);
            DrawToPage(page, Conv.Enletras(cfdi.Total.ToString()) + "M.N.", font7, brushBlack, formatLeft, leftMargin, y, out y, true);

            DrawToPageWithHeader(page, "Subtotal:", String.Format("       {0:C2}", cfdi.SubTotal), font7Bold, font7, brushBlack, formatLeft, midPage + (midPage / 2), y, out y);
            DrawToPageWithHeader(page, "Total:", String.Format("            {0:C2}", cfdi.Total), font7Bold, font7, brushBlack, formatLeft, midPage + (midPage / 2), y, out y);

            //QR Code with basic data
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(String.Format("?re={0}&rr={1}&tt={2:N1}&id={3}", cfdi.Emisor.Rfc, cfdi.Receptor.Rfc, cfdi.Total, cfdi.TimbreFiscal.UUID), QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);

            float qrPosition = y;

            PdfImage image = PdfImage.FromImage(qrCodeImage);
            page.Canvas.DrawImage(image, leftMargin, y, qrSize, qrSize);

            //Payment info
            y = qrPosition + sectionSpacing;
            DrawToPageWithHeader(page, "Método de pago:  ", cfdi.MetodoDePago, font7Bold, font7, brushBlack, formatLeft, leftMargin + qrSize, y, out y);
            DrawToPageWithHeader(page, "Cuenta:  ", cfdi.NumCtaPago ?? "", font7Bold, font7, brushBlack, formatLeft, leftMargin + qrSize, y, out y);
            DrawToPageWithHeader(page, "Forma de pago:  ", cfdi.FormaDePago, font7Bold, font7, brushBlack, formatLeft, leftMargin + qrSize, y, out y);
            DrawToPageWithHeader(page, "Condiciones de pago:  ", cfdi.CondicionesDePago ?? "", font7Bold, font7, brushBlack, formatLeft, leftMargin + qrSize, y, out y);

            y = qrPosition + qrSize + sectionSpacing;
            page.Canvas.DrawLine(penLightBlack10p, leftMargin, y, pageWidth, y);
            text = "Cadena original del complemento de certificación del SAT";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, formatLeft, leftMargin, y, out y);
            area = new RectangleF(leftMargin, y, pageWidth - 5, 50);
            DrawToPage(page, String.Format("||{0}|{1}|{2}|{3}|{4}", cfdi.TimbreFiscal.Version, cfdi.TimbreFiscal.UUID, cfdi.TimbreFiscal.FechaTimbrado, cfdi.Sello, cfdi.TimbreFiscal.NoCertificadoSAT), font7Bold, brushBlack, formatLeft, area, y, out y, false);

            y += sectionSpacing;
            page.Canvas.DrawLine(penLightBlack10p, leftMargin, y, pageWidth, y);
            text = "Sello digital del SAT";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, formatLeft, leftMargin, y, out y);
            area = new RectangleF(leftMargin, y, pageWidth - 5, 50);
            DrawToPage(page, cfdi.TimbreFiscal.SelloSAT, font7Bold, brushBlack, formatLeft, area, y, out y, false);

            y += sectionSpacing;
            page.Canvas.DrawLine(penLightBlack10p, leftMargin, y, pageWidth, y);
            text = "Sello digital del contribuyente que lo expide";
            size = font7Bold.MeasureString(text, formatLeft);
            y -= 4;
            DrawToPage(page, text, font7Bold, brushLightGray, formatLeft, leftMargin, y, out y);
            area = new RectangleF(leftMargin, y, pageWidth - 5, 50);
            DrawToPage(page, cfdi.Sello, font7Bold, brushBlack, formatLeft, area, y, out y, false);

            //Footer
            DrawToPage(page, "Este documento es una representación impresa de un CFDI", font7, brushBlack, formatLeft, midPage, page.Canvas.ClientSize.Height - 30, out y, false);
        }

        private static void DrawToPage(PdfPageBase page, string text, PdfTrueTypeFont font, PdfBrush brushColor, PdfStringFormat formatLeft, float x, float y, out float yout)
        {
            DrawToPage(page, text, font, brushColor, formatLeft, x, y, out yout, false);
        }

        private static void DrawToPage(PdfPageBase page, string text, PdfTrueTypeFont font, PdfBrush brushColor, PdfStringFormat formatLeft, float x, float y, out float yout, bool ignoreOut)
        {
            page.Canvas.DrawString(text, font, brushColor, x, y, formatLeft);
            SizeF size = font.MeasureString(text, formatLeft);
            yout = (ignoreOut) ? y : y + size.Height + 2;
        }

        private static void DrawToPage(PdfPageBase page, string text, PdfTrueTypeFont font, PdfBrush brushColor, PdfStringFormat formatLeft, RectangleF area, float y, out float yout, bool ignoreOut)
        {
            page.Canvas.DrawString(text, font, brushColor, area, formatLeft);
            SizeF size = font.MeasureString(text, area.Size, formatLeft);
            yout = (ignoreOut) ? y : y + size.Height + 2;
        }

        private static void DrawToPageWithHeader(PdfPageBase page, string header, string text, PdfTrueTypeFont fontHeader, PdfTrueTypeFont font, PdfBrush brushColor, PdfStringFormat formatLeft, float x, float y, out float yout, bool ignoreOut)
        {
            page.Canvas.DrawString(header, fontHeader, brushColor, x, y, formatLeft);
            SizeF size = font.MeasureString(header, formatLeft);
            page.Canvas.DrawString(text, font, brushColor, x + size.Width + 1, y, formatLeft);
            yout = (ignoreOut) ? y : y + size.Height + 2;
        }

        private static void DrawToPageWithHeader(PdfPageBase page, string header, string text, PdfTrueTypeFont fontHeader, PdfTrueTypeFont font, PdfBrush brushColor, PdfStringFormat formatLeft, float leftMargin, float y, out float yout)
        {
            DrawToPageWithHeader(page, header, text, fontHeader, font, brushColor, formatLeft, leftMargin, y, out yout, false);
        }
    }
}
