using System.ComponentModel.DataAnnotations;

public class SolicitarTraspasoViewModel
{
    [Required]
    [Display(Name = "Bien a trasladar")]
    public int BienId { get; set; }

    [Required]
    [Display(Name = "Departamento destino")]
    public int? DestinationDepartmentId { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio")]
    public string Motivo { get; set; }
}