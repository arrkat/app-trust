using Microsoft.AspNetCore.Http;

using Moq;

using AppTrust.Sdk;



namespace AppTrust.Sdk.Tests;



public class InboundTrustStrategyHandlerTests

{

    [Fact]

    public async Task VerifyAsync_WhenAllStrategiesPassWithMatchingCallerIds_ReturnsSuccess()

    {

        // Arrange — Both mode: every strategy must pass and agree on caller ID

        var context = new DefaultHttpContext();

        var first = CreateMockStrategy(context, new TrustResult(true, AppConstants.ClientIdentifier));

        var second = CreateMockStrategy(context, new TrustResult(true, AppConstants.ClientIdentifier));



        var handler = new InboundTrustStrategyHandler([first.Object, second.Object]);



        // Act

        var result = await handler.VerifyAsync(context);



        // Assert

        Assert.True(result.IsValid);

        Assert.Equal(AppConstants.ClientIdentifier, result.CallerId);

        first.Verify(x => x.VerifyAsync(context), Times.Once);

        second.Verify(x => x.VerifyAsync(context), Times.Once);

    }



    [Fact]

    public async Task VerifyAsync_WhenOneStrategyFails_ReturnsInvalid()

    {

        // Arrange

        var context = new DefaultHttpContext();

        var first = CreateMockStrategy(context, new TrustResult(true, AppConstants.ClientIdentifier));

        var second = CreateMockStrategy(context, new TrustResult(false, null));



        var handler = new InboundTrustStrategyHandler([first.Object, second.Object]);



        // Act

        var result = await handler.VerifyAsync(context);



        // Assert — first strategy passes, second fails; handler returns invalid after both run

        Assert.False(result.IsValid);

        Assert.Null(result.CallerId);

        first.Verify(x => x.VerifyAsync(context), Times.Once);

        second.Verify(x => x.VerifyAsync(context), Times.Once);

    }



    [Fact]

    public async Task VerifyAsync_WhenCallerIdsMismatch_ReturnsInvalid()

    {

        // Arrange — both strategies pass individually, but disagree on who called

        var context = new DefaultHttpContext();

        var first = CreateMockStrategy(context, new TrustResult(true, AppConstants.ClientIdentifier));

        var second = CreateMockStrategy(context, new TrustResult(true, "other-caller"));



        var handler = new InboundTrustStrategyHandler([first.Object, second.Object]);



        // Act

        var result = await handler.VerifyAsync(context);



        // Assert

        Assert.False(result.IsValid);

        Assert.Null(result.CallerId);

    }



    [Fact]

    public async Task VerifyAsync_WhenSingleStrategyPasses_ReturnsItsResult()

    {

        // Arrange — JWT-only or mTLS-only mode uses a single strategy

        var context = new DefaultHttpContext();

        var strategy = CreateMockStrategy(context, new TrustResult(true, AppConstants.ClientIdentifier));



        var handler = new InboundTrustStrategyHandler([strategy.Object]);



        // Act

        var result = await handler.VerifyAsync(context);



        // Assert

        Assert.True(result.IsValid);

        Assert.Equal(AppConstants.ClientIdentifier, result.CallerId);

    }



    [Fact]

    public void Constructor_WhenNoStrategiesProvided_Throws()

    {

        // Arrange & Act

        var exception = Assert.Throws<ArgumentException>(() => new InboundTrustStrategyHandler([]));



        // Assert

        Assert.NotNull(exception);

    }



    private static Mock<IInboundTrustStrategy> CreateMockStrategy(HttpContext context, TrustResult result)

    {

        var mock = new Mock<IInboundTrustStrategy>();

        mock.Setup(x => x.VerifyAsync(context)).ReturnsAsync(result);

        return mock;

    }

}

