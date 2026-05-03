
namespace EtPro.Models
{
    public class User 
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public int? DepartmentID { get; set; }
        public ICollection<UserPermission> ActualUserPermissions { get; set; } = new List<UserPermission>();

    }
}
