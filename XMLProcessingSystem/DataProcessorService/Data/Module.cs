namespace DataProcessorService.Data
{
    public class Module
    {
        public Guid Id { get; set; }
        public string ModuleCategoryID { get; set; } = "";
        public string ModuleState { get; set; } = "";
        public int? IndexWithinRole { get; set; } = null;
        public string PackageID { get; set; } = "";
    }
}
