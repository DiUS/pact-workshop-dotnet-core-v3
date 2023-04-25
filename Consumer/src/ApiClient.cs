using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Consumer;

public class ApiClient
{
    private readonly Uri _baseUri;

    public ApiClient(Uri baseUri)
    {
        _baseUri = baseUri;
    }

    public async Task<HttpResponseMessage> GetAllProducts()
    {
        using var client = new HttpClient { BaseAddress = _baseUri };
        try
        {
            var response = await client.GetAsync($"/api/products");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception("There was a problem connecting to Provider API.", ex);
        }
    }

    public async Task<HttpResponseMessage> GetProduct(int id)
    {
        using var client = new HttpClient { BaseAddress = _baseUri };
        try
        {
            var response = await client.GetAsync($"/api/product/{id}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception("There was a problem connecting to Provider API.", ex);
        }
    }
}