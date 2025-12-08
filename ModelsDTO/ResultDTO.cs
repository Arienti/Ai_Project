namespace ModelsDTO
{
    public class ResultDTO
    {
        public object? Data { get; set; }

        public List<string> Errors { get; private set; } = new();

        public bool IsSuccess => Errors.Count == 0;

        public string ErrorMessages { get { return string.Join("; ", Errors); } }

        public void AddError(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Errors.Add(message);
            }
        }

        public static ResultDTO Success(object? data = default)
        {
            return new ResultDTO
            {
                Data = data
            };
        }

        public static ResultDTO Fail(string? errorMessage = null)
        {
            var result = new ResultDTO();
            result.AddError(errorMessage);
            return result;
        }
    }
}