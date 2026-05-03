using EtPro.Models;

namespace EtPro.Models
{
    public class TemplatePermission
    {
        public int Id { get; set; }
        public string Name { get; set; }       
        public string Description { get; set; }
        public bool Editable { get; set; } = true;
        
        public ICollection<TemplatePermissionDetails> Details { get; set; } = new List<TemplatePermissionDetails>();
    }
}
