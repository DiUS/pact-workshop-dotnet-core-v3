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
            app.UseMiddleware<AuthTokenRequestFilter>(); // STEP_10
            _proxy.Configure(app, env);
        }
    }
}