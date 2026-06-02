namespace AppB.Contracts;

public interface ISessionService
{
    Guid CreateSession(string callerId);
}
