using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace IntelliCenterGateway.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("[controller]/[action]")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Account/SignedOut");
        }

        [HttpPost("[controller]/[action]")]
        public IActionResult Token(string username, string password, string grant_type)
        {
            var users = _config.GetSection("Users").Get<Dictionary<string, string>>();

            if (users.ContainsKey(username) && users[username] == password && grant_type == "password")
            {
                var claims = new List<Claim>()
                {
                    new Claim(JwtRegisteredClaimNames.UniqueName, username)
                };

                var signingHandler = new JwtSecurityTokenHandler();

                var jwtToken = signingHandler.CreateJwtSecurityToken(
                    _config["Token:Issuer"],
                    _config["Token:Audience"],
                    new ClaimsIdentity(claims, "jwt"),
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddSeconds(_config.GetValue<int>("Token:ValidFor")),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Token:SigningKey"])), SecurityAlgorithms.HmacSha256));

                return Json(new
                {
                    token_type = "Bearer",
                    access_token = jwtToken.RawData,
                    expires_in = _config.GetValue<int>("Token:ValidFor")
                });
            }
            else
                return BadRequest();
        }
    }
}
