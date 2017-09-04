using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CFDIXMLtoPDFwpf
{
    class Domicilio
    {
        private string calle;
        private string noExterior;
        private string noInterior;
        private string colonia;
        private string localidad;
        private string municipio;
        private string estado;
        private string pais;
        private int cp;

        public Domicilio(XmlNode xnDomicilio)
        {
            Calle = xnDomicilio.Attributes.GetNamedItem("calle").Value;
            NoExterior = xnDomicilio.Attributes.GetNamedItem("noExterior").Value;
            NoInterior = xnDomicilio.Attributes.GetNamedItem("noInterior")?.Value;
            Colonia = xnDomicilio.Attributes.GetNamedItem("colonia").Value;
            Localidad = xnDomicilio.Attributes.GetNamedItem("localidad").Value;
            Municipio = xnDomicilio.Attributes.GetNamedItem("municipio").Value;
            Estado = xnDomicilio.Attributes.GetNamedItem("estado").Value;
            Pais = xnDomicilio.Attributes.GetNamedItem("pais").Value;
            int.TryParse(xnDomicilio.Attributes.GetNamedItem("codigoPostal").Value, out cp);
        }

        public string Calle { get => calle; set => calle = value; }
        public string NoExterior { get => noExterior; set => noExterior = value; }
        public string NoInterior { get => noInterior; set => noInterior = value; }
        public string Colonia { get => colonia; set => colonia = value; }
        public string Localidad { get => localidad; set => localidad = value; }
        public string Municipio { get => municipio; set => municipio = value; }
        public string Estado { get => estado; set => estado = value; }
        public string Pais { get => pais; set => pais = value; }
        public int Cp { get => cp; set => cp = value; }
    }

    class Persona
    {
        private string nombre;
        private string rfc;
        private Domicilio domicilio;
        private string regimenFiscal;

        public Persona (XmlNode xnPersona, XmlNamespaceManager nameSpaceManager)
        {
            Rfc = xnPersona.Attributes.GetNamedItem("rfc").Value;
            Nombre = xnPersona.Attributes.GetNamedItem("nombre").Value;
            XmlNode xnDomicilio = xnPersona.SelectSingleNode("cfdi:Domicilio", nsmgr: nameSpaceManager);
            if (xnDomicilio == null)
            {
                xnDomicilio = xnPersona.SelectSingleNode("cfdi:DomicilioFiscal", nsmgr: nameSpaceManager);
            }
            Domicilio = new Domicilio(xnDomicilio);
            XmlNode xnRegimenFiscal = xnPersona.SelectSingleNode("cfdi:RegimenFiscal", nsmgr: nameSpaceManager);
            if(xnRegimenFiscal != null)
            {
                RegimenFiscal = xnRegimenFiscal.Attributes.GetNamedItem("Regimen").Value;
            }
        }

        public string Nombre { get => nombre; set => nombre = value; }
        public string Rfc { get => rfc; set => rfc = value; }
        public string RegimenFiscal { get => regimenFiscal; set => regimenFiscal = value; }
        internal Domicilio Domicilio { get => domicilio; set => domicilio = value; }
    }

    class Concepto
    {
        private int cantidad;
        private string unidad;
        private string noIdentificacion;
        private string descripcion;
        private float valorUnitario;
        private float importe;

        public Concepto(XmlNode xnConcepto)
        {
            int.TryParse(xnConcepto.Attributes.GetNamedItem("cantidad").Value, out cantidad);
            Unidad = xnConcepto.Attributes.GetNamedItem("unidad").Value;
            NoIdentificacion = xnConcepto.Attributes.GetNamedItem("noIdentificacion").Value;
            Descripcion = xnConcepto.Attributes.GetNamedItem("descripcion").Value;
            float.TryParse(xnConcepto.Attributes.GetNamedItem("valorUnitario").Value, out valorUnitario);
            float.TryParse(xnConcepto.Attributes.GetNamedItem("importe").Value, out importe);
        }

        public int Cantidad { get => cantidad; set => cantidad = value; }
        public string Unidad { get => unidad; set => unidad = value; }
        public string NoIdentificacion { get => noIdentificacion; set => noIdentificacion = value; }
        public string Descripcion { get => descripcion; set => descripcion = value; }
        public float ValorUnitario { get => valorUnitario; set => valorUnitario = value; }
        public float Importe { get => importe; set => importe = value; }
    }

    class Traslado
    {
        private string impuesto;
        private float tasa;
        private float importe;

        public Traslado(XmlNode xnTraslado)
        {
            Impuesto = xnTraslado.Attributes.GetNamedItem("impuesto").Value;
            float.TryParse(xnTraslado.Attributes.GetNamedItem("tasa").Value, out tasa);
            float.TryParse(xnTraslado.Attributes.GetNamedItem("importe").Value, out importe);
        }

        public string Impuesto { get => impuesto; set => impuesto = value; }
        public float Tasa { get => tasa; set => tasa = value; }
        public float Importe { get => importe; set => importe = value; }
    }

    class Impuestos
    {
        private List<Traslado> traslados= new List<Traslado>();
        private float totalImpuestosTrasladados;

        public Impuestos(XmlNode xnImpuestos, XmlNamespaceManager nameSpaceManager)
        {
            if (xnImpuestos.InnerXml.Length == 0)
            {
                return;
            }
            float.TryParse(xnImpuestos.Attributes.GetNamedItem("totalImpuestosTrasladados").Value, out totalImpuestosTrasladados);
            foreach (XmlNode xnTraslado in xnImpuestos.SelectNodes("//cfdi:Traslado", nameSpaceManager))
            {
                traslados.Add(new Traslado(xnTraslado));
            }
        }

        public float TotalImpuestosTrasladados { get => totalImpuestosTrasladados; set => totalImpuestosTrasladados = value; }
        internal List<Traslado> Traslados { get => traslados; set => traslados = value; }
    }

    class TimbreFiscalDigital
    {
        private string selloCFD;
        private string fechaTimbrado;
        private string uuid;
        private string noCertificadoSAT;
        private string version;
        private string selloSAT;

        public TimbreFiscalDigital(XmlNode xnTimbreFiscalDigital, XmlNamespaceManager nameSpaceManager)
        {
            //Desgined for version 1.0
            SelloCFD = xnTimbreFiscalDigital.Attributes.GetNamedItem("selloCFD").Value;
            FechaTimbrado = xnTimbreFiscalDigital.Attributes.GetNamedItem("FechaTimbrado").Value;
            UUID = xnTimbreFiscalDigital.Attributes.GetNamedItem("UUID").Value;
            NoCertificadoSAT = xnTimbreFiscalDigital.Attributes.GetNamedItem("noCertificadoSAT").Value;
            Version = xnTimbreFiscalDigital.Attributes.GetNamedItem("version").Value;
            SelloSAT = xnTimbreFiscalDigital.Attributes.GetNamedItem("selloSAT").Value;
        }

        public string SelloCFD { get => selloCFD; set => selloCFD = value; }
        public string FechaTimbrado { get => fechaTimbrado; set => fechaTimbrado = value; }
        public string UUID { get => uuid; set => uuid = value; }
        public string NoCertificadoSAT { get => noCertificadoSAT; set => noCertificadoSAT = value; }
        public string Version { get => version; set => version = value; }
        public string SelloSAT { get => selloSAT; set => selloSAT = value; }
    }

    public class CFDIXML
    {
        private string xmlFilename;
        private Persona emisor, receptor;
        private List<Concepto> conceptos = new List<Concepto>();
        private TimbreFiscalDigital timbreFiscal;
        private Impuestos impuestos;
        private string version;
        private string serie;
        private string folio;
        private string fecha;
        private string sello;
        private string formaDePago;
        private string noCertificado;
        private string certificado;
        private string condicionesDePago;
        private float subTotal;
        private float tipoCambio;
        private string moneda;
        private float total;
        private string tipoDeComprobante;
        private string metodoDePago;
        private string lugarExpedicion;
        private string numCtaPago;
        
        public CFDIXML(String filename)
        {
            xmlFilename = filename;
        }

        public string Version { get => version; set => version = value; }
        public string Serie { get => serie; set => serie = value; }
        public string Folio { get => folio; set => folio = value; }
        public string Fecha { get => fecha; set => fecha = value; }
        public string Sello { get => sello; set => sello = value; }
        public string FormaDePago { get => formaDePago; set => formaDePago = value; }
        public string NoCertificado { get => noCertificado; set => noCertificado = value; }
        public string Certificado { get => certificado; set => certificado = value; }
        public string CondicionesDePago { get => condicionesDePago; set => condicionesDePago = value; }
        public float SubTotal { get => subTotal; set => subTotal = value; }
        public float TipoCambio { get => tipoCambio; set => tipoCambio = value; }
        public string Moneda { get => moneda; set => moneda = value; }
        public float Total { get => total; set => total = value; }
        public string TipoDeComprobante { get => tipoDeComprobante; set => tipoDeComprobante = value; }
        public string MetodoDePago { get => metodoDePago; set => metodoDePago = value; }
        public string LugarExpedicion { get => lugarExpedicion; set => lugarExpedicion = value; }
        public string NumCtaPago { get => numCtaPago; set => numCtaPago = value; }
        internal Persona Emisor { get => emisor; set => emisor = value; }
        internal Persona Receptor { get => receptor; set => receptor = value; }
        internal List<Concepto> Conceptos { get => conceptos; set => conceptos = value; }
        internal TimbreFiscalDigital TimbreFiscal { get => timbreFiscal; set => timbreFiscal = value; }
        internal Impuestos Impuestos { get => impuestos; set => impuestos = value; }

        public CFDIXML LoadFile()
        {
            //Designed for version 3.2
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilename);

            XmlNamespaceManager nameSpaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nameSpaceManager.AddNamespace("cfdi", "http://www.sat.gob.mx/cfd/3");
            nameSpaceManager.AddNamespace("tfd", "http://www.sat.gob.mx/TimbreFiscalDigital");

            XmlNode xnComprobante = xmlDoc.SelectSingleNode("//cfdi:Comprobante", nameSpaceManager);
            if(xnComprobante == null) { return null; }
            Version = xnComprobante.Attributes.GetNamedItem("version").Value;
            Serie = xnComprobante.Attributes.GetNamedItem("serie")?.Value;
            Folio = xnComprobante.Attributes.GetNamedItem("folio").Value;
            Fecha = xnComprobante.Attributes.GetNamedItem("fecha").Value;
            Sello = xnComprobante.Attributes.GetNamedItem("sello").Value;
            FormaDePago = xnComprobante.Attributes.GetNamedItem("formaDePago").Value;
            NoCertificado = xnComprobante.Attributes.GetNamedItem("noCertificado").Value;
            Certificado = xnComprobante.Attributes.GetNamedItem("certificado").Value;
            CondicionesDePago = xnComprobante.Attributes.GetNamedItem("condicionesDePago")?.Value;
            float.TryParse(xnComprobante.Attributes.GetNamedItem("subTotal").Value, out subTotal);
            float.TryParse(xnComprobante.Attributes.GetNamedItem("TipoCambio").Value, out tipoCambio);
            Moneda = xnComprobante.Attributes.GetNamedItem("Moneda").Value;
            float.TryParse(xnComprobante.Attributes.GetNamedItem("total").Value, out total);
            TipoDeComprobante = xnComprobante.Attributes.GetNamedItem("tipoDeComprobante").Value;
            MetodoDePago = xnComprobante.Attributes.GetNamedItem("metodoDePago").Value;
            LugarExpedicion = xnComprobante.Attributes.GetNamedItem("LugarExpedicion").Value;
            NumCtaPago = xnComprobante.Attributes.GetNamedItem("NumCtaPago")?.Value;

            Emisor = new Persona(xmlDoc.SelectSingleNode("//cfdi:Emisor", nameSpaceManager), nameSpaceManager);
            Receptor = new Persona(xmlDoc.SelectSingleNode("//cfdi:Receptor", nameSpaceManager), nameSpaceManager);

            XmlNodeList concepts = xmlDoc.SelectNodes("//cfdi:Concepto",nameSpaceManager);
            foreach (XmlNode concepto in concepts)
            {
                conceptos.Add(new Concepto(concepto));
            }

            impuestos = new Impuestos(xmlDoc.SelectSingleNode("//cfdi:Impuestos", nameSpaceManager), nameSpaceManager);

            timbreFiscal = new TimbreFiscalDigital(xmlDoc.SelectSingleNode("//tfd:TimbreFiscalDigital", nameSpaceManager), nameSpaceManager);
            return this;
        }


    }
}
