using Advertisement.Domain.Shared.Exceptions;

namespace Advertisement.Application.Services.User.Contracts.Exceptions
{
    public class UserRegisteredException : DomainException
    {
        public UserRegisteredException(string message) : base(message)
        {
        }
    }
}
