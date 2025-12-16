namespace ExamAzure.Services
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
