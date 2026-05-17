using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtPro.Models
{
    public class SolicitarIncorporacionViewModel
    {
        [Required(ErrorMessage = "El número de identificación es obligatorio")]
        [MaxLength(50)]
        public string NumeroIdentificacion { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(50)] public string? Marca { get; set; }
        [MaxLength(50)] public string? Modelo { get; set; }
        [MaxLength(100)] public string? Serial { get; set; }
        [MaxLength(50)] public string? Color { get; set; }
        [MaxLength(50)] public string? Material { get; set; }
        [MaxLength(150)] public string? ObservacionesAdicionales { get; set; }

        [MaxLength(10)]
        public string? Subgrupo { get; set; }

        [MaxLength(10)]
        public string? Seccion { get; set; }

        public int Grupo { get; set; } = 2;

        [Required(ErrorMessage = "Debe seleccionar un departamento")]
        [Display(Name = "Departamento")]
        public int DependenciaID { get; set; }

        [Required(ErrorMessage = "El valor unitario es obligatorio")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Valor Unitario")]
        public decimal ValorUnitario { get; set; }
    }
}