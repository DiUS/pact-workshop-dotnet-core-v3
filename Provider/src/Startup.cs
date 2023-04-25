using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Provider.Repositories;

namespace Provider;

public static class Startup
{
    public static WebApplication WebApp(params string[] strings)
    {
        var builder = WebApplication.CreateBuilder(strings);
        builder
            .Services
            .AddSingleton<IProductRepository, ProductRepository>();
        
        var app = builder.Build();
        app.MapGet("/api/products", ([FromServices] IProductRepository repository) => repository.List());
        app.MapGet("/api/products/{id:int}",
            ([FromServices] IProductRepository repository, int id) => repository.Get(id));
        
        return app;
    }
}