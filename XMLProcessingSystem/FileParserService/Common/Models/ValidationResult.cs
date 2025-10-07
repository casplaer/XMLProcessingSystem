namespace FileParserService.Common.Models
{
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; init; } = new();
        public string Message => IsValid ? "Validation successful" : string.Join("; ", Errors);

        public static ValidationResult Success() => new();
        public static ValidationResult Failure(string error) => new() { Errors = { error } };
        public static ValidationResult Failure(IEnumerable<string> errors) => new() { Errors = errors.ToList() };
    }
}
