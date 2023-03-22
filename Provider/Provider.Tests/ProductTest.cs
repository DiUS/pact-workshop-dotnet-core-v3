using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;

namespace Provider.Tests;

public class ProductTest
{
    private const string PactServiceUri = "http://127.0.0.1:5234";
    private readonly ITestOutputHelper _output;

    public ProductTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task EnsureProviderApiHonoursPactWithConsumer()
    {
        // Arrange
        var config = new PactVerifierConfig
        {
            Outputters = new List<IOutput>
            {
                new XunitOutput(_output)
            }
        };

        await using var app = Startup.WebApp();
        app.Urls.Add(PactServiceUri);
        
        await app.StartAsync();

        // Act / Assert
        using var verifier = new PactVerifier(config);
        var pact = new FileInfo(Path.Join("..", "..", "..", "..", "..", "pacts", "ApiClient-ProductService.json"));
        verifier.ServiceProvider("ProductService", new Uri(PactServiceUri))
            .WithFileSource(pact)
            .Verify();
    }
}