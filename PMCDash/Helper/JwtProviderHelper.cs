using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using PMCDash.Models;
using Microsoft.Extensions.Configuration;
namespace PMCDash.Helper
{

    public class JwtProviderHelper
    {
        private readonly IConfiguration _configuration;

        public JwtProviderHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public string CreateJwtToken(AuthRequestcs model)
        {
            var authTime = DateTime.UtcNow;
            var expires = authTime.AddDays(1);
            var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
            identity.AddClaims(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, model.UserName),
                new Claim(ClaimTypes.Role, "S"),
                new Claim(ClaimTypes.Expiration, expires.ToString("s")),
            });
            var signKey = _configuration.GetValue<string>("JwtBearerSettings:SignKey");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _configuration.GetValue<string>("JwtBearerSettings:Issuer"),
                Subject = identity,
                Expires = expires,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            });
            var serializeToken = tokenHandler.WriteToken(token);
            return serializeToken;
        }     
    }
}
