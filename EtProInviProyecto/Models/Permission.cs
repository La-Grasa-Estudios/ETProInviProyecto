using EtPro.Models;

namespace EtPro.Models
{
    public class Permission
    {
        public int ID { get; set; }
        public string Name { get; set; }      
        public string Description { get; set; }  
        public string Category { get; set; }   

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

        public ICollection<TemplatePermissionDetails> TemplatePermissionInfo { get; set; } = new List<TemplatePermissionDetails>();
    }
}
