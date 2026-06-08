using AppTrust.Service.Infrastructure;

namespace AppTrust.Service.Tests;

public class CorrelationIdGeneratorTests
{
    [Fact]
    public void Generate_ReturnsUniqueNonEmptyGuids()
    {
        // Arrange
        var generator = new CorrelationIdGenerator();

        // Act
        var first = generator.Generate();
        var second = generator.Generate();

        // Assert
        Assert.NotEqual(Guid.Empty, first);
        Assert.NotEqual(Guid.Empty, second);
        Assert.NotEqual(first, second);
    }
}
