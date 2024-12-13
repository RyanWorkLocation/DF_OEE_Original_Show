using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PMCDash.Models;
using System;
using System.Threading.Tasks;

namespace PMCDash.Filters
{
    public class ExceptionFilter : Attribute, IAsyncExceptionFilter
    {
        public Task OnExceptionAsync(ExceptionContext context)
        {
            //switch (context.Exception)
            //{

            //}
            context.Result = new ObjectResult(new ActionResponse<object>
            {
                Data = null,
                Message = context.Exception.Message
            });
            context.HttpContext.Response.StatusCode = 400;
            return Task.CompletedTask;
        }
    }
}
