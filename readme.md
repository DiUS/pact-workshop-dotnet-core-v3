# Example .NET Core Project for Pact Workshop

- [Prerequisites](#prerequisites)
- [Workshop Steps](#workshop-steps)
  - [Step 1 - Fork the Repo & Explore the Code!](#step-1---fork-the-repo--explore-the-code)
    - [CompletedSolution](#completedsolution)
    - [YourSolution](#yoursolution)
  - [Step 2 - Understanding The Consumer Project](#step-2---understanding-the-consumer-project)
    - [Step 2.1 - Start the Provider API Locally](#step-21---start-the-provider-api-locally)
    - [Step 2.2 - Execute the Consumer](#step-22---execute-the-consumer)
  - [Step 3 - Testing the Consumer Project with Pact](#step-3---testing-the-consumer-project-with-pact)
    - [Step 3.1 - Creating a Test Project for Consumer with XUnit](#step-31---creating-a-test-project-for-consumer-with-xunit)
    - [Step 3.2 - Configuring the Mock HTTP Pact Server on the Consumer](#step-32---configuring-the-mock-http-pact-server-on-the-consumer)
    - [Step 3.3 - Creating Your First Pact Test for the Consumer Client](#step-33---creating-your-first-pact-test-for-the-consumer-client)
  - [Step 4 - Testing the Provider Project with Pact](#step-4---testing-the-provider-project-with-pact)
    - [Step 4.1 - Creating a Provider State HTTP Server](#step-41---creating-a-provider-state-http-server)
    - [Step 4.2 - Creating the Provider API Pact Test](#step-42---creating-the-provider-api-pact-test)
    - [Step 4.2.1 - Creating the XUnitOutput Class](#step-421---creating-the-xunitoutput-class)
    - [Step 4.3 - Running Your Provider API Pact Test](#step-43---running-your-provider-api-pact-test)
    - [Step 4.3.1 - Start Your Provider API Locally](#step-431---start-your-provider-api-locally)
    - [Step 4.3.2 - Run your Provider API Pact Test](#step-432---run-your-provider-api-pact-test)
  - [Step 5 - Missing Consumer Pact Test Cases](#step-5---missing-consumer-pact-test-cases)

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

# Prerequisites

This workshop while written with .NET Core is not specifically about it so in-depth knowledge of .NET Core is not required if you can
write code in any other language you should be fine.

However before taking part in this workshop please make sure you have:

* [.NET Core SDK](https://www.microsoft.com/net/download/)
* An account at [Github.com](www.github.com)!
* A text editor/IDE that supports .NET Core. Check out [VSCode](https://code.visualstudio.com/)

# Add the Pacflow Nuget repository to Visual Studio/NuGet.Config

In order to use the 4.0.0-beta version of Pact.Net you need to add the Pacflow Nuget repository to Visual Studio and/or your Nuget.Config
file so that the libraries can be downloaded.

* For Visual Studio add `https://pactfoundation.jfrog.io/artifactory/api/nuget/default-nuget-local` as a Package Source
* For NuGet.Config (found at %appdata%\NuGet\NuGet.Config (Windows) or ~/.config/NuGet/NuGet.Config (Mac/Linux)) add
`<add key="ArtifactoryNuGetV3" value="https://pactfoundation.jfrog.io/artifactory/api/nuget/v3/default-nuget-local" protocolVersion="3" />`

# Workshop Steps

## Step 1 - Fork the Repo & Explore the Code!

Create a fork of [pact-workshop-dotnet-core-v2](https://github.com/dius/pact-workshop-dotnet-core-v2) and familiarise yourself with
its contents. There are two main folders to be aware of:

### CompletedSolution

This folder contains a complete sample solution for the workshop so if you get stuck at any point or are unsure what to do next take a look
in here and you will see all the completed code for guidance.

Within the folder is a Consumer project in the **Consumer/src** folder which is a simple .NET Core console application that connects to the
Provider project which is in the **Provider/src** folder and is an ASP.NET Core Web API. Both projects also have a **tests/** folder which
is where the [Pact](https://docs.pact.io/) tests for both projects exist.

### YourSolution

This folder follows the same structure as the *CompletedSolution/* folder except for the *tests/* folders are empty! During this workshop you
will be creating the test projects using [Pact](https://docs.pact.io/) to test both the *Consumer* project and the *Provider* project.

## Step 2 - Understanding The Consumer Project

The *Consumer* is a .NET Core console application which validates date & time strings by making requests to our *Provider* API. Take a look
at the code. You might notice before we can run the project successfully we need the Provider API running locally.

### Step 2.1 - Start the Provider API Locally

Using the command line navigate to:

```
[RepositoryRoot]/YourSolution/Provider/src/
```

Once in the Provider */src/* directory first do a ```dotnet restore``` at the command line to pull down the dependencies required for the project.
Once that has completed run ```dotnet run``` this will start your the Provider API. Now check that everything is working O.K. by navigating to
the URL below in your browser:

```
http://localhost:9000/api/provider?validDateTime=05/01/2018
```

If your request is successful you should see in your browser:

```
{"test":"NO","validDateTime":"05-01-2018 00:00:00"}
```

If you see the above leave the Provider API running then you are ready to try out the consumer.

#### NB: Potential Error

If you get a **404** error check that the path ```[RepositoryRoot]/YourSolution/data``` exists with a text file in it called **somedata.txt** in it. We will
talk about this file later on.

### Step 2.2 - Execute the Consumer

With the Provider API running open another command line instance and navigate to:

```
[RepositoryRoot]/YourSolution/Consumer/src/
```

Once in the directory run another ```dotnet restore``` to pull down the dependencies for the Consumer project. Once this is completed at the command line
type in ```dotnet run``` you should see output:

```
MyPc:src thomas.shipley$ dotnet run
-------------------
Running consumer with args: dateTimeToValidate = 05/01/2018, baseUri = http://localhost:9000
To use with your own parameters:
Usage: dotnet run [DateTime To Validate] [Provider Api Uri]
Usage Example: dotnet run 01/01/2018 http://localhost:9000
-------------------
Validating date...
{"test":"NO","validDateTime":"05-01-2018 00:00:00"}
...Date validation complete. Goodbye.
```

If you see output similar to above in your command line then the consumer is now running successfully! If you want to now you can experiment with passing in
parameters different to the defaults.

## Step 3 - Testing the Consumer Project with Pact

Now we have tested the Provider API and Consumer run successfully on your machine we can start to create our Pact tests. Pact files are **Consumer Driven**
that is to say, they work by the *Consumer* defining in there Pact tests first what they expect from a provider which can be verified by the *Provider*.
So let's follow this convention and create our *Consumer* tests first.

### Step 3.1 - Creating a Test Project for Consumer with XUnit

Pact cannot execute tests on its own it needs a test runner project. For this workshop, we will be using [XUnit](https://xunit.github.io/) to create the project
navigate to ```[RepositoryRoot]/YourSolution/Consumer/tests``` and run:

```
dotnet new xunit
```

This will create an empty XUnit project with all the references you need... expect Pact. Depending on what OS you are completing this workshop on you will need
to run one of the following commands:

```
dotnet add package PactNet --version 4.0.0-beta
dotnet add package PactNet.Native --version 0.1.0-beta
```

Finally you will need to add a reference to the Consumer Client project src code. So again
at the same command line type and run the command:

```
dotnet add reference ../src/consumer.csproj
```

This will allow you to access public code from the Consumer Client project which you will
need to do to test the code!

Once this command runs successfully you will have in ```[RepositoryRoot]/YourSolution/Consumer/tests``` an empty .NET Core XUnit Project with Pact
and we can begin to setup Pact!

### Step 3.2 - Configuring the Mock HTTP Pact Server on the Consumer

Pact works by placing a mock HTTP server between the consumer and provider(s) in an application to handle mocked provider interactions on the consumer
side and replay this actions on the provider side to verify them. With previous versions of PactNet this was something we had to set up ourselves but
with version 4.0.0 it's integrated into the library so no additional setup is ncessary.

### Step 3.3 - Creating Your First Pact Test for the Consumer Client

Update the test class added by the ```dotnet new xunit``` command to be named ```ConsumerPactTests``` and update the file name to match. 
With that done update the constructor to initialise the Pact

```csharp
using System;
using System.Net;
using System.Net.Http;
using Xunit;
using Consumer;
using PactNet;
using PactNet.Native;
using Xunit.Abstractions;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace tests
{
    public class ConsumerPactTests
    {
        private IPactBuilderV3 pact;

        public ConsumerPactTests(ITestOutputHelper output)
        {
            var Config = new PactConfig
            {
                PactDir = @"..\..\..\..\..\pacts",
                LogDir = @".\pact_logs",
                Outputters = new[] { new XUnitOutput(output) },
                DefaultJsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
            };

            pact = Pact.V3("Consumer", "Provider", Config).UsingNativeBackend();
        }
    }
}
```

The constructor is doing a couple of things right now:

  * It creates a PactConfig object which allows us to specify:
    * The Pact files will be generated and overwritten too ([RepositoryRoot]/pacts).
    * The Pact Log files will be written to the executing directory.

  * Creates a `Pact` object that follows the Pact Specification v3
    * Define the name of our Consumer project (Consumer) which will be used in other Pact Test projects.
    * Define the relationships our Consumer project has with others. In this case, just one called "Provider" this name will map to the same name used in the Provider Project Pact tests.
    * Tells Pact to use the Rust based backend to run the tests `UsingNativeBackend()`

All the Pact tests added during this workshop will follow the same three steps:

1. Mock out an interaction with the Provider API.
2. Interact with the mocked out interaction using our Consumer code.
3. Assert the result is what we expected.

There will a compilation error related to the ```XUnitOutput``` class which is still missing, we'll add that next

#### Step 3.3.1 - Add XUnitOutput class to capture standard out from Rust process

Unfortunately XUnit does not capture output from standard out by default so we need to add this
manually. Create a new class file named ```XUnitOutput``` with the following content:

```csharp
using System;
using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace tests
{
    public class XUnitOutput : IOutput
    {
        private readonly ITestOutputHelper _output;

        public XUnitOutput(ITestOutputHelper output)
        {
            _output = output;
        }

        public void WriteLine(string line)
        {
            _output.WriteLine(line);
        }
    }
}
```

This should fix the compilation issue with ```ConsumerPactTest```

#### Step 3.3.1 - Mocking an Interaction with the Provider

For the first test, we shall check that if we pass an invalid date string to our Consumer
that the Provider API will return a ```400``` response and a message explaining why the
request is invalid.

Create a test in ```ConsumerPactTests``` called ```ItHandlesInvalidDateParam()``` and
using the code below mock out our HTTP request to the Provider API which will return a
```400```:

```csharp
[Fact]
public async void ItHandlesInvalidDateParam()
{
    // Arange
    var invalidRequestMessage = "validDateTime is not a date or time";
    pact.UponReceiving("A invalid GET request for Date Validation with invalid date parameter")
            .Given("There is data")
            .WithRequest(HttpMethod.Get, "/api/provider")
            .WithQuery("validDateTime", "lolz")
        .WillRespond()
            .WithStatus(HttpStatusCode.BadRequest)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new { message = invalidRequestMessage });
}
```

The code above uses the ```pact``` to setup our mocked response.
Breaking it down by the different method calls:

* ```UponReceiving("")```

When this method executes it will add a description of what the mocked HTTP request
represents to the Pact file. It is important to be accurate here as this message is what
will be shown when a test fails to help a developer understand what went wrong.

* ```Given("")```

This workshop will talk more about the Given method when writing the Provider API Pact test
but for now, it is important to know that the Given method manages the state that your test
requires to be in place before running. In our example, we require the Provider API to
have some data. The Provider API Pact test will parse these given statements and map
them to methods which will execute code to setup the required state(s).

* ```WithRequest(HttpMethod.Get, "/api/provider")```

Here is where the configuration for your mocked HTTP request is added. In our example
we have added what *Method* the request is (Get) the *Path* the request is made to  (/api/provider/)

* ```WithQuery("validDateTime", "lolz")```

The query parameters passed to the endpoint as key value pairs

* ```WillRespond()```

Used to indicate that the start of the response back from the Provider API

* WithStatus(HttpStatusCode.BadRequest)

The response will have an HTTP status code of ```400```

* With JsonBody(new { message = invalidRequestMessage })

Defines the body of the response message

All the methods above on running the test will generate a *Pact file* which will be used
by the Provider API to make the same requests against the actual API to ensure the responses
match the expectations of the Consumer.

#### Step 3.3.2 - Completing Your First Consumer Test

With the mocked response setup the rest of the test can be treated like any other test
you would write; perform an action and assert the result:

```csharp
[Fact]
public async void ItHandlesInvalidDateParam()
{
    // Arange
    var invalidRequestMessage = "validDateTime is not a date or time";
    pact.UponReceiving("A invalid GET request for Date Validation with invalid date parameter")
            .Given("There is data")
            .WithRequest(HttpMethod.Get, "/api/provider")
            .WithQuery("validDateTime", "lolz")
        .WillRespond()
            .WithStatus(HttpStatusCode.BadRequest)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new { message = invalidRequestMessage });

    // Act & Assert
    await pact.VerifyAsync(async ctx => {
        var response = await ConsumerApiClient.ValidateDateTimeUsingProviderApi("lolz", ctx.MockServerUri);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(invalidRequestMessage, body);
    });

}
```

With the updated test above it will make a request using our Consumer client and get the
mocked interaction back which we assert on to confirm the error message is the one we
expect.

Now all that is left to do is run your test. From the
```[RepositoryRoot]/YourSolution/Consumer/tests/``` directory run the ```dotnet test```
command at the command line. If successful you should see some output like this:

```
$ dotnet test
  Determining projects to restore...
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/YourSolution/Consumer/src/consumer.csproj (in 80 ms).
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/YourSolution/Consumer/tests/tests.csproj (in 460 ms).
  consumer -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/YourSolution/Consumer/src/bin/Debug/netcoreapp3.1/consumer.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/YourSolution/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/YourSolution/Consumer/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
```

If you now navigate to ```[RepositoryRoot]/pacts``` you will see the pact file your test
generated. Take a moment to have a look at what it contains which is a JSON representation
of the mocked our requests your test made.

With your Consumer Pact Test passing and your new Pact file we can now create the Provider
Pact test which will validate your mocked responses match actual responses from the
Provider API.

## Step 4 - Testing the Provider Project with Pact

Navigate to the ```[RepositoryRoot]/YourSolution/Provider/tests``` directory in your
command line and create another new XUnit project by running the command
```dotnet new xunit```. Once again you will also need to add the correct version of
the PactNet package using one of the command line commands below:

```
dotnet add package PactNet --version 4.0.0-beta
dotnet add package PactNet.Native --version 0.1.0-beta
```

With all the packages added to our Provider API test project, we are ready to move onto
the next step; hooking into the application so we can manage test environment state.

### Step 4.1 - Managing Provider State

The Pact tests for the Provider API will need to do two things:

1. Manage the state of the Provider API as dictated by the Pact file.
2. Communicate with the Provider API to verify that the real responses for HTTP requests
defined in the Pact file match the mocked ones.

For the first point, we need to create an HTTP API used exclusively by our tests to manage
the transitions in the state. The first step is to inject a simple api endpoint into your
application.

#### Step 4.1.1 - Injecting API endpoint to Manage Provider State

First, navigate to your new Provider Tests project
(```[RepositoryRoot]/YourSolution/Provider/tests/```) and create a file and corresponding
class called ```TestStartup.cs```. In which we will proxy the application Startup to inject
middleware:

``` csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using tests.Middleware;
using Microsoft.AspNetCore.Hosting;
using Provider;

namespace tests
{
    public class TestStartup
    {
        private Startup _proxy;

        public TestStartup(IConfiguration configuration)
        {
            _proxy = new Startup(configuration);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _proxy.ConfigureServices(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ProviderStateMiddleware>();
            _proxy.Configure(app, env);
        }
    }
}
```

When you created the class above you might have noticed that the compiler has found a
compilation error because we haven't created the ProviderStateMiddleware class yet.

#### Step 4.1.2 - Creating a The Provider State Middleware

When creating a Pact test for a Provider your test needs its own API. The reason for
this is so it can manage the state of your API based on what the Pact file needs for each
request. This might be actions like ensuring a user is in the database or a user has
permission to access a resource.

Above we took the first step to create this API for our tests to access but currently
it both doesn't compile and even if we removed the ```app.UseMiddleware``` line it 
wouldn't do anything. We need to create a way for the API to manage the states required
by our tests. We will do this by creating a piece of middleware (similar to a controller)
that handles requests to the path ```/provider-states/```.

##### Step 4.1.2.1 - Creating the ProviderState Class

First create a new folder at ```[RepositoryRoot]/YourSolution/Provider/tests/Middleware```
and create a file and corresponding class called ```ProviderState.cs``` and add the
following code:

```csharp
namespace tests.Middleware
{
    public class ProviderState
    {
        public string Consumer { get; set; }
        public string State { get; set; }
    }
}
```

This is a simple class which represents the data sent to the ```/provider-states/``` path.
The first property will store the name of *Consumer* who is requesting the state change.
Which in our case is **Consumer**. The second property stores the state we want the
Provider API to be in.

With this class in place, we can create the middleware class.

##### Step 4.1.2.2 - Creating the ProviderStateMiddleware Class

Again at ```[RepositoryRoot]/YourSolution/Provider/tests/Middleware``` create a file and corresponding class called ```ProviderStateMiddleware.cs```. For now add the following code:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Newtonsoft.Json;

namespace tests.Middleware
{
    public class ProviderStateMiddleware
    {
        private const string ConsumerName = "Consumer";
        private readonly RequestDelegate _next;
        private readonly IDictionary<string, Action> _providerStates;

        public ProviderStateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value == "/provider-states/")
            {
                this.HandleProviderStatesRequest(context);
                await context.Response.WriteAsync(String.Empty);
            }
            else
            {
                await this._next(context);
            }
        }

        private async Task HandleProviderStatesRequestAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            if (context.Request.Method.ToUpper() == HttpMethod.Post.ToString().ToUpper() &&
                context.Request.Body != null)
            {
                string jsonRequestBody = String.Empty;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    jsonRequestBody = await reader.ReadToEndAsync();
                }

                var providerState = JsonConvert.DeserializeObject<ProviderState>(jsonRequestBody);

                //A null or empty provider state key must be handled
                if (providerState != null && !String.IsNullOrEmpty(providerState.State))
                {
                    _providerStates[providerState.State].Invoke();
                }
            }
        }
    }
}
```

The code above gives us a way to handle requests to the ```/provider-states/``` path and
based on the ```ProviderState.State``` requested run some associated code but in the code
above the ```_providerStates``` is empty so let's update the constructor to set up two states
and the associated code. The states to be added are:

1. "There is data"

This state will create a text file called ```somedata.txt``` in your operation system's temporary
directory. We used this directory because we experienced some inconsistencies between different
operation systems when using relative paths.

2. "There is no data"

This state will delete the text file ```somedata.txt``` from your operating system's temporary
directory if it exists. This state is not currently used by our Consumer Pact test but could be 
if some more test cases were added ;).

The code for this looks like:

```csharp
public class ProviderStateMiddleware
{
        private const string ConsumerName = "Consumer";
        private readonly RequestDelegate _next;
        private readonly IDictionary<string, Action> _providerStates;

        public ProviderStateMiddleware(RequestDelegate next)
        {
            _next = next;
            _providerStates = new Dictionary<string, Action>
            {
                {
                    "There is no data",
                    RemoveAllData
                },
                {
                    "There is data",
                    AddData
                }
            };
        }

        private void RemoveAllData()
        {
            var deletePath = Path.Combine(DataPath(), "somedata.txt");

            if (File.Exists(deletePath))
            {
                File.Delete(deletePath);
            }
        }

        private void AddData()
        {
            var writePath = Path.Combine(DataPath(), "somedata.txt");

            if (!Directory.Exists(DataPath()))
            {
                Directory.CreateDirectory(DataPath());
            }

            if (!File.Exists(writePath))
            {
                File.Create(writePath);
            }
        }

        private string DataPath()
        {
            return Path.Combine(Path.GetTempPath(), "data");
        }
```

Now we have initialised our ```_providerStates``` field with the two states which map to
```AddData()``` and ```RemoveAllData()``` respectively. Now if our Consumer Pact test
contains the step:

```csharp
    _mockProviderService.Given("There is data");
```

When setting up a mock request our Provider API Pact test will map this to the
```AddData()``` method and create the ```somedata.txt``` file if it does not already exist.
If the mock defines the Given step as:

```csharp
    _mockProviderService.Given("There is no data");
```

Then the ```RemoveAllData()``` method will be called and if the ```somedata.txt``` file
exists it will be deleted.

With this code in place the ```ProviderStateMiddleware``` class should be completed and
look like:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Newtonsoft.Json;

namespace tests.Middleware
{
    public class ProviderStateMiddleware
    {
        private const string ConsumerName = "Consumer";
        private readonly RequestDelegate _next;
        private readonly IDictionary<string, Action> _providerStates;

        public ProviderStateMiddleware(RequestDelegate next)
        {
            _next = next;
            _providerStates = new Dictionary<string, Action>
            {
                {
                    "There is no data",
                    RemoveAllData
                },
                {
                    "There is data",
                    AddData
                }
            };
        }

        private void RemoveAllData()
        {
            var deletePath = Path.Combine(DataPath(), "somedata.txt");

            if (File.Exists(deletePath))
            {
                File.Delete(deletePath);
            }
        }

        private void AddData()
        {
            var writePath = Path.Combine(DataPath(), "somedata.txt");

            if (!Directory.Exists(DataPath()))
            {
                Directory.CreateDirectory(DataPath());
            }

            if (!File.Exists(writePath))
            {
                File.Create(writePath);
            }
        }

        private string DataPath()
        {
            return Path.Combine(Path.GetTempPath(), "data");
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value == "/provider-states/")
            {
                await this.HandleProviderStatesRequestAsync(context);
                await context.Response.WriteAsync(String.Empty);
            }
            else
            {
                await this._next(context);
            }
        }

        private async Task HandleProviderStatesRequestAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            if (context.Request.Method.ToUpper() == HttpMethod.Post.ToString().ToUpper() &&
                context.Request.Body != null)
            {
                string jsonRequestBody = String.Empty;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    jsonRequestBody = await reader.ReadToEndAsync();
                }

                var providerState = JsonConvert.DeserializeObject<ProviderState>(jsonRequestBody);

                //A null or empty provider state key must be handled
                if (providerState != null && !String.IsNullOrEmpty(providerState.State))
                {
                    _providerStates[providerState.State].Invoke();
                }
            }
        }
    }
}
```

#### Step 4.1.2 - Configure custom XUnit output

The test constructor has an instance of ```ITestOutputHelper``` injected in order to capture
console output to standard out, unfortunately XUnit does not do this by default.

```csharp
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using PactNet;
using PactNet.Infrastructure.Outputters;
using tests.XUnitHelpers;
using Xunit;
using Xunit.Abstractions;

namespace tests
{
    public class ProviderApiTests
    {
        private string _pactServiceUri = "http://127.0.0.1:9001";
        private ITestOutputHelper _outputHelper { get; }

        public ProviderApiTests(ITestOutputHelper output)
        {
            _outputHelper = output;
        }

        [Fact]
        public void EnsureProviderApiHonoursPactWithConsumer()
        {
        }
    }
}
```

Create the folder ```[RepositoryRoot]/YourSolution/Provider/tests/XUnitHelpers``` and inside create the file 
```XUnitOutput.cs``` and the corresponding class which should look like:

```csharp
using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace tests.XUnitHelpers
{
    public class XUnitOutput : IOutput
    {
        private readonly ITestOutputHelper _output;

        public XUnitOutput(ITestOutputHelper output)
        {
            _output = output;
        }

        public void WriteLine(string line)
        {
            _output.WriteLine(line);
        }
    }
}
```

### Step 4.2 - Creating the Provider API Pact Test

With our Provider States API in place and managed by our test when it is run we can
complete our test. Update the ```EnsureProviderApiHonoursPactWithConsumer()``` test
to:

```csharp
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
        var pactFile = new FileInfo(@"../../../../../pacts/consumer-provider.json");
        pactVerifier.FromPactFile(pactFile)
            .WithProviderStateUrl(new Uri($"{_pactServiceUri}/provider-states"))
            .ServiceProvider("Provider", new Uri(_pactServiceUri))
            .HonoursPactWith("Consumer")
            .Verify();
    }
}
```

The **Act/Assert** part of this test creates a new
[PactVerifier](https://github.com/pact-foundation/pact-net/blob/master/PactNet/PactVerifier.cs)
instance which first uses a call to ```ProviderState``` to know where our Provider States
API is hosted. Next, the ```ServiceProvider``` method takes the name of the Provider being
verified in our case **Provider** and a URI to where it is hosted. Then the
```HonoursPactWith()``` method tells Pact the name of the consumer that generated the Pact
which needs to be verified with the Provider API - in our case **Consumer**.  Finally, in
our workshop, we point Pact directly to the Pact File (instead of hosting elsewhere) and 
call ```Verify``` to test that the mocked request and responses in the Pact file for our
Consumer and Provider match the real responses from the Provider API.

### Step 4.3 - Running Your Provider API Pact Test

Now we have a test in the Consumer Project which creates our Pact file based on its mock
requests to the Provider API and we have a Pact test in the Provider API which consumes
this Pact file to verify the mocks match the actual responses we should run the Provider
tests!

### Step 4.3.1 - Run your Provider API Pact Test

First, confirm you have a Pact file at ```[RepositoryRoot]/YourSolution/pacts``` called
consumer-provider.json.

Next, create a command line window and navigate to
```[RepositoryRoot]/YourSolution/Provider/tests``` and to run the tests type in and execute
the command below:

```
dotnet test
```

Once you run this command and it completes you will hopefully see some output which looks like:

```
  Determining projects to restore...
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/CompletedSolution/Provider/src/provider.csproj (in 75 ms).
  Restored /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/CompletedSolution/Provider/tests/tests.csproj (in 344 ms).
  provider -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/CompletedSolution/Provider/src/bin/Debug/netcoreapp3.1/provider.dll
  tests -> /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/CompletedSolution/Provider/tests/bin/Debug/netcoreapp3.1/tests.dll
Test run for /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/CompletedSolution/Provider/tests/bin/Debug/netcoreapp3.1/tests.dll (.NETCoreApp,Version=v3.1)
Microsoft (R) Test Execution Command Line Tool Version 16.11.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Verifying a pact between Consumer and Provider
  Given There is data
  Given There is no data
  Given There is data
  Given There is data
  A invalid GET request for Date Validation with invalid date parameter
    returns a response which
      has status code 400 (OK)
      includes headers
        "Content-Type" with value "application/json; charset=utf-8" (OK)
      has a matching body (OK)

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms - /Users/erikdanielsen/work/dius/pact-workshop-dotnet-core-v2/CompletedSolution/Provider/tests/bin/Debug/netcoreapp3.1/tests.dll (netcoreapp3.1)
```

Hopefully, you see the above output which means your Pact Provider test was successful!
At this point, you now have a working local example of a Pact test suite that tests
both the Consumer and Provider sides of an application but a few test cases are
missing...

## Step 5 - Missing Consumer Pact Test Cases

The Consumer Pact test suite only has one test in it. But there are a few test cases
which could also be implemented:

* It handles an empty date parameter.
* It handles having no data in the data folder.
* It parses a date correctly.

For the final step of this workshop take some time to update your Consumer Pact tests
to implement one or all of the test cases above. Once done generate a new Pact file
by running your Consumer Pact tests and validate your Pact file against the Provider
API. 

If you are struggling take a look at
```[RepositoryRoot]/CompletedSolution/Consumer/tests``` which contains the solutions
to each test case. But perhaps give it a go first!

# Copyright Notice & Licence 

This workshop is a port of the [Ruby Project for Pact Workshop](https://github.com/DiUS/pact-workshop-ruby-v2) with some
minor modifications. It is covered under the same Apache License 2.0 as the original Ruby workshop.
