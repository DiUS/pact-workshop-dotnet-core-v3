using System;
using System.Net.Http;
using System.Threading.Tasks;
using Consumer;

var client = new ApiClient(new Uri("http://localhost:9001"));
await Print(client.GetAllProducts(), "** Retrieving product list **");

const int productId = 10;
await Print(client.GetProduct(productId), $"** Retrieving product with id={productId} **");

async Task Print(Task<HttpResponseMessage> request, string message)
{
    Console.WriteLine(message);
    
    var response = await request;
    Console.WriteLine( $"Response.Code={response.StatusCode}, Response.Body={await response.Content.ReadAsStringAsync()}");
    Console.WriteLine();
}