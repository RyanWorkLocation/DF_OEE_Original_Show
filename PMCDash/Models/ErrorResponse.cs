using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PMCDash.Models
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }

        public string Message { get; set; }
    }
}
