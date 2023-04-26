# Pact .NET Core Workshop

When writing a lot of small services, testing the interactions between these becomes a major headache.
That's the problem Pact is trying to solve.

Integration tests typically are slow and brittle, requiring each component to have its own environment to run the tests in.
With a micro-service architecture, this becomes even more of a problem. They also have to be 'all-knowing' and this makes them
difficult to keep from being fragile.

After J. B. Rainsberger's talk [Integrated Tests Are A Scam](https://www.youtube.com/watch?v=VDfX44fZoMc) people have been thinking
how to get the confidence we need to deploy our software to production without having a tiresome integration test suite that does
not give us all the coverage we think it does.

PactNet is a .NET implementation of Pact that allows you to define a pact between service consumers and providers. It provides a DSL for
service consumers to define the request they will make to a service producer and the response they expect back. This expectation is
used in the consumer's specs to provide a mock producer and is also played back in the producer specs to ensure the producer actually
does provide the response that the consumer expects.

This allows you to test both sides of an integration point using fast unit tests.

## Introduction

This workshop is aimed at demonstrating core features and benefits of contract testing with Pact.

Whilst contract testing can be applied retrospectively to systems, we will follow the [consumer driven contracts](https://martinfowler.com/articles/consumerDrivenContracts.html) approach in this workshop - where a new consumer and provider are created in parallel to evolve a service over time, especially where there is some uncertainty with what is to be built.

This workshop should take from 1 to 2 hours, depending on how deep you want to go into each topic. It's based on the [Javascript version of the same workshop](https://github.com/pact-foundation/pact-workshop-js)

**Workshop outline**:

- [Pact .NET Core Workshop](#pact-net-core-workshop)
  - [Introduction](#introduction)
  - [Learning objectives](#learning-objectives)
  - [Prerequisites](#prerequisites)
  - [Workshop Steps](#workshop-steps)
  - [Preamble: Clone repository and explore](#preamble-clone-repository-and-explore)
    - [Consumer](#consumer)
    - [Provider](#provider)
  - [Step 1: Simple consumer calling provider](#step-1-simple-consumer-calling-provider)
  - [Step 2 - Integration problems...](#step-2---integration-problems)
    - [Run the Consumer](#run-the-consumer)
  - [Step 3 - Pact to the rescue](#step-3---pact-to-the-rescue)
  - [Step 4 - Verify the provider](#step-4---verify-the-provider)
  - [Step 5 - Back to the client we go](#step-5---back-to-the-client-we-go)
  - [Step 6 - Consumer updates contract for missing products](#step-6---consumer-updates-contract-for-missing-products)
  - [Step 7 - Adding the missing states](#step-7---adding-the-missing-states)
  - [Step 8 - Authorization](#step-8---authorization)
  - [Step 9 - Implement authorization on the provider](#step-9---implement-authorization-on-the-provider)
  - [Step 10 - Request Filters on the Provider](#step-10---request-filters-on-the-provider)

## Learning objectives

If running this as a team workshop format, you may want to take a look through the [learning objectives](./LEARNING.md).

## Prerequisites

This workshop while written with .NET Core is not specifically about it so in-depth knowledge of .NET Core is not required if you can
write code in any other language you should be fine.

However before taking part in this workshop please make sure you have:

* [.NET Core SDK](https://www.microsoft.com/net/download/). Make sure you pick version 6.0 for this.
* An account at [Github.com](www.github.com)!
* A text editor/IDE that supports .NET Core. Check out [VSCode](https://code.visualstudio.com/)

## Workshop Steps

## Preamble: Clone repository and explore

Clone [pact-workshop-dotnet-core-v3](https://github.com/dius/pact-workshop-dotnet-core-v3) and familiarise yourself with
its contents. There are two components in scope for our workshop.

1. Product client. A command line client that queries the Product service for product information.
2. Product Service (Provider). Provides useful things about products, such as listing all products and getting the details of an individual product.

The two components can be found in their respective folders and each have a solution (`.sln`) file and project files for the app and test projects (`.csproj`)

### Consumer

A consumer project in the [**Consumer/src**](Consumer/src) folder which is a simple .NET Core console application that connects to the
Provider project

### Provider

A provider in the [**Provider/src**](Provider/src) folder and is an ASP.NET Core Web API. Both projects also have a **tests/** folder which
is where the [Pact](https://docs.pact.io/) tests for both projects exist. ([**Consumer/tests**](Consumer/tests) / [**Consumer/tests**](Consumer/tests))

## Step 1: Simple consumer calling provider

We need to first create an HTTP client to make the calls to our provider service:

![Simple Consumer](diagrams/workshop_step1.svg)

The Consumer has implemented the product service client which has the following:

- `GET /products` - Retrieve all products
- `GET /products/{id}` - Retrieve a single product by ID

The diagram below highlights the interaction for retrieving a product with ID 10:

![Sequence Diagram](diagrams/workshop_step1_class-sequence-diagram.svg)

You can see the client interface we created in [`Consumer/src/ApiClient.cs`](Consumer/src/ApiClient.cs):

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Consumer
{
    public class ApiClient
    {
        private readonly Uri BaseUri;

        public ApiClient(Uri baseUri)
        {
            this.BaseUri = baseUri;
        }

        public async Task<HttpResponseMessage> GetAllProducts()
        {
            using (var client = new HttpClient { BaseAddress = BaseUri })
            {
                try
                {
                    var response = await client.GetAsync($"/api/products");
                    return response;
                }
                catch (Exception ex)
                {
                    throw new Exception("There was a problem connecting to Products API.", ex);
                }
            }
        }

        public async Task<HttpResponseMessage> GetProduct(int id)
        {
            using (var client = new HttpClient { BaseAddress = BaseUri })
            {
                try
                {
                    var response = await client.GetAsync($"/api/product/{id}");
                    return response;
                }
                catch (Exception ex)
                {
                    throw new Exception("There was a problem connecting to Products API.", ex);
                }
            }
        }
    }
}
```

After forking or cloning the repository, we may want to install the dependencies:

1. `cd Consumer\src`
2. `dotnet restore`

We can run the client with `dotnet run` - it should fail with the error below, because the Provider is not running.

![Failed step1 page](diagrams/workshop_step1_failed.png)

## Step 2 - Integration problems...

our provider team has started building out their API in parallel. Let's run our website against our provider (you'll need two terminals to do this):

### Start the Provider

in `Provider/src` run `dotnet run`

```console
❯dotnet run
Hosting environment: Development
Content root path: /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src
Now listening on: http://localhost:9001
Application started. Press Ctrl+C to shut down.
```

### Run the Consumer

in `Consumer/src` run `dotnet run`

```console
❯ dotnet run 
**Retrieving product list**
Response.Code=OK, Response.Body=[
  {
    "id": 9,
    "name": "GEM Visa",
    "type": "CREDIT_CARD",
    "version": "v2"
  },
  {
    "id": 10,
    "name": "28 Degrees",
    "type": "CREDIT_CARD",
    "version": "v1"
  }
]


**Retrieving product with id=10
Response.Code=NotFound, Response.Body=
```

So we're able to retrieve a list of products but not a specific product even though one exists with id=10. We're getting a *404* response every time we attempt to access an individual product.

We need to have a conversation about what the endpoint should be!

## Step 3 - Pact to the rescue

Unit tests are written and executed in isolation of any other services. When we write tests for code that talk to other services, they are built on trust that the contracts are upheld. There is no way to validate that the consumer and provider can communicate correctly.

> An integration contract test is a test at the boundary of an external service verifying that it meets the contract expected by a consuming service — [Martin Fowler](https://martinfowler.com/bliki/IntegrationContractTest.html)

Adding contract tests via Pact would have highlighted the `/product/{id}` endpoint was incorrect.

Let us add Pact to the project and write a consumer pact test for the `GET /product/{id}` endpoint.

*Provider states* is an important concept of Pact that we need to introduce. These states help define the state that the provider should be in for specific interactions. For the moment, we will initially be testing the following states:

- `product with ID 10 exists`
- `products exist`

The consumer can define the state of an interaction using the `given` property.

The file can be found at  [Consumer/tests/ApiTest.cs](Consumer/tests/ApiTest.cs)

Note how similar it looks to a unit test:

```csharp
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
                }
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
                    .WithRequest(HttpMethod.Get, "/api/product/10")
                .WillRespond()
                    .WithStatus(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json; charset=utf-8")
                    .WithJsonBody(new TypeMatcher(products[1]));

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetProduct(10);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }
    }
}
```

![Test using Pact](diagrams/workshop_step3_pact.svg)

This test starts a mock server on a specificed port (9000 here) that acts as our provider service.

Running this test still passes, but it creates a pact file which we can use to validate our assumptions on the provider side, and have conversation around.

```console
$ ❯ dotnet test
  Determining projects to restore...
  Restored /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/tests.csproj (in 317 ms).
  1 of 2 projects are up-to-date for restore.
  consumer -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/net6.0/consumer.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 2 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (net6.0)
```

A pact file should have been generated in [*pacts/ApiClient-ProductService.json*](pacts/ApiClient-ProductService.json)

*NOTE*: even if the API client had been graciously provided for us by our Provider Team, it doesn't mean that we shouldn't write contract tests - because the version of the client we have may not always be in sync with the deployed API - and also because we will write tests on the output appropriate to our specific needs.

## Step 4 - Verify the provider

Now let's make a start on writing Pact tests to validate the consumer contract:

In [`provider/tests/ProductTest.cs`](provider/tests/ProductTest.cs):

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;

namespace tests
{
    public class ProductTest
    {
        private string _pactServiceUri = "http://127.0.0.1:9001";
        private readonly ITestOutputHelper _output;

        public ProductTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EnsureProviderApiHonoursPactWithConsumer()
        {
            // Arrange
            var config = new PactVerifierConfig
            {
                Outputters = new List<IOutput>
                {
                    new XunitOutput(_output)
                }
            };

            using (var _webHost = WebHost.CreateDefaultBuilder().UseStartup<TestStartup>().UseUrls(_pactServiceUri).Build())
            {
                _webHost.Start();

                //Act / Assert
                IPactVerifier pactVerifier = new PactVerifier(config);
                var pactFile = new FileInfo(Path.Join("..", "..", "..", "..", "..", "pacts", "ApiClient-ProductService.json"));
                pactVerifier.ServiceProvider("ProductService", new Uri(_pactServiceUri))
                .WithFileSource(pactFile)
                .WithProviderStateUrl(new Uri($"{_pactServiceUri}/provider-states"))
                .Verify();
            }
        }
    }
}
```

We now need to validate the pact generated by the consumer is valid, by executing it against the running service provider, which should fail:

```console
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/net6.0/provider.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService

  A valid request for a product
     Given product with ID 10 exists
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)

  A valid request for all products
     Given products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)


Failures:

1) Verifying a pact between ApiClient and ProductService Given product with ID 10 exists - A valid request for a product
    1.1) has a matching body
           / -> Expected body Present(65 bytes) but was empty
    1.2) has status code 200
           expected 200 but was 404
    1.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''

There were 1 pact failures

[xUnit.net 00:00:01.26]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [824 ms]
  Error Message:
   PactNet.Exceptions.PactFailureException : Pact verification failed
  Stack Trace:
     at PactNet.Verifier.InteropVerifierProvider.Execute()
   at PactNet.Verifier.PactVerifierSource.Verify()
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 43
  Standard Output Messages:
 Starting verification...
 Pact verification failed
 
 Verifier Output
 ---------------
 
 Verifying a pact between ApiClient and ProductService
 
   A valid request for a product
      Given product with ID 10 exists
     returns a response which
       has status code 200 (FAILED)
       includes headers
         "Content-Type" with value "application/json; charset=utf-8" (FAILED)
       has a matching body (FAILED)
 
   A valid request for all products
      Given products exist
     returns a response which
       has status code 200 (OK)
       includes headers
         "Content-Type" with value "application/json; charset=utf-8" (OK)
       has a matching body (OK)
 
 
 Failures:
 
 1) Verifying a pact between ApiClient and ProductService Given product with ID 10 exists - A valid request for a product
     1.1) has a matching body
            / -> Expected body Present(65 bytes) but was empty
     1.2) has status code 200
            expected 200 but was 404
     1.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
            Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
 
 There were 1 pact failures
 
 Verifier Logs
 -------------
 2023-04-25T21:07:18.025217Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'product with ID 10 exists' for 'A valid request for a product'
 2023-04-25T21:07:18.196409Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for a product'
 2023-04-25T21:07:18.197103Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:07:18.197104Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/product/10, query: None, headers: None, body: Missing )
 2023-04-25T21:07:18.208601Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 404, headers: Some({"content-length": ["0"], "server": ["Kestrel"], "date": ["Tue", "25 Apr 2023 21:07:17 GMT"]}), body: Empty )
 2023-04-25T21:07:18.208817Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(65 bytes) )
 2023-04-25T21:07:18.360650Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'products exist' for 'A valid request for all products'
 2023-04-25T21:07:18.509190Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for all products'
 2023-04-25T21:07:18.509220Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:07:18.509221Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products, query: None, headers: None, body: Missing )
 2023-04-25T21:07:18.520127Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 200, headers: Some({"date": ["Tue", "25 Apr 2023 21:07:17 GMT"], "server": ["Kestrel"], "content-type": ["application/json; charset=utf-8"], "transfer-encoding": ["chunked"]}), body: Present(191 bytes, application/json;charset=utf-8) )
 2023-04-25T21:07:18.520168Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(130 bytes) )
```

![Pact Verification](diagrams/workshop_step4_pact.svg)

The test has failed, as the expected path `/product/{id}` is returning 404. We incorrectly believed our provider was following a RESTful design, but the authors were too lazy to implement a better routing solution 🤷.

The correct endpoint which the consumer should call is `/products/{id}`.

## Step 5 - Back to the client we go

We now need to update the consumer client and tests to hit the correct product path.

First, we need to update the GET route for the client:

In [`Consumer/src/ApiClient.cs`](Consumer/src/ApiClient.cs):

```csharp
        public async Task<HttpResponseMessage> GetProduct(int id)
        {
            using (var client = new HttpClient { BaseAddress = BaseUri })
            {
                try
                {
                    // var response = await client.GetAsync($"/api/products/{id}"); // STEP_4
                    var response = await client.GetAsync($"/api/product/{id}"); // STEP_5
                    return response;
                }
                catch (Exception ex)
                {
                    throw new Exception("There was a problem connecting to Products API.", ex);
                }
            }
        }
```

Then we need to update the Pact test `ID 10 exists` to use the correct endpoint in `path`.

In [`Consumer/tests/ApiTest.cs`](Consumer/tests/ApiTest.cs):

```csharp
        [Fact]
        public async Task GetProduct()
        {
            // Arrange
            pact.UponReceiving("A valid request for a product")
                    .Given("product with ID 10 exists")
                    // .WithRequest(HttpMethod.Get, "/api/product/10") // STEP_4
                    .WithRequest(HttpMethod.Get, "/api/products/10") // STEP_5
                .WillRespond()
                    .WithStatus(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json; charset=utf-8")
                    .WithJsonBody(new TypeMatcher(products[1]));

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetProduct(10);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }
```

![Pact Verification](diagrams/workshop_step5_pact.svg)

Let's run and generate an updated pact file on the client:

Run this command under [`Consumer`](Consumer):

`dotnet test`

```console
$  dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/net6.0/consumer.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 8 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (net6.0)
```

Now we run the provider tests again with the updated contract

Run this command under [`Provider`](Provider):

`dotnet test`

```console
[1] $ dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/net6.0/provider.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService

  A valid request for a product
     Given product with ID 10 exists
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for all products
     Given products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)



Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (net6.0)
```

Yay - green ✅!

## Step 6 - Consumer updates contract for missing products

We're now going to add 2 more scenarios for the contract

- What happens when we make a call for a product that doesn't exist? We assume we'll get a `404`.
- What happens when we make a call for getting all products but none exist at the moment? We assume a `200` with an empty array.

Let's write a test for these scenarios, and then generate an updated pact file.

In [`Consumer/tests/ApiTest.cs`](Consumer/tests/ApiTest.cs):

```csharp
        [Fact]
        public async Task NoProductsExist()
        {
            // Arrange
            pact.UponReceiving("A valid request for all products")
                    .Given("no products exist")
                    .WithRequest(HttpMethod.Get, "/api/products")
                .WillRespond()
                    .WithStatus(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json; charset=utf-8")
                    .WithJsonBody(new TypeMatcher(new List<object>()));

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetAllProducts();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        [Fact]
        public async Task ProductDoesNotExist()
        {
            // Arrange
            pact.UponReceiving("A valid request for a product")
                    .Given("product with ID 11 does not exist")
                    .WithRequest(HttpMethod.Get, "/api/products/11")
                .WillRespond()
                    .WithStatus(HttpStatusCode.NotFound);

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetProduct(11);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            });
        }

```

Notice that our new tests look almost identical to our previous tests, and only differ on the expectations of the *response* - the HTTP request expectations are exactly the same.

Run this command under [`Consumer`](Consumer):

`dotnet test`

```console
dotnet test 
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/net6.0/consumer.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 44 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (net6.0)
```

What does our provider have to say about this new test?

Run this command under [`Provider`](Provider):

`dotnet test`

```console
$ dotnet test 
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/net6.0/provider.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QENFP45T", Request id "0HMQ5QENFP45T:00000002": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'no products exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 97
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 70
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QENFP45T", Request id "0HMQ5QENFP45T:00000003": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'no products exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 97
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 70
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QENFP45T", Request id "0HMQ5QENFP45T:00000004": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'no products exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 97
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 70
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QENFP45V", Request id "0HMQ5QENFP45V:00000002": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'product with ID 11 does not exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 97
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 70
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QENFP45V", Request id "0HMQ5QENFP45V:00000003": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'product with ID 11 does not exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 97
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 70
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QENFP45V", Request id "0HMQ5QENFP45V:00000004": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'product with ID 11 does not exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 97
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 70
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)

Verifying a pact between ApiClient and ProductService

  A valid request for all products
     Given no products exist
      Request Failed - One or more of the setup state change handlers has failed

  A valid request for a product
     Given product with ID 10 exists
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for a product
     Given product with ID 11 does not exist
      Request Failed - One or more of the setup state change handlers has failed

  A valid request for all products
     Given products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)


Failures:

1) Verifying a pact between ApiClient and ProductService Given no products exist - A valid request for all products - One or more of the setup state change handlers has failed

2) Verifying a pact between ApiClient and ProductService Given product with ID 11 does not exist - A valid request for a product - One or more of the setup state change handlers has failed


There were 2 pact failures

[xUnit.net 00:00:04.07]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [3 s]
  Error Message:
   PactNet.Exceptions.PactFailureException : Pact verification failed
  Stack Trace:
     at PactNet.Verifier.InteropVerifierProvider.Execute()
   at PactNet.Verifier.PactVerifierSource.Verify()
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 43
  Standard Output Messages:
 Starting verification...
 Pact verification failed
 

 Verifier Logs
 -------------
 2023-04-25T21:26:07.762714Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'no products exist' for 'A valid request for all products'
 2023-04-25T21:26:09.055377Z ERROR ThreadId(09) pact_verifier: Provider setup state change for 'no products exist' has failed - MismatchResult::Error("Invalid status code: 500", None)
 2023-04-25T21:26:09.222861Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'product with ID 10 exists' for 'A valid request for a product'
 2023-04-25T21:26:09.378031Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for a product'
 2023-04-25T21:26:09.379094Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:26:09.379097Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products/10, query: None, headers: None, body: Missing )
 2023-04-25T21:26:09.404093Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 200, headers: Some({"date": ["Tue", "25 Apr 2023 21:26:08 GMT"], "server": ["Kestrel"], "transfer-encoding": ["chunked"], "content-type": ["application/json; charset=utf-8"]}), body: Present(82 bytes, application/json;charset=utf-8) )
 2023-04-25T21:26:09.404145Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(65 bytes) )
 2023-04-25T21:26:09.554354Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'product with ID 11 does not exist' for 'A valid request for a product'
 2023-04-25T21:26:10.805314Z ERROR ThreadId(09) pact_verifier: Provider setup state change for 'product with ID 11 does not exist' has failed - MismatchResult::Error("Invalid status code: 500", None)
 2023-04-25T21:26:10.961930Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'products exist' for 'A valid request for all products'
 2023-04-25T21:26:11.104594Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for all products'
 2023-04-25T21:26:11.104626Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:26:11.104628Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products, query: None, headers: None, body: Missing )
 2023-04-25T21:26:11.106257Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 200, headers: Some({"content-type": ["application/json; charset=utf-8"], "date": ["Tue", "25 Apr 2023 21:26:10 GMT"], "transfer-encoding": ["chunked"], "server": ["Kestrel"]}), body: Present(191 bytes, application/json;charset=utf-8) )
 2023-04-25T21:26:11.106274Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(130 bytes) )
```

We got two failures related to provider state:

```md
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMB3KDP4M8GV", Request id "0HMB3KDP4M8GV:00000002": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'no products exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 84
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 57
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMB3KDP4M8H0", Request id "0HMB3KDP4M8H0:00000002": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'product does not exist' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 84
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 57
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
```

We can solve this by making sure we handle the missing provider states, which also helps us understand how Provider states work more generally.

## Step 7 - Adding the missing states

Our code already deals with missing products and sends a `404` response, however our test data fixture always has product ID 9 and 10 in our database.

In this step, we will add a state handler to our provider Pact verifications, which will update the state of our data store depending on which states the consumers require.

States are invoked prior to the actual test function is invoked. You can see the full [lifecycle here](https://github.com/pact-foundation/pact-go#lifecycle-of-a-provider-verification).

We're going to add handlers for all our states:

- products exist
- no products exist
- product with ID 10 exists
- product with ID 11 does not exist

Let's open up our provider Pact verifications in [`Provider/tests/Middleware/ProviderStateMiddleware.cs`](Provider/tests/Middleware/ProviderStateMiddleware.cs):

```csharp
// update the dictionary defined in the constructor
            _providerStates = new Dictionary<string, Action>
            {
                { "products exist", ProductsExist },
                { "product with ID 10 exists", Product10Exists },
                { "product with ID 11 does not exist", Product11DoesNotExist }, // STEP_7
                { "no products exist", NoProductsExist }, // STEP_7
                // { "No auth token is provided", Product10Exists } // STEP_10
            };
```

Also implement handlers for the two new states:

```csharp
private void NoProductsExist()
{
    _repository.SetState(new List<Product>());
}

private void Product11DoesNotExist()
{
    ProductsExist();
}
```

In `NoProductsExist()` we set the current state to an empty list. For `Product11DoesNotExist()` we can just use one of the existing states since product 11 does not exist.

Let's see how we go now:

Run this command under [`Provider`](Provider):

`dotnet test`

```console
$ dotnet test 
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/net6.0/provider.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService

  A valid request for all products
     Given no products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for a product
     Given product with ID 10 exists
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for a product
     Given product with ID 11 does not exist
    returns a response which
      has status code 404 (OK)
      has a matching body (OK)

  A valid request for all products
     Given products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)



Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (net6.0)
```

*NOTE*: The states are not necessarily a 1 to 1 mapping with the consumer contract tests. You can reuse states amongst different tests. In this scenario we could have used `no products exist` for both tests which would have equally been valid.

## Step 8 - Authorization

It turns out that not everyone should be able to use the API. After a discussion with the team, it was decided that a time-bound bearer token would suffice. The token must be in `yyyy-MM-ddTHHmm` format and within 1 hour of the current time.

In the case a valid bearer token is not provided, we expect a `401`. Let's first update the consumer tests and add scenarios for expected the `Authorization` header. In [`Consumer/tests/ApiTest.cs`](Consumer/tests/ApiTest.cs):

```csharp
        [Fact]
        public async Task GetAllProducts()
        {
            // Arrange
            pact.UponReceiving("A valid request for all products")
                    .Given("products exist")
                    .WithRequest(HttpMethod.Get, "/api/products")
                    .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z")) // STEP_8
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
                    // .WithRequest(HttpMethod.Get, "/api/product/10") // STEP_4
                    .WithRequest(HttpMethod.Get, "/api/products/10") // STEP_5
                    .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z")) // STEP_8
                .WillRespond()
                    .WithStatus(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json; charset=utf-8")
                    .WithJsonBody(new TypeMatcher(products[1]));

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetProduct(10);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        
        // STEP_6
        [Fact]
        public async Task NoProductsExist()
        {
            // Arrange
            pact.UponReceiving("A valid request for all products")
                    .Given("no products exist")
                    .WithRequest(HttpMethod.Get, "/api/products")
                    .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))  // STEP_8
                .WillRespond()
                    .WithStatus(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json; charset=utf-8")
                    .WithJsonBody(new TypeMatcher(new List<object>()));

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetAllProducts();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });
        }

        // STEP_6
        [Fact]
        public async Task ProductDoesNotExist()
        {
            // Arrange
            pact.UponReceiving("A valid request for a product")
                    .Given("product with ID 11 does not exist")
                    .WithRequest(HttpMethod.Get, "/api/products/11")
                    .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))  // STEP_8
                .WillRespond()
                    .WithStatus(HttpStatusCode.NotFound);

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetProduct(11);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            });
        }

        // STEP_8
        [Fact]
        public async Task GetProductMissingAuthHeader()
        {
            // Arrange
            pact.UponReceiving("A valid request for a product")
                    .Given("No auth token is provided")
                    .WithRequest(HttpMethod.Get, "/api/products/10")
                .WillRespond()
                    .WithStatus(HttpStatusCode.Unauthorized);

            await pact.VerifyAsync(async ctx => {
                var response = await ApiClient.GetProduct(10);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            });
        }
```

We could implement the header as well but let's first briefly explain what's going on here. Instead of simply entering a header we use a `matcher`. A matcher
can be used when we don't necessarily need to test exact values but e.g. just check the type, or the pattern. In the example above we use a `Regex` matcher.
We provide an example value (which will end up in the pact file) as well as the pattern we want the value to match. This means that we can dynamically generate
the value and still have a passing test.

Before we implement the change, let's see what happens if we run it without implementing. The output is very long so it's been abbreviated to show the only one
the four test cases:

```console
[1] $ dotnet test 
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/net6.0/consumer.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.41]     tests.ApiTest.GetAllProducts [FAIL]
[xUnit.net 00:00:00.42]     tests.ApiTest.GetProduct [FAIL]
  Failed tests.ApiTest.GetAllProducts [43 ms]
  Error Message:
   Assert.Equal() Failure
Expected: OK
Actual:   InternalServerError
  Stack Trace:
     at tests.ApiTest.<GetAllProducts>b__5_0(IConsumerContext ctx) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/ApiTest.cs:line 61
   at PactNet.PactBuilder.VerifyAsync(Func`2 interact)
   at tests.ApiTest.GetAllProducts() in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/ApiTest.cs:line 59
--- End of stack trace from previous location ---
  Standard Output Messages:
 Mock driver logs:
 
 2023-04-25T21:42:47.302759Z  INFO tokio-runtime-worker pact_mock_server::hyper_server: Received request HTTP Request ( method: GET, path: /api/products, query: None, headers: Some({"host": ["localhost:9000"]}), body: Empty )
 2023-04-25T21:42:47.302792Z  INFO tokio-runtime-worker pact_matching: comparing to expected HTTP Request ( method: GET, path: /api/products, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
 


  Failed tests.ApiTest.GetProduct [2 ms]
  Error Message:
   Assert.Equal() Failure
Expected: OK
Actual:   InternalServerError
  Stack Trace:
     at tests.ApiTest.<GetProduct>b__6_0(IConsumerContext ctx) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/ApiTest.cs:line 81
   at PactNet.PactBuilder.VerifyAsync(Func`2 interact)
   at tests.ApiTest.GetProduct() in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/ApiTest.cs:line 79
--- End of stack trace from previous location ---
  Standard Output Messages:
 Mock driver logs:
 
 2023-04-25T21:42:47.310701Z  INFO tokio-runtime-worker pact_mock_server::hyper_server: Received request HTTP Request ( method: GET, path: /api/products/10, query: None, headers: Some({"host": ["localhost:9000"]}), body: Empty )
 2023-04-25T21:42:47.310721Z  INFO tokio-runtime-worker pact_matching: comparing to expected HTTP Request ( method: GET, path: /api/products/10, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
```

The standard logs, don't always give us enough information in case of failure, you can enable debug logging in [`Consumer/tests/ApiTest.cs`](Consumer/tests/ApiTest.cs):

```csharp
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
```

and run your test again, The key information can be found in the `DEBUG` output:

```console
 2023-04-25T21:44:17.273570Z DEBUG tokio-runtime-worker pact_matching: --> Mismatches: [HeaderMismatch { key: "Authorization", expected: "\"Bearer 2019-01-14T11:34:18.045Z\"", actual: "", mismatch: "Expected header 'Authorization' but was missing" }]
 2023-04-25T21:44:17.273591Z DEBUG tokio-runtime-worker pact_mock_server::hyper_server: Request did not match: Request did not match - HTTP Request ( method: GET, path: /api/products/10, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )    0) Expected header 'Authorization' but was missing
```

Let's update the consumer to pass the bearer token, which was missing

In [`consumer/src/ApiClient.cs`](consumer/src/ApiClient.cs):

```csharp
        public async Task<HttpResponseMessage> GetAllProducts()
        {
            using (var client = new HttpClient { BaseAddress = BaseUri })
            {
                try
                {
                    client.DefaultRequestHeaders.Add("Authorization", AuthorizationHeaderValue()); // STEP_8
                    var response = await client.GetAsync($"/api/products");
                    return response;
                }
                catch (Exception ex)
                {
                    throw new Exception("There was a problem connecting to Products API.", ex);
                }
            }
        }

        public async Task<HttpResponseMessage> GetProduct(int id)
        {
            using (var client = new HttpClient { BaseAddress = BaseUri })
            {
                try
                {
                    client.DefaultRequestHeaders.Add("Authorization", AuthorizationHeaderValue()); // STEP_8
                    var response = await client.GetAsync($"/api/products/{id}");
                    // var response = await client.GetAsync($"/api/product/{id}"); // STEP_5
                    return response;
                }
                catch (Exception ex)
                {
                    throw new Exception("There was a problem connecting to Products API.", ex);
                }
            }
        }

        // STEP_8
        private string AuthorizationHeaderValue()
{
            return $"Bearer {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}";
        }
```

Generate a new Pact file:

```console
$ dotnet test 
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/net6.0/consumer.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 55 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/net6.0/tests.dll (net6.0

```

The changes in the pact file located at [`pacts/ApiClient-ProductService.json`](pacts/ApiClient-ProductService.json) is now that we have some logic for header matching:

```json
      "request": {
        "headers": {
          "Authorization": "Bearer 2019-01-14T11:34:18.045Z"
        },
        "matchingRules": {
          "header": {
            "$.Authorization[0]": {
              "combine": "AND",
              "matchers": [
                {
                  "match": "regex",
                  "regex": "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"
                }
              ]
            }
          }
        },
        "method": "GET",
        "path": "/api/products"
      },
```

Let's test the provider.

Run this command under [`Provider`](Provider):

`dotnet test`

```console
[1] $ dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/net6.0/provider.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QRJDM11B", Request id "0HMQ5QRJDM11B:00000002": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'No auth token is provided' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 99
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 72
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QRJDM11B", Request id "0HMQ5QRJDM11B:00000003": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'No auth token is provided' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 99
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 72
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5QRJDM11B", Request id "0HMQ5QRJDM11B:00000004": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'No auth token is provided' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 99
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 72
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)

Verifying a pact between ApiClient and ProductService

  A valid request for a product
     Given No auth token is provided
      Request Failed - One or more of the setup state change handlers has failed

  A valid request for all products
     Given no products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for a product
     Given product with ID 10 exists
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for a product
     Given product with ID 11 does not exist
    returns a response which
      has status code 404 (OK)
      has a matching body (OK)

  A valid request for all products
     Given products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)


Failures:

1) Verifying a pact between ApiClient and ProductService Given No auth token is provided - A valid request for a product - One or more of the setup state change handlers has failed


There were 1 pact failures

[xUnit.net 00:00:03.31]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [2 s]
  Error Message:
   PactNet.Exceptions.PactFailureException : Pact verification failed
  Stack Trace:
     at PactNet.Verifier.InteropVerifierProvider.Execute()
   at PactNet.Verifier.PactVerifierSource.Verify()
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 43
  Standard Output Messages:
 Starting verification...
 Pact verification failed
 
 Verifier Logs
 -------------
 2023-04-25T21:49:09.979495Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'No auth token is provided' for 'A valid request for a product'
 2023-04-25T21:49:11.277542Z ERROR ThreadId(09) pact_verifier: Provider setup state change for 'No auth token is provided' has failed - MismatchResult::Error("Invalid status code: 500", None)
 2023-04-25T21:49:11.450384Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'no products exist' for 'A valid request for all products'
 2023-04-25T21:49:11.594683Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for all products'
 2023-04-25T21:49:11.595974Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:49:11.595977Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products, query: None, headers: None, body: Missing )
 2023-04-25T21:49:11.612998Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 200, headers: Some({"date": ["Tue", "25 Apr 2023 21:49:10 GMT"], "server": ["Kestrel"], "content-type": ["application/json; charset=utf-8"], "transfer-encoding": ["chunked"]}), body: Present(2 bytes, application/json;charset=utf-8) )
 2023-04-25T21:49:11.613047Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(2 bytes) )
 2023-04-25T21:49:11.768563Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'product with ID 10 exists' for 'A valid request for a product'
 2023-04-25T21:49:11.913473Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for a product'
 2023-04-25T21:49:11.913512Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:49:11.913513Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products/10, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
 2023-04-25T21:49:11.921699Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 200, headers: Some({"date": ["Tue", "25 Apr 2023 21:49:11 GMT"], "transfer-encoding": ["chunked"], "server": ["Kestrel"], "content-type": ["application/json; charset=utf-8"]}), body: Present(82 bytes, application/json;charset=utf-8) )
 2023-04-25T21:49:11.921718Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(65 bytes) )
 2023-04-25T21:49:12.066023Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'product with ID 11 does not exist' for 'A valid request for a product'
 2023-04-25T21:49:12.218216Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for a product'
 2023-04-25T21:49:12.218250Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:49:12.218251Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products/11, query: None, headers: None, body: Missing )
 2023-04-25T21:49:12.219873Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 404, headers: Some({"date": ["Tue", "25 Apr 2023 21:49:11 GMT"], "content-length": ["0"], "server": ["Kestrel"]}), body: Empty )
 2023-04-25T21:49:12.219880Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 404, headers: None, body: Missing )
 2023-04-25T21:49:12.374008Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'products exist' for 'A valid request for all products'
 2023-04-25T21:49:12.524133Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for all products'
 2023-04-25T21:49:12.524173Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:49:12.524174Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
 2023-04-25T21:49:12.524756Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 200, headers: Some({"content-type": ["application/json; charset=utf-8"], "date": ["Tue", "25 Apr 2023 21:49:11 GMT"], "server": ["Kestrel"], "transfer-encoding": ["chunked"]}), body: Present(191 bytes, application/json;charset=utf-8) )
 2023-04-25T21:49:12.524769Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(130 bytes) )
```

Now with the most recently added interactions where we are expecting a response of 401 when no authorization header is sent, we are getting 200...

## Step 9 - Implement authorization on the provider

We will add a middleware to check the Authorization header and deny the request with `401` if the token is older than 1 hour.

In the folder called [`Provider/src/Middleware`](Provider/src/Middleware`), we are going to create a class called [`AuthorizationMiddleware.cs`](Provider/src/Middleware/AuthorizationMiddleware.cs):

```csharp
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace provider.Middleware
{
    public class AuthorizationMiddleware
    {
        private const string AuthorizationHeaderKey = "Authorization";
        private readonly RequestDelegate _next;

        public AuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                DateTime tokenTime = DateTime.Parse(AuthorizationHeader(context.Request));

                if (IsOlderThanOneHour(tokenTime))
                {
                    UnauthorizedResponse(context);
                }
                else
                {
                    await this._next(context);
                }
            }
            else
            {
                UnauthorizedResponse(context);
            }
        }

        private string AuthorizationHeader(HttpRequest request)
        {
            request.Headers.TryGetValue(AuthorizationHeaderKey, out var authorizationHeader);
            var match = Regex.Match(authorizationHeader, "Bearer (.*)");
            return match.Groups[1].Value;
        }

        private bool IsOlderThanOneHour(DateTime tokenTime)
        {
            return tokenTime < DateTime.Now.AddHours(-1);
        }

        private void UnauthorizedResponse(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }
}
```

Add the middleware in [`Provider/src/Startup.cs`](Provider/src/Startup.cs), including the `using` statement:

```csharp
using provider.Middleware; // STEP_9

...

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMiddleware<AuthorizationMiddleware>(); // STEP_9
            app.UseRouting();
            app.UseEndpoints(e => e.MapControllers());
            
        }
```

This means that a client must present an HTTP `Authorization` header that looks as follows:

```sh
Authorization: Bearer 2006-01-02T15:04
```

Let's test this out:

Run this command under [`Provider`](Provider):

`dotnet test`

```console
[1] $ dotnet test 
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/net6.0/provider.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5R09TV075", Request id "0HMQ5R09TV075:00000002": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'No auth token is provided' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 99
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 72
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5R09TV075", Request id "0HMQ5R09TV075:00000003": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'No auth token is provided' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 99
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 72
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)
fail: Microsoft.AspNetCore.Server.Kestrel[13]
      Connection id "0HMQ5R09TV075", Request id "0HMQ5R09TV075:00000004": An unhandled exception was thrown by the application.
      System.Collections.Generic.KeyNotFoundException: The given key 'No auth token is provided' was not present in the dictionary.
         at System.Collections.Generic.Dictionary`2.get_Item(TKey key)
         at tests.Middleware.ProviderStateMiddleware.HandleProviderStatesRequest(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 99
         at tests.Middleware.ProviderStateMiddleware.Invoke(HttpContext context) in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/Middleware/ProviderStateMiddleware.cs:line 72
         at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol.ProcessRequests[TContext](IHttpApplication`1 application)

Verifying a pact between ApiClient and ProductService

  A valid request for a product
     Given No auth token is provided
      Request Failed - One or more of the setup state change handlers has failed

  A valid request for all products
     Given no products exist
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)

  A valid request for a product
     Given product with ID 10 exists
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)

  A valid request for a product
     Given product with ID 11 does not exist
    returns a response which
      has status code 404 (FAILED)
      has a matching body (OK)

  A valid request for all products
     Given products exist
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)


Failures:

1) Verifying a pact between ApiClient and ProductService Given No auth token is provided - A valid request for a product - One or more of the setup state change handlers has failed

2) Verifying a pact between ApiClient and ProductService Given no products exist - A valid request for all products
    2.1) has a matching body
           / -> Expected body Present(2 bytes) but was empty
    2.2) has status code 200
           expected 200 but was 401
    2.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
3) Verifying a pact between ApiClient and ProductService Given product with ID 10 exists - A valid request for a product
    3.1) has a matching body
           / -> Expected body Present(65 bytes) but was empty
    3.2) has status code 200
           expected 200 but was 401
    3.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
4) Verifying a pact between ApiClient and ProductService Given product with ID 11 does not exist - A valid request for a product
    4.1) has status code 404
           expected 404 but was 401
5) Verifying a pact between ApiClient and ProductService Given products exist - A valid request for all products
    5.1) has a matching body
           / -> Expected body Present(130 bytes) but was empty
    5.2) has status code 200
           expected 200 but was 401
    5.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''

There were 5 pact failures

[xUnit.net 00:00:03.30]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [2 s]
  Error Message:
   PactNet.Exceptions.PactFailureException : Pact verification failed
  Stack Trace:
     at PactNet.Verifier.InteropVerifierProvider.Execute()
   at PactNet.Verifier.PactVerifierSource.Verify()
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 43
  Standard Output Messages:
 Starting verification...
 Pact verification failed
 
 Verifier Output
 ---------------
 
 Verifying a pact between ApiClient and ProductService
 
   A valid request for a product
      Given No auth token is provided
       Request Failed - One or more of the setup state change handlers has failed
 
   A valid request for all products
      Given no products exist
     returns a response which
       has status code 200 (FAILED)
       includes headers
         "Content-Type" with value "application/json; charset=utf-8" (FAILED)
       has a matching body (FAILED)
 
   A valid request for a product
      Given product with ID 10 exists
     returns a response which
       has status code 200 (FAILED)
       includes headers
         "Content-Type" with value "application/json; charset=utf-8" (FAILED)
       has a matching body (FAILED)
 
   A valid request for a product
      Given product with ID 11 does not exist
     returns a response which
       has status code 404 (FAILED)
       has a matching body (OK)
 
   A valid request for all products
      Given products exist
     returns a response which
       has status code 200 (FAILED)
       includes headers
         "Content-Type" with value "application/json; charset=utf-8" (FAILED)
       has a matching body (FAILED)
 
 
 Failures:
 
 1) Verifying a pact between ApiClient and ProductService Given No auth token is provided - A valid request for a product - One or more of the setup state change handlers has failed
 
 2) Verifying a pact between ApiClient and ProductService Given no products exist - A valid request for all products
     2.1) has a matching body
            / -> Expected body Present(2 bytes) but was empty
     2.2) has status code 200
            expected 200 but was 401
     2.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
            Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
 3) Verifying a pact between ApiClient and ProductService Given product with ID 10 exists - A valid request for a product
     3.1) has a matching body
            / -> Expected body Present(65 bytes) but was empty
     3.2) has status code 200
            expected 200 but was 401
     3.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
            Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
 4) Verifying a pact between ApiClient and ProductService Given product with ID 11 does not exist - A valid request for a product
     4.1) has status code 404
            expected 404 but was 401
 5) Verifying a pact between ApiClient and ProductService Given products exist - A valid request for all products
     5.1) has a matching body
            / -> Expected body Present(130 bytes) but was empty
     5.2) has status code 200
            expected 200 but was 401
     5.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
            Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
 
 There were 5 pact failures
 
 Verifier Logs
 -------------
 2023-04-25T21:57:35.018185Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'No auth token is provided' for 'A valid request for a product'
 2023-04-25T21:57:36.301488Z ERROR ThreadId(09) pact_verifier: Provider setup state change for 'No auth token is provided' has failed - MismatchResult::Error("Invalid status code: 500", None)
 2023-04-25T21:57:36.460737Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'no products exist' for 'A valid request for all products'
 2023-04-25T21:57:36.630956Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for all products'
 2023-04-25T21:57:36.632136Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:57:36.632140Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products, query: None, headers: None, body: Missing )
 2023-04-25T21:57:36.634834Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 401, headers: Some({"date": ["Tue", "25 Apr 2023 21:57:35 GMT"], "server": ["Kestrel"], "content-length": ["0"]}), body: Empty )
 2023-04-25T21:57:36.634874Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(2 bytes) )
 2023-04-25T21:57:36.784738Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'product with ID 10 exists' for 'A valid request for a product'
 2023-04-25T21:57:36.932746Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for a product'
 2023-04-25T21:57:36.932784Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:57:36.932786Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products/10, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
 2023-04-25T21:57:36.936515Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 401, headers: Some({"date": ["Tue", "25 Apr 2023 21:57:36 GMT"], "content-length": ["0"], "server": ["Kestrel"]}), body: Empty )
 2023-04-25T21:57:36.936528Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(65 bytes) )
 2023-04-25T21:57:37.083492Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'product with ID 11 does not exist' for 'A valid request for a product'
 2023-04-25T21:57:37.229871Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for a product'
 2023-04-25T21:57:37.229899Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:57:37.229900Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products/11, query: None, headers: None, body: Missing )
 2023-04-25T21:57:37.230174Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 401, headers: Some({"date": ["Tue", "25 Apr 2023 21:57:36 GMT"], "server": ["Kestrel"], "content-length": ["0"]}), body: Empty )
 2023-04-25T21:57:37.230181Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 404, headers: None, body: Missing )
 2023-04-25T21:57:37.379565Z  INFO ThreadId(09) pact_verifier: Running setup provider state change handler 'products exist' for 'A valid request for all products'
 2023-04-25T21:57:37.531002Z  INFO ThreadId(09) pact_verifier: Running provider verification for 'A valid request for all products'
 2023-04-25T21:57:37.531038Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request to provider at http://127.0.0.1:9001/
 2023-04-25T21:57:37.531039Z  INFO ThreadId(09) pact_verifier::provider_client: Sending request HTTP Request ( method: GET, path: /api/products, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
 2023-04-25T21:57:37.531277Z  INFO ThreadId(09) pact_verifier::provider_client: Received response: HTTP Response ( status: 401, headers: Some({"server": ["Kestrel"], "content-length": ["0"], "date": ["Tue", "25 Apr 2023 21:57:36 GMT"]}), body: Empty )
 2023-04-25T21:57:37.531286Z  INFO ThreadId(09) pact_matching: comparing to expected response: HTTP Response ( status: 200, headers: Some({"Content-Type": ["application/json; charset=utf-8"]}), body: Present(130 bytes) )
 

Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (net6.0)
```

Oh, dear. *More* tests are failing. Can you understand why? If you can't don't worry, we will find out in the next step!

## Step 10 - Request Filters on the Provider

Because our pact file has static data in it, our bearer token is now out of date, so when Pact verification passes it to the Provider we get a `401`. There are multiple ways to resolve this - mocking or stubbing out the authentication component is a common one. In our use case, we are going to use a process referred to as *Request Filtering*, using a `RequestFilter`.

*NOTE*: This is an advanced concept and should be used carefully, as it has the potential to invalidate a contract by bypassing its constraints. See https://github.com/DiUS/pact-jvm/blob/master/provider/junit/README.md#modifying-the-requests-before-they-are-sent for more details on this.

The approach we are going to take to inject the header is as follows:

1. If we receive any Authorization header, we override the incoming request with a valid (in time) Authorization header, and continue with whatever call was being made
2. If we don't receive an Authorization header, we do nothing

*NOTE*: We are not considering the `403` scenario in this example.

We'll implement this as a Middleware, similarly to have we deal with `provider states`.

In the folder [`Provider/tests/Middleware`](Provider/tests/Middleware) add a new file called [`AuthTokenRequestFilter.cs`](Provider/tests/Middleware/AuthTokenRequestFilter.cs):

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace tests.Middleware
{   
    // STEP_10
    public class AuthTokenRequestFilter
    {
        private const string AuthorizationHeaderKey = "Authorization";
        private readonly RequestDelegate _next;

        public AuthTokenRequestFilter(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey(AuthorizationHeaderKey))
            {
                context.Request.Headers.Remove(AuthorizationHeaderKey);
                context.Request.Headers.Add(AuthorizationHeaderKey, HeaderValue());
            }
            await this._next(context);
        }

        private StringValues HeaderValue()
        {
            return $"Bearer {DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}";
        }
    }
}
```

Then wire the new middleware in [`Provider/tests/TestStartup.cs`](Provider/tests/TestStartup.cs)

```csharp
// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseMiddleware<ProviderStateMiddleware>();
    app.UseMiddleware<AuthTokenRequestFilter>(); // STEP_10
    _proxy.Configure(app, env);
}
```

And finally, add the corresponding state handler [`Provider/tests/Middleware/ProviderStateMiddleware.cs`](Provider/tests/Middleware/ProviderStateMiddleware.cs)

We map it to a valid product, that we don't want an authenticated user to get access to, they wont be able to unless they present an valid authentication token

```csharp
            _providerStates = new Dictionary<string, Action>
            {
                { "products exist", ProductsExist },
                { "product with ID 10 exists", Product10Exists },
                { "product with ID 11 does not exist", Product11DoesNotExist }, // STEP_7
                { "no products exist", NoProductsExist }, // STEP_7
                { "No auth token is provided", Product10Exists } // STEP_10
            };
```

We can now run the Provider tests

Run this command under [`Provider`](Provider):

`dotnet test`

```dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/net6.0/provider.dll
  tests -> /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll
Test run for /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.3.1 (arm64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService

  A valid request for a product
     Given No auth token is provided
    returns a response which
      has status code 401 (OK)
      has a matching body (OK)

  A valid request for all products
     Given no products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for a product
     Given product with ID 10 exists
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

  A valid request for a product
     Given product with ID 11 does not exist
    returns a response which
      has status code 404 (OK)
      has a matching body (OK)

  A valid request for all products
     Given products exist
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)



Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/saf/dev/DIUS/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net6.0/tests.dll (net6.0)
```
