using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Formatter
{
    public class JsonInputFormatter : InputFormatter
    {
        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead(InputFormatterContext context)
        {
            return base.CanRead(context);
        }

    }
}
