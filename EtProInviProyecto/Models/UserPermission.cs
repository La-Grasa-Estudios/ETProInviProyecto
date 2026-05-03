namespace EtPro.Models
{
    public class UserPermission
    {
        public string UserID { get; set; }
        public User UserInstance { get; set; }
        
        public int PermissionID { get; set; }
        public Permission Permission { get; set; }
    }
}
