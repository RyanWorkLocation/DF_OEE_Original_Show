using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace PMCDash.Formatter
{
    public class JsonOutputFormatter : OutputFormatter
    {
      
        public JsonOutputFormatter()
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add("application/json");
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return base.CanWriteResult(context);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            return context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(context.Object, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}