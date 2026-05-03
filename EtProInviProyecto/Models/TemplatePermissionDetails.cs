using EtPro.Models;

namespace EtPro.Models
{
    public class TemplatePermissionDetails
    {
        public int TemplateID { get; set; }
        public TemplatePermission Template { get; set; }
        
        public int PermissionID { get; set; }
        public Permission PermissionInstance { get; set; }
    }
}
