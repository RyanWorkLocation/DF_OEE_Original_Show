using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PMCDash.Models;
namespace PMCDash.Filters
{
    public class AuthorizationFilter : Attribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authorizationHeader = context.HttpContext.Request.Headers["Authorization"].ToString().Split(" ");
            var scheme = authorizationHeader.First();
            var value = authorizationHeader.Last();
            if (scheme == "Token" && value == "StarBugs_Valid_Token")
            {
                return Task.CompletedTask;
            }

            var error = new ErrorResponse
            {
                StatusCode = 401,
                Message = "Invalid Token"
            };

            var accept = context.HttpContext.Request.Headers["Accept"].ToString();

            if (accept.Contains("application/json"))
            {
                context.Result = new ContentResult
                {
                    //Content = _cryptoUtil.Encrypt(JsonSerializer.Serialize(error, new JsonSerializerOptions
                    //{
                    //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    //}))
                    Content = "Error",
                };

                return Task.CompletedTask;
            }

            context.Result = new ObjectResult(error);

            return Task.CompletedTask;
        }
    }
}
