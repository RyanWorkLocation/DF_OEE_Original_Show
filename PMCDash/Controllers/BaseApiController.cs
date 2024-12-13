using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PMCDash.Filters;
using PMCDash.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
namespace PMCDash.Controllers
{
    [ApiController]
    [ExceptionFilter]
    [Authorize]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ActionResponse<object>), (int)HttpStatusCode.BadRequest)]
    public class BaseApiController : ControllerBase
    {
    }
}
