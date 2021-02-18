using System;
using System.Threading;
using System.Threading.Tasks;
using Advertisement.Application.Common;
using Advertisement.Application.Identity.Contracts;
using Advertisement.Application.Identity.Interfaces;
using Advertisement.Application.Repositories;
using Advertisement.Application.Services.User.Contracts;
using Advertisement.Application.Services.User.Contracts.Exceptions;
using Advertisement.Application.Services.User.Interfaces;
using Advertisement.Domain.Shared.Exceptions;

namespace Advertisement.Application.Services.User.Implementations
{
    public sealed class UserServiceV1 : IUserService
    {
        private readonly IRepository<Domain.User, string> _repository;
        private readonly IIdentityService _identityService;

        public UserServiceV1(IRepository<Domain.User, string> repository, IIdentityService identityService)
        {
            _repository = repository;
            _identityService = identityService;
        }

        public async Task<Register.Response> Register(Register.Request registerRequest, CancellationToken cancellationToken)
        {
            //TODO проверить на дубликаты

            var response = await _identityService.CreateUser(
                new CreateUser.Request
                {
                    Username = registerRequest.Username, Password = registerRequest.Password,
                    Role = RoleConstants.UserRole
                }, cancellationToken);

            if (response.IsSuccess)
            {
                var domainUser = new Domain.User
                {
                    Id = response.UserId,
                    FirstName = registerRequest.FirstName,
                    LastName = registerRequest.LastName,
                    MiddleName = registerRequest.MiddleName,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.Save(domainUser, cancellationToken);

                return new Register.Response
                {
                    UserId = response.UserId
                };
            }

            throw new UserRegisteredException(string.Join(';', response.Errors));
        }

        public async Task Update(Update.Request request, CancellationToken cancellationToken)
        {
            var domainUser = await _repository.FindById(request.Id, cancellationToken);
            if (domainUser == null)
            {
                throw new UserNotFoundException($"Пользователь с идентификатором {request.Id} не найден");
            }

            var currentUserId = await _identityService.GetCurrentUserId(cancellationToken);
            if (domainUser.Id != currentUserId)
            {
                throw new NoRightsException("Нет прав");
            }

            domainUser.FirstName = request.FirstName;
            domainUser.LastName = request.LastName;
            domainUser.MiddleName = request.MiddleName;
            domainUser.UpdatedAt = DateTime.UtcNow;

            await _repository.Save(domainUser, cancellationToken);
        }

        //public async Task<Login.Response> Login(Login.Request loginRequest, CancellationToken cancellationToken)
        //{
        //    //var user = await _repository.FindWhere(u => u.Name == loginRequest.Name, cancellationToken);
        //    //if (user == null)
        //    //{
        //    //    throw new UserNotFoundException(loginRequest.Name);
        //    //}

        //    //if (!user.Password.Equals(loginRequest.Password))
        //    //{
        //    //    throw new NoRightsException("Нет прав");
        //    //}            
            
        //    //var claims = new List<Claim>
        //    //{
        //    //    new(ClaimTypes.Name, loginRequest.Name),
        //    //    new(ClaimTypes.NameIdentifier, user.Id.ToString()) 
        //    //};
            
        //    //var token = new JwtSecurityToken
        //    //(
        //    //    claims: claims,
        //    //    expires: DateTime.UtcNow.AddDays(60),
        //    //    notBefore: DateTime.UtcNow,
        //    //    signingCredentials: new SigningCredentials(
        //    //        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Token:Key"])),
        //    //        SecurityAlgorithms.HmacSha256
        //    //    )
        //    //);

        //    //return new Login.Response
        //    //{
        //    //    Token = new JwtSecurityTokenHandler().WriteToken(token)
        //    //};

        //    throw new NotImplementedException();
        //}
    }
}