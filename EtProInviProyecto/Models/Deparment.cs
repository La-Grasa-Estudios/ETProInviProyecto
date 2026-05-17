using EtPro.Models;
using System.ComponentModel.DataAnnotations;

namespace EtPro.Models
{
    public class Department
    {
        [Key]
        public int ID { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string? ManagerID { get; set; }
        public User? Manager { get; set; }

        public string? CustodianID { get; set; }
        public User? Custodian { get; set; }

        public int? ParentDepartmentID { get; set; }
        public Department? ParentDepartment { get; set; }
        public ICollection<Department> ChildDepartments { get; set; } = new List<Department>();
    }
}