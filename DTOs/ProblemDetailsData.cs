namespace Learning_Project.DTOs
{
    public class ProblemDetailsData
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
        public string Instance { get; set; }
        public int Status { get; set; }
        public List<ValidationError> ValidationErrors { get; set; }
        public string Timestamp { get; set; }
        public string? TraceId { get; set; }
    }
}
