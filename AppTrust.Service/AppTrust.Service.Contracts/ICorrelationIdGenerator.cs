namespace AppTrust.Service.Contracts;

public interface ICorrelationIdGenerator
{
    Guid Generate();
}
