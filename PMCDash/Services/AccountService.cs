using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PMCDash.Models;
using PMCDash.Helper;
namespace PMCDash.Services
{
    public class AccountService
    {
        private readonly JwtProviderHelper _jwtProvider;

        public AccountService(JwtProviderHelper jwtProvider)
        {
            _jwtProvider = jwtProvider;
        }
        public string SignIn(AuthRequestcs authRequestcs)
        {
            if (authRequestcs.UserName == "admin" && authRequestcs.Password == "1234")
                return _jwtProvider.CreateJwtToken(authRequestcs);
            return string.Empty;
        }
    }
}
