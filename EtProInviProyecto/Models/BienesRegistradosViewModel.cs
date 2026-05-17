using EtPro.Models;

namespace EtProInviProyecto.Models
{
    public class BienesRegistradosViewModel
    {
        public int TotalBienes { get; set; }
        public int TotalActivos { get; set; }
        public int TotalMantenimiento { get; set; }
        public int TotalDesincorporados { get; set; }
        public List<Movement> MovimientosRecientes { get; set; } = new List<Movement>();

        public int SolicitudesPendientes { get; set; }

        public int CustodiosACargo { get; set; }   
        public string GerenteEncargado { get; set; } 
    }
}
