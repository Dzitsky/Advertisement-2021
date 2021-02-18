using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advertisement.Application.Identity.Contracts;
using Advertisement.Application.Identity.Interfaces;
using Advertisement.Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Advertisement.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public IdentityService(IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _configuration = configuration;
        }

        public Task<string> GetCurrentUserId(CancellationToken cancellationToken = default)
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            return Task.FromResult(_userManager.GetUserId(claimsPrincipal));
        }

        public async Task<bool> IsInRole(string userId, string role, CancellationToken cancellationToken = default)
        {
            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                //TODO Сделать норм Exception
                throw new Exception("Пользователь не найден");
            }

            return await _userManager.IsInRoleAsync(identityUser, role);
        }

        public async Task<CreateUser.Response> CreateUser(CreateUser.Request request, CancellationToken cancellationToken = default)
        {
            var existedUser = await _userManager.FindByNameAsync(request.Username);
            if (existedUser != null)
            {
                throw new ConflictException("Пользователь с таким именем уже существует");
            }

            var identityUser = new IdentityUser {UserName = request.Username};
            var identityResult = await _userManager.CreateAsync(identityUser, request.Password);

            if (identityResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(identityUser, request.Role);
                return new CreateUser.Response
                {
                    UserId = identityUser.Id,
                    IsSuccess = true
                };
            }

            return new CreateUser.Response
            {
                IsSuccess = false,
                Errors = identityResult.Errors.Select(x => x.Description).ToArray()
            };
        }

        public async Task<CreateToken.Response> CreateToken(CreateToken.Request request, CancellationToken cancellationToken = default)
        {
            var identityUser = await _userManager.FindByNameAsync(request.Username);
            if (identityUser == null)
            {
                //TODO это плохо!!!!!1!11!
                throw new Exception("Пользователь не найден");
            }

            var passwordCheckResult = await _userManager.CheckPasswordAsync(identityUser, request.Password);
            if (!passwordCheckResult)
            {
                throw new NoRightsException("Неправильный логин или пароль");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.NameIdentifier, identityUser.Id)
            };

            var userRoles = await _userManager.GetRolesAsync(identityUser);
            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken
            (
                claims: claims,
                expires: DateTime.UtcNow.AddDays(60),
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Token:Key"])),
                    SecurityAlgorithms.HmacSha256
                )
            );

            return new CreateToken.Response
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }
    }
}
