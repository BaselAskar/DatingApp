using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Entities;
using API.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace API.Services
{
    public class TokenServices : ITokenServices
    {
        private SymmetricSecurityKey _key;
        public TokenServices(IConfiguration config)
        {
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
        }
        public string CreateToken(AppUser user)
        {
            //initial Claims for token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId,user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName,user.UserName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(2),
                SigningCredentials = new SigningCredentials(_key,SecurityAlgorithms.HmacSha512Signature)
            };

            var tokenHandlar = new JwtSecurityTokenHandler();

            var token = tokenHandlar.CreateToken(tokenDescriptor);

            return tokenHandlar.WriteToken(token);
        }
    }
}