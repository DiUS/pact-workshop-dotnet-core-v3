using System.IO;
using PactNet;
using Xunit.Abstractions;
using Xunit;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Consumer;
using PactNet.Matchers;
using PactNet.Output.Xunit;

namespace Consumer.Tests;

public class ApiTest
{
    private readonly IPactBuilderV3 _pact;
    private readonly List<object> _products = new()
    {
        new { id = 9, type = "CREDIT_CARD", name = "GEM Visa", version = "v2" },
        new { id = 10, type = "CREDIT_CARD", name = "28 Degrees", version = "v1" }
    };

    public ApiTest(ITestOutputHelper output)
    {
        var config = new PactConfig
        {
            PactDir = Path.Join("..", "..", "..", "..", "..", "pacts"),
            Outputters = new[] { new XunitOutput(output) },
            DefaultJsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }
        };

        _pact = Pact.V3("ApiClient", "ProductService", config)
            .WithHttpInteractions(8513);
    }

    [Fact]
    public async void GetAllProducts()
    {
        // Arrange
        _pact.UponReceiving("A valid request for all products")
            .Given("products exist")
            .WithRequest(HttpMethod.Get, "/api/products")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new TypeMatcher(_products));

        await _pact.VerifyAsync(async ctx => {
            var response = await new ApiClient(ctx.MockServerUri).GetAllProducts();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }

    [Fact]
    public async void GetProduct()
    {
        // Arrange
        _pact.UponReceiving("A valid request for a product")
            .Given("product with ID 10 exists")
            .WithRequest(HttpMethod.Get, "/api/product/10")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new TypeMatcher(_products[1]));

        await _pact.VerifyAsync(async ctx => {
            var response = await new ApiClient(ctx.MockServerUri).GetProduct(10);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
    }
}