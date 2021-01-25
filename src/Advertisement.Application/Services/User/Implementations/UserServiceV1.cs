﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Advertisement.Application.Services.User.Contracts;
using Advertisement.Application.Services.User.Contracts.Exceptions;
using Advertisement.Application.Services.User.Interfaces;
using Advertisement.Domain.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Advertisement.Application.Services.User.Implementations
{
    public sealed class UserServiceV1 : IUserService
    {
        private readonly IRepository<Domain.User, int> _repository;
        private readonly IHttpContextAccessor _contextAccessor;

        private readonly IConfiguration _configuration;

        public UserServiceV1(IRepository<Domain.User, int> repository, IHttpContextAccessor contextAccessor, IConfiguration configuration)
        {
            _repository = repository;
            _contextAccessor = contextAccessor;
            _configuration = configuration;
        }

        public async Task<Domain.User> GetCurrent(CancellationToken cancellationToken)
        {
            var id = 
                _contextAccessor
                    .HttpContext
                    .User
                    .Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                ?.Value;

            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var intId = int.Parse(id);
            var user = await _repository.FindById(intId, cancellationToken);

            if (user == null)
            {
                throw new UserNotFoundException(intId);
            }

            return user;
        }

        public async Task<Register.Response> Register(Register.Request registerRequest, CancellationToken cancellationToken)
        {
            var user = new Domain.User
            {
                Name = registerRequest.Name,
                Password = registerRequest.Password,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.Save(user, cancellationToken);

            return new Register.Response
            {
                UserId = user.Id
            };
        }

        public async Task<Login.Response> Login(Login.Request loginRequest, CancellationToken cancellationToken)
        {
            var user = await _repository.FindWhere(u => u.Name == loginRequest.Name, cancellationToken);
            if (user == null)
            {
                throw new UserNotFoundException(loginRequest.Name);
            }

            if (!user.Password.Equals(loginRequest.Password))
            {
                throw new NoRightsException("Нет прав");
            }            
            
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, loginRequest.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()) 
            };
            
            var token = new JwtSecurityToken
            (
                claims: claims,
                expires: DateTime.UtcNow.AddDays(60),
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Token:Key"])),
                    SecurityAlgorithms.HmacSha256)
            );

            return new Login.Response
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }
    }
}