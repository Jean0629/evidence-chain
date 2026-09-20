using EvidenceChain.Application.Auth;
using EvidenceChain.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EvidenceChain.Infrastructure.Auth
{
    public class TokenService(IConfiguration config) : ITokenService
    {
        public string GenerateToken(Custodian custodian)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, custodian.Id.ToString()),
            new Claim(ClaimTypes.Name, custodian.DisplayName),
            new Claim(ClaimTypes.Role, custodian.Role.ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:ExpirationMinutes")),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
