namespace EtPro.Models
{
    public class EtiquetaViewModel
    {
        public int BienId { get; set; }
        public string Grupo { get; set; }
        public string Subgrupo { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string Ubicacion { get; set; }
        public string Anio { get; set; }
        public string QrBase64 { get; set; }
    }
}