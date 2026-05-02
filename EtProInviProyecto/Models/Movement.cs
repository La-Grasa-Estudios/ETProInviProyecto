// Models/Movement.cs
using ETPro.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtPro.Models
{
    public enum MovementType { Traspaso, Desincorporacion, Incorporacion }

    public class Movement
    {
        [Key]
        public int Id { get; set; }

        public int BienId { get; set; }
        public BienMueble Bien { get; set; }

        public MovementType Type { get; set; }

        public int? OriginDepartmentId { get; set; }
        public Department? OriginDepartment { get; set; }

        public int? DestinationDepartmentId { get; set; }
        public Department? DestinationDepartment { get; set; }

        [Required]
        public string Motivo { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Estado { get; set; } = "Pendiente";  

        public string UsuarioSolicitanteId { get; set; }
        public User UsuarioSolicitante { get; set; }

        public string? UsuarioAprobadorId { get; set; }
        public User? UsuarioAprobador { get; set; }

        public DateTime? FechaAprobacion { get; set; }
    }
}