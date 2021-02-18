using System.Threading;
using System.Threading.Tasks;
using Advertisement.Application.Services.User.Contracts;

namespace Advertisement.Application.Services.User.Interfaces
{
    public interface IUserService
    {
        Task<Register.Response> Register(Register.Request registerRequest, CancellationToken cancellationToken);
        Task Update(Update.Request request, CancellationToken cancellationToken);
    }
}