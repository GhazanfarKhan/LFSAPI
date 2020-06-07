using LFSAPI.Entities;
using LFSAPI.Responses;
using LFSAPI.Settings;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LFSAPI.Services
{
    public class TokenService
    {
        private readonly JWTSettings jwtSettings;
        public TokenService(JWTSettings _jwtSettings)
        {
            jwtSettings = _jwtSettings;
        }
        public string GenerateToken(LoginResponse user)
        {
            var claims = BuildClaims(user);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.JwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(jwtSettings.JwtExpireDays));

            var token = new JwtSecurityToken(
                jwtSettings.JwtIssuer,
                jwtSettings.JwtIssuer,
                claims,
                expires: expires,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private List<Claim> BuildClaims(LoginResponse user)
        {
            var claims = new List<Claim>
            {
                new Claim("Email", user.Email),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };
            return claims;
        }
    }
}
