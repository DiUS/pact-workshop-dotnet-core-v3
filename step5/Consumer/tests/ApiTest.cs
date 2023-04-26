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
using System.Threading.Tasks;

namespace tests
{
    public class ApiTest
    {
        private IPactBuilderV3 pact;
        private readonly ApiClient ApiClient;
        private readonly int port = 9000;
        private readonly List<object> products;

        public ApiTest(ITestOutputHelper output)
        {
            products = new List<object>()
            {
                new { id = 9, type = "CREDIT_CARD", name = "GEM Visa", version = "v2" },
                new { id = 10, type = "CREDIT_CARD", name = "28 Degrees", version = "v1" }
            };

            var Config = new PactConfig
            {
                PactDir = Path.Join("..", "..", "..", "..", "..", "pacts"),
                Outputters = new[] { new XUnitOutput(output) },
                DefaultJsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                },
                LogLevel = PactLogLevel.Debug // STEP_8
            };

            pact = Pact.V3("ApiClient", "ProductService", Config).WithHttpInteractions(port);
            ApiClient = new ApiClient(new System.Uri($"http://localhost:{port}"));
        }

        [Fact]
        public async Task GetAllProducts()
        {
            // Arrange
            pact.UponReceiving("A valid request for all products")
                    .Given("products exist")
                    .WithRequest(HttpMethod.Get, "/api/products")
                    // .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z")) // STEP_8
                .WillRespond()
                    .WithStatus(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json; charset=utf-8")
                    .WithJsonBody(new TypeMatcher(products));

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetAllProducts();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        [Fact]
        public async Task GetProduct()
        {
            // Arrange
            pact.UponReceiving("A valid request for a product")
                    .Given("product with ID 10 exists")
                    // .WithRequest(HttpMethod.Get, "/api/product/10") // STEP_1 - STEP_4
                    .WithRequest(HttpMethod.Get, "/api/products/10") // STEP_5
                    // .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z")) // STEP_8
                .WillRespond()
                    .WithStatus(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json; charset=utf-8")
                    .WithJsonBody(new TypeMatcher(products[1]));

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetProduct(10);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        
        // // STEP_6
        // [Fact]
        // public async Task NoProductsExist()
        // {
        //     // Arrange
        //     pact.UponReceiving("A valid request for all products")
        //             .Given("no products exist")
        //             .WithRequest(HttpMethod.Get, "/api/products")
        //             // .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))  // STEP_8
        //         .WillRespond()
        //             .WithStatus(HttpStatusCode.OK)
        //             .WithHeader("Content-Type", "application/json; charset=utf-8")
        //             .WithJsonBody(new TypeMatcher(new List<object>()));

        //     await pact.VerifyAsync(async ctx => {
        //         var response = await ApiClient.GetAllProducts();
        //         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        //     });
        // }

        // // STEP_6
        // [Fact]
        // public async Task ProductDoesNotExist()
        // {
        //     // Arrange
        //     pact.UponReceiving("A valid request for a product")
        //             .Given("product with ID 11 does not exist")
        //             .WithRequest(HttpMethod.Get, "/api/products/11")
        //             // .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))  // STEP_8
        //         .WillRespond()
        //             .WithStatus(HttpStatusCode.NotFound);

        //     await pact.VerifyAsync(async ctx => {
        //         var response = await ApiClient.GetProduct(11);
        //         Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        //     });
        // }

        // // STEP_8
        // [Fact]
        // public async Task GetProductMissingAuthHeader()
        // {
        //     // Arrange
        //     pact.UponReceiving("A valid request for a product")
        //             .Given("No auth token is provided")
        //             .WithRequest(HttpMethod.Get, "/api/products/10")
        //         .WillRespond()
        //             .WithStatus(HttpStatusCode.Unauthorized);

        //     await pact.VerifyAsync(async ctx => {
        //         var response = await ApiClient.GetProduct(10);
        //         Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        //     });
        // }
    }
}