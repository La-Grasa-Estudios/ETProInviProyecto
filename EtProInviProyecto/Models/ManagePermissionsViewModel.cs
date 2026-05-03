namespace EtPro.Models
{
    public class ManagePermissionsViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<PermissionCheck> Permissions { get; set; }
        public List<TemplateOption> Templates { get; set; }
        public int? SelectedTemplateId { get; set; }
    }

    public class PermissionCheck
    {
        public int PermissionId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool Assigned { get; set; }
    }

    public class TemplateOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}