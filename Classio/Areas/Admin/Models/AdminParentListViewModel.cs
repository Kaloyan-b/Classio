namespace Classio.Areas.Admin.Models
{
    public class AdminParentListViewModel
    {
        public int ParentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LinkedChildrenCount { get; set; }
    }
}
