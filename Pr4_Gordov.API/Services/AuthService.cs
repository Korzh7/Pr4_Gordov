using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pr4_Gordov.API.Data;
using Pr4_Gordov.API.DTO.Auth;
using Pr4_Gordov.API.Models;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Pr4_Gordov.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _hasher = new();
        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            if(request.Login == null || request.Password == null){
                return null;
            }
            if (request.Password != request.ConfirmPassword)
            {
                return null;
            }
            if(await _context.Users.AnyAsync(u=>u.Login == request.Login))
            {
                return null;
            }
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (role == null) return null;
            var user = new User
            {
                Login = request.Login,
                RoleId = role.Id,
                CreatedAt = DateTime.UtcNow
                
               
            };
            user.PasswordHash = _hasher.HashPassword(user, request.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new AuthResponse(
                Token: GenerateToken(user, role.Name),
                Login: user.Login,
                Role: role.Name
            );
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            if(request.Login == null || request.Password == null)
            {
                return null;
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Login == request.Login);
            if (user == null) return null;

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed) return null;

            return new AuthResponse(GenerateToken(user, user.Role.Name), user.Login, user.Role.Name);
        }

        private string GenerateToken(User user, string roleName)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, roleName)
            };
            var key = _configuration["Jwt:Key"];
            if(key == null) key = "SuperSecretKeyForGordovPractice2026!Key";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(
                issuer:"Pr4_Gordov",
                audience: "Pr4_GordovClient",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );
            string jwtTokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            return jwtTokenString;
        }
        
    }
}

    