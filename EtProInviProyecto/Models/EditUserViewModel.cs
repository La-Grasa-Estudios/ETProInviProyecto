using System.ComponentModel.DataAnnotations;

namespace EtPro.Models
{
    public class EditUserViewModel
    {
        public string ID { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [Display(Name = "Usuario")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del usuario")]
        public string FullName { get; set; }

        [Display(Name = "Departamento")]
        public int? DepartmentID { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña (dejar en blanco para no cambiar)")]
        public string? NewPassword { get; set; }
    }
}