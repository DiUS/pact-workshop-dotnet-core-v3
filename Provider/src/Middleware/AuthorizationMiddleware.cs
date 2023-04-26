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