using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace tests.Middleware
{   
    // STEP_10
    // public class AuthTokenRequestFilter
    // {
    //     private const string AuthorizationHeaderKey = "Authorization";
    //     private readonly RequestDelegate _next;

    //     public AuthTokenRequestFilter(RequestDelegate next)
    //     {
    //         _next = next;
    //     }

    //     public async Task Invoke(HttpContext context)
    //     {
    //         if (context.Request.Headers.ContainsKey(AuthorizationHeaderKey))
    //         {
    //             context.Request.Headers.Remove(AuthorizationHeaderKey);
    //             context.Request.Headers.Add(AuthorizationHeaderKey, HeaderValue());
    //         }
    //         await this._next(context);
    //     }

    //     private StringValues HeaderValue()
    //     {
    //         return $"Bearer {DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}";
    //     }
    // }
}