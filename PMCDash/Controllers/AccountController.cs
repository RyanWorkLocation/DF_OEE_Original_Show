using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMCDash.Filters;
using PMCDash.Services;
using PMCDash.Models;
using Microsoft.AspNetCore.Authorization;
namespace PMCDash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : BaseApiController
    {
        private readonly AccountService _accountService;
        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// 登入使用者
        /// </summary>
        /// <param name="authRequest">提供帳號密碼 Ex : admin/1234</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public string Login(AuthRequestcs authRequest)
        {
            return _accountService.SignIn(authRequest);        
        }

        /// <summary>
        /// 驗證Token是否還在效期內
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("Validate")]
        public string ValidateUser()
        {
            return User.Identity.Name;
        }
    }
}
