
namespace ETPro.Models
{
    public class User 
    {
        public string ID { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public int? DepartmentID { get; set; }
        public ICollection<UserPermission> ActualUserPermissions { get; set; } = new List<UserPermission>();

    }
}
