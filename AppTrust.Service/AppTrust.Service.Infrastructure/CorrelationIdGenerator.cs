using AppTrust.Service.Contracts;

namespace AppTrust.Service.Infrastructure;

public sealed class CorrelationIdGenerator : ICorrelationIdGenerator
{
    public Guid Generate() => Guid.NewGuid();
}
