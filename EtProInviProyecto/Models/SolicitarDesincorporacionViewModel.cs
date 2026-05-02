using System.ComponentModel.DataAnnotations;

namespace EtPro.Models
{
    public class SolicitarDesincorporacionViewModel
    {
        [Required(ErrorMessage = "El bien es obligatorio")]
        [Display(Name = "Bien a desincorporar")]
        public int BienId { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio")]
        [Display(Name = "Motivo")]
        public string Motivo { get; set; }
    }
}