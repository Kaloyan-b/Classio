namespace Classio.Areas.Parent.Models
{
    public class ParentDashboardViewModel
    {
        public string ParentName { get; set; } = string.Empty;
        public List<ChildSummaryViewModel> Children { get; set; } = new List<ChildSummaryViewModel>();
    }
    public class ChildSummaryViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public double OverallAverage { get; set; }
        public double WeightedAbsences { get; set; }
    }
}
