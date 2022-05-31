| :exclamation: WARNING: This workshop is out of date and uses an earlier beta version of the Pact .NET project. It is not currently recommended for use |
|-----------------------------------------|

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
- [Prerequisites](#prerequisites)
- [Workshop Steps](#workshop-steps)
  - [Preamble: **clone repository and explore**](#preamble-clone-repository-and-explore)
  - [Step 1: **create consumer**](#step-1---simple-consumer-calling-provider): Create our consumer before the Provider API even exists
  - [Step 2: **integration problems**](#step-2---integration-problems): Connecting the client to the product service
  - [Step 3: **pact test**](#step-3---pact-to-the-rescue): Write a Pact test for our consumer
  - [Step 4: **pact verification**](#step-4---verify-the-provider): Verify the consumer pact with the Provider API
  - [Step 5: **fix consumer**](#step-5---back-to-the-client-we-go): Fix the consumer's bad assumptions about the Provider
  - [Step 6: **pact test**](#step-6---consumer-updates-contract-for-missing-products): Write a pact test for `404` (missing User) in consumer
  - [Step 7: **provider states**](#step-7---adding-the-missing-states): Update API to handle `404` case
  - [Step 9: **pact test**](#step-9---implement-authorisation-on-the-provider): Update API to handle `401` case
  - [Step 10: **request filters**](#step-10---request-filters-on-the-provider): Fix the provider to support the `401` case

## Learning objectives

If running this as a team workshop format, you may want to take a look through the [learning objectives](./LEARNING.md).

# Prerequisites

This workshop while written with .NET Core is not specifically about it so in-depth knowledge of .NET Core is not required if you can
write code in any other language you should be fine.

However before taking part in this workshop please make sure you have:

* [.NET Core SDK](https://www.microsoft.com/net/download/). Make sure you pick version 3.1 for this.
* An account at [Github.com](www.github.com)!
* A text editor/IDE that supports .NET Core. Check out [VSCode](https://code.visualstudio.com/)

## Add the Pacflow Nuget repository to Visual Studio/NuGet.Config

In order to use the 4.0.0-beta version of Pact.Net you need to add the Pacflow Nuget repository to Visual Studio and/or your Nuget.Config
file so that the libraries can be downloaded.

* For Visual Studio add `https://pactfoundation.jfrog.io/artifactory/api/nuget/default-nuget-local` as a Package Source. In order to see the
package you'll need to tick the `Include prereleases` checkbox since the libraries we'll use are beta versions
* For NuGet.Config (found at %appdata%\NuGet\NuGet.Config (Windows) or ~/.config/NuGet/NuGet.Config (Mac/Linux)) add
`<add key="ArtifactoryNuGetV3" value="https://pactfoundation.jfrog.io/artifactory/api/nuget/v3/default-nuget-local" protocolVersion="3" />`

# Workshop Steps

## Preamble: Clone repository and explore

Clone [pact-workshop-dotnet-core-v3](https://github.com/dius/pact-workshop-dotnet-core-v3) and familiarise yourself with
its contents. There are two components in scope for our workshop.

1. Product client. A command line client that queries the Product service for product information.
1. Product Service (Provider). Provides useful things about products, such as listing all products and getting the details of an individual product.

The two components can be found in their respective folders and each have a solution (`.sln`) file and project files for the app and test projects (`.csproj`)

### Consumer

A consumer project in the **Consumer/src** folder which is a simple .NET Core console application that connects to the
Provider project

### Provider

A provider in the **Provider/src** folder and is an ASP.NET Core Web API. Both projects also have a **tests/** folder which
is where the [Pact](https://docs.pact.io/) tests for both projects exist.

## Step 1: Simple consumer calling provider

We need to first create an HTTP client to make the calls to our provider service:

![Simple Consumer](diagrams/workshop_step1.svg)

The Consumer has implemented the product service client which has the following:

- `GET /products` - Retrieve all products
- `GET /products/{id}` - Retrieve a single product by ID

The diagram below highlights the interaction for retrieving a product with ID 10:

![Sequence Diagram](diagrams/workshop_step1_class-sequence-diagram.svg)

You can see the client interface we created in `Consumer/src/ApiClient.cs`:

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

# Step 2 - Integration problems...

our provider team has started building out their API in parallel. Let's run our website against our provider (you'll need two terminals to do this):


```console
# Terminal 1
erikdanielsen@Erik’s MacBook Pro:~/work/dius/pact-workshop-dotnet-core-v3/Provider/src (branch: master!)
$ dotnet run
Hosting environment: Development
Content root path: /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src
Now listening on: http://localhost:9001
Application started. Press Ctrl+C to shut down.
```

```console
# Terminal 2
[130] $ dotnet run                                                                                                                                                                  ✘
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


**Retrieving product with id=10**
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

Note how similar it looks to a unit test:

```csharp
using System.IO;
using PactNet;
using PactNet.Native;
using Xunit.Abstractions;
using Xunit;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Consumer;
using PactNet.Matchers;

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
                LogDir = "pact_logs",
                Outputters = new[] { new XUnitOutput(output) },
                DefaultJsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
            };

            pact = Pact.V3("ApiClient", "ProductService", Config).UsingNativeBackend(port);
            ApiClient = new ApiClient(new System.Uri($"http://localhost:{port}"));
        }

        [Fact]
        public async void GetAllProducts()
        {
            // Arange
            pact.UponReceiving("A valid request for all products")
                    .Given("There is data")
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
        public async void GetProduct()
        {
            // Arange
            pact.UponReceiving("A valid request for a product")
                    .Given("There is data")
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

```

![Test using Pact](diagrams/workshop_step3_pact.svg)

This test starts a mock server on a specificed port (9000 here) that acts as our provider service.

Running this test still passes, but it creates a pact file which we can use to validate our assumptions on the provider side, and have conversation around.

```console
$ dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/netcoreapp3.1/consumer.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 12 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (netcoreapp3.1)
```

A pact file should have been generated in *pacts/ApiClient-ProductService.json*

*NOTE*: even if the API client had been graciously provided for us by our Provider Team, it doesn't mean that we shouldn't write contract tests - because the version of the client we have may not always be in sync with the deployed API - and also because we will write tests on the output appropriate to our specific needs.

## Step 4 - Verify the provider

Now let's make a start on writing Pact tests to validate the consumer contract:

In `provider/test/`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Native;
using tests.XUnitHelpers;
using Xunit;
using Xunit.Abstractions;

namespace tests
{
    public class ProductTest
    {
        private string _pactServiceUri = "http://127.0.0.1:9001";
        private ITestOutputHelper _outputHelper { get; }

        public ProductTest(ITestOutputHelper output)
        {
            _outputHelper = output;
        }

        [Fact]
        public void EnsureProviderApiHonoursPactWithConsumer()
        {
            // Arrange
            var config = new PactVerifierConfig
            {
                // NOTE: We default to using a ConsoleOutput, however xUnit 2 does not capture the console output,
                // so a custom outputter is required.
                Outputters = new List<IOutput>
                {
                    new XUnitOutput(_outputHelper)
                }
            };

            using (var _webHost = WebHost.CreateDefaultBuilder().UseStartup<TestStartup>().UseUrls(_pactServiceUri).Build())
            {
                _webHost.Start();

                //Act / Assert
                IPactVerifier pactVerifier = new PactVerifier(config);
                var pactFile = new FileInfo(Path.Join("..", "..", "..", "..", "..", "pacts", "ApiClient-ProductService.json"));
                pactVerifier.FromPactFile(pactFile)
                    .WithProviderStateUrl(new Uri($"{_pactServiceUri}/provider-states"))
                    .ServiceProvider("ProductService", new Uri(_pactServiceUri))
                    .HonoursPactWith("ApiClient")
                    .Verify();
            }
        }
    }
}
```

We now need to validate the pact generated by the consumer is valid, by executing it against the running service provider, which should fail:

```console
[1] $ dotnet test                                                                                                                                                                                                  ✘
  Determining projects to restore...
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/provider.csproj (in 58 ms).
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/tests.csproj (in 240 ms).
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/provider.csproj (in 530 ms).
  provider -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/netcoreapp3.1/provider.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (.NETCoreApp,Version=v5.0)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService
  Given product with ID 10 exists
  Given products exist
  A valid request for a product
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)
  A valid request for all products
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)


Failures:

1) Verifying a pact between ApiClient and ProductService Given product with ID 10 exists - A valid request for a product returns a response which
    1.1) has a matching body
           / -> Expected body Present(65 bytes) but was empty
    1.2) has status code 200
           expected 200 but was 404
    1.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''

There were 1 pact failures

[xUnit.net 00:00:00.93]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [358 ms]
  Error Message:
   PactNet.PactFailureException : The verification process failed, see output for errors
  Stack Trace:
     at PactNet.Native.NativePactVerifier.Verify(String args) in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/NativePactVerifier.cs:line 34
   at PactNet.Native.PactVerifier.Verify() in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/PactVerifier.cs:line 240
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 46
  Standard Output Messages:
 Invoking the pact verifier with args:
 --file
 /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/pacts/ApiClient-ProductService.json
 --state-change-url
 http://127.0.0.1:9001/provider-states
 --provider-name
 ProductService
 --hostname
 127.0.0.1
 --port
 9001
 --filter-consumer
 ApiClient
 --loglevel
 trace



Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (net5.0)
```

![Pact Verification](diagrams/workshop_step4_pact.svg)

The test has failed, as the expected path `/product/{id}` is returning 404. We incorrectly believed our provider was following a RESTful design, but the authors were too lazy to implement a better routing solution 🤷.

The correct endpoint which the consumer should call is `/products/{id}`.

## Step 5 - Back to the client we go

We now need to update the consumer client and tests to hit the correct product path.

First, we need to update the GET route for the client:

In `Consumer/src/ApiClient.cs`:

```csharp
public async Task<HttpResponseMessage> GetProduct(int id)
{
    using (var client = new HttpClient { BaseAddress = BaseUri })
    {
        try
        {
            var response = await client.GetAsync($"/api/products/{id}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception("There was a problem connecting to Provider API.", ex);
        }
    }
}
```

Then we need to update the Pact test `ID 10 exists` to use the correct endpoint in `path`.

In `Consumer/tests/ApiTest.cs`:

```csharp
[Fact]
public async void GetProduct()
{
    // Arange
    pact.UponReceiving("A valid request for a product")
            .Given("There is data")
            .WithRequest(HttpMethod.Get, "/api/products/10")
        .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(products[1]);

    await pact.VerifyAsync(async ctx => {
        var response = await ApiClient.GetProduct(10);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    });
}
```

![Pact Verification](diagrams/workshop_step5_pact.svg)

Let's run and generate an updated pact file on the client:

```console
$ dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/netcoreapp3.1/consumer.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 12 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (netcoreapp3.1)
```

Now we run the provider tests again with the updated contract

Run thse command under `Consumer/tests`:

```console
[1] $ dotnet test                                                                                                                                                                   ✘
  Determining projects to restore...
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/provider.csproj (in 53 ms).
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/tests.csproj (in 225 ms).
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/provider.csproj (in 524 ms).
  provider -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/netcoreapp3.1/provider.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (.NETCoreApp,Version=v5.0)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService
  Given There is data
  Given There is data
  A valid request for a product
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)
  A valid request for all products
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)



Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (net5.0)
```

Yay - green ✅!

## Step 6 - Consumer updates contract for missing products

We're now going to add 2 more scenarios for the contract

- What happens when we make a call for a product that doesn't exist? We assume we'll get a `404`.
- What happens when we make a call for getting all products but none exist at the moment? We assume a `200` with an empty array.

Let's write a test for these scenarios, and then generate an updated pact file.

In `Consumer/tests/ApiTest.cs`:

```csharp
[Fact]
public async void NoProductsExist()
{
    // Arange
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
public async void ProductDoesNotExist()
{
    // Arange
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

Notice that our new tests look almost identical to our previous tests, and only differ on the expectations of the _response_ - the HTTP request expectations are exactly the same.

```console
$ dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/netcoreapp3.1/consumer.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 63 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (netcoreapp3.1)
```

What does our provider have to say about this new test? 

```console
$ dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/netcoreapp3.1/provider.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (.NETCoreApp,Version=v5.0)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService
  Given no products exist
  Given product does not exist
  Given product with ID 10 exists
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
  Given products exist
  A valid request for all products
      Request Failed - One or more of the state change handlers has failed
  A valid request for a product
      Request Failed - One or more of the state change handlers has failed
  A valid request for a product
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)
  A valid request for all products
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)


Failures:

1) Verifying a pact between ApiClient and ProductService Given no products exist - A valid request for all products - One or more of the state change handlers has failed

2) Verifying a pact between ApiClient and ProductService Given product does not exist - A valid request for a product - One or more of the state change handlers has failed


There were 2 pact failures

[xUnit.net 00:00:00.93]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [392 ms]
  Error Message:
   PactNet.PactFailureException : The verification process failed, see output for errors
  Stack Trace:
     at PactNet.Native.NativePactVerifier.Verify(String args) in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/NativePactVerifier.cs:line 34
   at PactNet.Native.PactVerifier.Verify() in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/PactVerifier.cs:line 240
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 46
  Standard Output Messages:
 Invoking the pact verifier with args:
 --file
 /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/pacts/ApiClient-ProductService.json
 --state-change-url
 http://127.0.0.1:9001/provider-states
 --provider-name
 ProductService
 --hostname
 127.0.0.1
 --port
 9001
 --filter-consumer
 ApiClient
 --loglevel
 trace



Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (net5.0)
```

We got two failures related to provider state:
```
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

Let's open up our provider Pact verifications in `Provider/tests/Middleware/ProviderStateMiddleware`:

```csharp
// update the dictionary defined in the constructor
_providerStates = new Dictionary<string, Action>
{
    { "products exist", ProductsExist },
    { "no products exist", NoProductsExist },
    { "product with ID 11 does not exist", Product11DoesNotExist },
    { "product with ID 10 exists", Product10Exists }
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

```console
erikdanielsen@Erik’s MacBook Pro:~/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests (branch: master!)
$ dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/netcoreapp3.1/consumer.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 61 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (netcoreapp3.1)
```

_NOTE_: The states are not necessarily a 1 to 1 mapping with the consumer contract tests. You can reuse states amongst different tests. In this scenario we could have used `no products exist` for both tests which would have equally been valid.

## Step 8 - Authorization

It turns out that not everyone should be able to use the API. After a discussion with the team, it was decided that a time-bound bearer token would suffice. The token must be in `yyyy-MM-ddTHHmm` format and within 1 hour of the current time.

In the case a valid bearer token is not provided, we expect a `401`. Let's first update the consumer tests and add scenarios for expected the `Authorization` header. In `Consumer/tests/ApiTest.cs`:

```csharp
[Fact]
public async void GetAllProducts()
{
    // Arange
    pact.UponReceiving("A valid request for all products")
            .Given("products exist")
            .WithRequest(HttpMethod.Get, "/api/products")
            .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
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
public async void GetProduct()
{
    // Arange
    pact.UponReceiving("A valid request for a product")
            .Given("product with ID 10 exists")
            .WithRequest(HttpMethod.Get, "/api/products/10")
            .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
        .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new TypeMatcher(products[1]));

    await pact.VerifyAsync(async ctx => {
        var response = await ApiClient.GetProduct(10);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    });
}

[Fact]
public async void NoProductsExist()
{
    // Arange
    pact.UponReceiving("A valid request for all products")
            .Given("no products exist")
            .WithRequest(HttpMethod.Get, "/api/products")
            .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
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
public async void ProductDoesNotExist()
{
    // Arange
    pact.UponReceiving("A valid request for a product")
            .Given("product with ID 11 does not exist")
            .WithRequest(HttpMethod.Get, "/api/products/11")
            .WithHeader("Authorization", Match.Regex("Bearer 2019-01-14T11:34:18.045Z", "Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z"))
        .WillRespond()
            .WithStatus(HttpStatusCode.Unauthorized);

    await pact.VerifyAsync(async ctx => {
        var response = await ApiClient.GetProduct(11);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    });

    [Fact]
    public async void GetProductMissingAuthHeader()
    {
        // Arange
        pact.UponReceiving("A valid request for a product")
                .Given("No auth token is provided")
                .WithRequest(HttpMethod.Get, "/api/products/10")
            .WillRespond()
                .WithStatus(HttpStatusCode.Unauthorized);

        await pact.VerifyAsync(async ctx => {
            var response = await ApiClient.GetProduct(10);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        });
    }
}
```

We could implement the header as well but let's first briefly explain what's going on here. Instead of simply entering a header we use a `matcher`. A matcher
can be used when we don't necessarily need to test exact values but e.g. just check the type, or the pattern. In the example above we use a `Regex` matcher.
We provide an example value (which will end up in the pact file) as well as the pattern we want the value to match. This means that we can dynamically generate
the value and still have a passing test.

Before we implement the change, let's see what happens if we run it without implementing. The output is very long so it's been abbreviated to show the only one
the four test cases:

```console
[1] $ dotnet test                                                                                                                                                                                                  ✘
  Determining projects to restore...
  All projects are up-to-date for restore.
  consumer -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/netcoreapp3.1/consumer.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.46]     tests.ApiTest.ProductDoesNotExist [FAIL]
[xUnit.net 00:00:00.51]     tests.ApiTest.GetAllProducts [FAIL]
[xUnit.net 00:00:00.52]     tests.ApiTest.GetProduct [FAIL]
[xUnit.net 00:00:00.52]     tests.ApiTest.NoProductsExist [FAIL]
  Failed tests.ApiTest.ProductDoesNotExist [58 ms]
  Error Message:
   Assert.Equal() Failure
Expected: NotFound
Actual:   InternalServerError
  Stack Trace:
     at tests.ApiTest.<ProductDoesNotExist>b__8_0(IConsumerContext ctx) in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/ApiTest.cs:line 116
   at PactNet.Native.NativePactBuilder.VerifyAsync(Func`2 interact) in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/NativePactBuilder.cs:line 112
   at tests.ApiTest.ProductDoesNotExist() in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/ApiTest.cs:line 114
   at System.Threading.Tasks.Task.<>c.<ThrowAsync>b__139_0(Object state)
  Standard Output Messages:
 Mock server logs:

 [DEBUG][pact_mock_server::hyper_server] Creating pact request from hyper request
 [DEBUG][pact_mock_server::hyper_server] Extracting query from uri /api/products/11
 [INFO][pact_mock_server::hyper_server] Received request Request ( method: GET, path: /api/products/11, query: None, headers: Some({"host": ["localhost:9000"]}), body: Empty )
 [INFO][pact_matching] comparing to expected Request ( method: GET, path: /api/products/11, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
 [DEBUG][pact_matching]      body: ''
 [DEBUG][pact_matching]      matching_rules: MatchingRules { rules: {PATH: MatchingRuleCategory { name: PATH, rules: {} }, HEADER: MatchingRuleCategory { name: HEADER, rules: {"Authorization": RuleList { rules: [Regex("Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z")], rule_logic: And }} }} }
 [DEBUG][pact_matching]      generators: Generators { categories: {} }
 [DEBUG][pact_matching::matchers] String -> String: comparing '/api/products/11' to '/api/products/11' using Equality
 [DEBUG][pact_matching] expected content type = '*/*', actual content type = '*/*'
 [DEBUG][pact_matching] content type header matcher = 'None'
 [DEBUG][pact_matching] --> Mismatches: [HeaderMismatch { key: "Authorization", expected: "\"Bearer 2019-01-14T11:34:18.045Z\"", actual: "", mismatch: "Expected header 'Authorization' but was missing" }]
 [DEBUG][pact_mock_server::hyper_server] Request did not match: Request did not match - Request ( method: GET, path: /api/products/11, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )    0) Expected header 'Authorization' but was missing
```

The key information can be found in the `DEBUG` output:

```console
[INFO][pact_matching] comparing to expected Request ( method: GET, path: /api/products/11, query: None, headers: Some({"Authorization": ["Bearer 2019-01-14T11:34:18.045Z"]}), body: Missing )
 [DEBUG][pact_matching]      body: ''
 [DEBUG][pact_matching]      matching_rules: MatchingRules { rules: {PATH: MatchingRuleCategory { name: PATH, rules: {} }, HEADER: MatchingRuleCategory { name: HEADER, rules: {"Authorization": RuleList { rules: [Regex("Bearer \\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z")], rule_logic: And }} }} }
 ```

Let's update the consumer to pass the bearer token.

In `consumer/ApiClient.cs`:

```csharp
public async Task<HttpResponseMessage> GetAllProducts()
{
    using (var client = new HttpClient { BaseAddress = BaseUri })
    {
        try
        {
            client.DefaultRequestHeaders.Add("Authorization", AuthorizationHeaderValue()); 
            var response = await client.GetAsync($"/api/products");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception("There was a problem connecting to Provider API.", ex);
        }
    }
}

public async Task<HttpResponseMessage> GetProduct(int id)
{
    using (var client = new HttpClient { BaseAddress = BaseUri })
    {
        try
        {
            client.DefaultRequestHeaders.Add("Authorization", AuthorizationHeaderValue());
            var response = await client.GetAsync($"/api/products/{id}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception("There was a problem connecting to Provider API.", ex);
        }
    }
}

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
  consumer -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/src/bin/Debug/netcoreapp3.1/consumer.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 65 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (netcoreapp3.1)

```

The changes in the pact file is now that we have some logic for header matching:

```json
  "request": {
    "headers": {
      "Authorization": "Bearer 2019-01-14T11:34:18.045Z"
    },
    "matchingRules": {
      "header": {
        "Authorization": {
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
  }
```

Let's test the provider. Run the command:

```console
[1] $ dotnet test                                                                                                                                                                                                  ✘
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/netcoreapp3.1/provider.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (.NETCoreApp,Version=v5.0)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService
  Given no products exist
  Given product with ID 10 exists
  Given product with ID 10 exists
  Given product with ID 11 does not exist
  Given products exist
  A valid request for all products
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)
  A request for a product with no auth token provided
    returns a response which
      has status code 401 (FAILED)
      has a matching body (OK)
  A valid request for a product
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)
  A valid request for a product
    returns a response which
      has status code 404 (OK)
      has a matching body (OK)
  A valid request for all products
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)


Failures:

1) Verifying a pact between ApiClient and ProductService Given product with ID 10 exists - A request for a product with no auth token provided returns a response which
    1.1) has status code 401
           expected 401 but was 200

There were 1 pact failures

[xUnit.net 00:00:01.08]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [425 ms]
  Error Message:
   PactNet.PactFailureException : The verification process failed, see output for errors
  Stack Trace:
     at PactNet.Native.NativePactVerifier.Verify(String args) in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/NativePactVerifier.cs:line 34
   at PactNet.Native.PactVerifier.Verify() in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/PactVerifier.cs:line 240
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 46
  Standard Output Messages:
 Invoking the pact verifier with args:
 --file
 /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/pacts/ApiClient-ProductService.json
 --state-change-url
 http://127.0.0.1:9001/provider-states
 --provider-name
 ProductService
 --hostname
 127.0.0.1
 --port
 9001
 --filter-consumer
 ApiClient
 --loglevel
 trace



Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (net5.0)
```

Now with the most recently added interactions where we are expecting a response of 401 when no authorization header is sent, we are getting 200...

## Step 9 - Implement authorisation on the provider

We will add a middleware to check the Authorization header and deny the request with `401` if the token is older than 1 hour.


In a new folder called `Middleware`, create a class called `AuthorizationMiddleware`:

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

Add the middleware in `Startup.cs`, includeing the `using` statement:

```csharp
using provider.Middleware;

...

// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    app.UseMiddleware<AuthorizationMiddleware>();
    app.UseRouting();
    app.UseEndpoints(e => e.MapControllers());
}
```

This means that a client must present an HTTP `Authorization` header that looks as follows:

```
Authorization: Bearer 2006-01-02T15:04
```

Let's test this out:

```console
[1] $ dotnet test                                                                                                                                                                                                  ✘
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/netcoreapp3.1/provider.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (.NETCoreApp,Version=v5.0)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService
  Given no products exist
  Given product with ID 10 exists
  Given product with ID 10 exists
  Given product with ID 11 does not exist
  Given products exist
  A valid request for all products
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)
  A request for a product with no auth token provided
    returns a response which
      has status code 401 (OK)
      has a matching body (OK)
  A valid request for a product
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)
  A valid request for a product
    returns a response which
      has status code 404 (FAILED)
      has a matching body (OK)
  A valid request for all products
    returns a response which
      has status code 200 (FAILED)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (FAILED)
      has a matching body (FAILED)


Failures:

1) Verifying a pact between ApiClient and ProductService Given no products exist - A valid request for all products returns a response which
    1.1) has a matching body
           / -> Expected body Present(2 bytes) but was empty
    1.2) has status code 200
           expected 200 but was 401
    1.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
2) Verifying a pact between ApiClient and ProductService Given product with ID 10 exists - A valid request for a product returns a response which
    2.1) has a matching body
           / -> Expected body Present(65 bytes) but was empty
    2.2) has status code 200
           expected 200 but was 401
    2.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''
3) Verifying a pact between ApiClient and ProductService Given product with ID 11 does not exist - A valid request for a product returns a response which
    3.1) has status code 404
           expected 404 but was 401
4) Verifying a pact between ApiClient and ProductService Given products exist - A valid request for all products returns a response which
    4.1) has a matching body
           / -> Expected body Present(130 bytes) but was empty
    4.2) has status code 200
           expected 200 but was 401
    4.3) includes header 'Content-Type' with value '"application/json; charset=utf-8"'
           Expected header 'Content-Type' to have value '"application/json; charset=utf-8"' but was ''

There were 4 pact failures

[xUnit.net 00:00:00.88]     tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [FAIL]
  Failed tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer [313 ms]
  Error Message:
   PactNet.PactFailureException : The verification process failed, see output for errors
  Stack Trace:
     at PactNet.Native.NativePactVerifier.Verify(String args) in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/NativePactVerifier.cs:line 34
   at PactNet.Native.PactVerifier.Verify() in /Users/erikdanielsen/work/dius/pact-net/src/PactNet.Native/PactVerifier.cs:line 240
   at tests.ProductTest.EnsureProviderApiHonoursPactWithConsumer() in /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/ProductTest.cs:line 46
  Standard Output Messages:
 Invoking the pact verifier with args:
 --file
 /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/pacts/ApiClient-ProductService.json
 --state-change-url
 http://127.0.0.1:9001/provider-states
 --provider-name
 ProductService
 --hostname
 127.0.0.1
 --port
 9001
 --filter-consumer
 ApiClient
 --loglevel
 trace



Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (net5.0)
```

Oh, dear. _More_ tests are failing. Can you understand why?

## Step 10 - Request Filters on the Provider

Because our pact file has static data in it, our bearer token is now out of date, so when Pact verification passes it to the Provider we get a `401`. There are multiple ways to resolve this - mocking or stubbing out the authentication component is a common one. In our use case, we are going to use a process referred to as _Request Filtering_, using a `RequestFilter`.

_NOTE_: This is an advanced concept and should be used carefully, as it has the potential to invalidate a contract by bypassing its constraints. See https://github.com/DiUS/pact-jvm/blob/master/provider/junit/README.md#modifying-the-requests-before-they-are-sent for more details on this.

The approach we are going to take to inject the header is as follows:

1. If we receive any Authorization header, we override the incoming request with a valid (in time) Authorization header, and continue with whatever call was being made
1. If we don't receive an Authorization header, we do nothing

_NOTE_: We are not considering the `403` scenario in this example.

We'll implement this as a Middleware, similarly to have we deal with `provider states`.

In the folder `Provider/tests/Middleware` add a new file called `AuthTokenRequestFilter.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace tests.Middleware
{
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

Then wire the new middleware in `Provider/tests/Middleware/AuthTokenRequestFilter.cs`

```csharp
// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseMiddleware<ProviderStateMiddleware>();
    app.UseMiddleware<AuthTokenRequestFilter>();
    _proxy.Configure(app, env);
}
```

We can now run the Provider tests

```console
[1] $ dotnet test                                                                                                                                                                                                  ✘
  Determining projects to restore...
  All projects are up-to-date for restore.
  provider -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/src/bin/Debug/netcoreapp3.1/provider.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (.NETCoreApp,Version=v5.0)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between ApiClient and ProductService
  Given no products exist
  Given product with ID 10 exists
  Given product with ID 10 exists
  Given product with ID 11 does not exist
  Given products exist
  A valid request for all products
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)
  A request for a product with no auth token provided
    returns a response which
      has status code 401 (OK)
      has a matching body (OK)
  A valid request for a product
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)
  A valid request for a product
    returns a response which
      has status code 404 (OK)
      has a matching body (OK)
  A valid request for all products
    returns a response which
      has status code 200 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)



Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v3/Provider/tests/bin/Debug/net5.0/tests.dll (net5.0)
```

