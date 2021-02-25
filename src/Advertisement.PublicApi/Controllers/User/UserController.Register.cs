using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Advertisement.Application.Services.User.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Advertisement.PublicApi.Controllers.User
{
    public partial class UserController
    {
        public sealed class UserRegisterRequest
        {
            [Required]
            [MaxLength(30, ErrorMessage = "Максимальная длина логина не должна превышать 30 символов")]
            public string Username { get; set; }

            [Required]
            [MaxLength(100, ErrorMessage = "Максимальная длина имени не должна превышать 30 символов")]
            public string FirstName { get; set; }

            [Required]
            [MaxLength(100, ErrorMessage = "Максимальная длина фамилии не должна превышать 30 символов")]
            public string LastName { get; set; }

            [MaxLength(100, ErrorMessage = "Максимальная длина отчества не должна превышать 30 символов")]
            public string MiddleName { get; set; }
            
            [Required]
            public string Password { get; set; }
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Register(UserRegisterRequest request, CancellationToken cancellationToken)
        {
            var registrationResult = await _userService.Register(new Register.Request
            {
                Username = request.Username,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                MiddleName = request.MiddleName
            }, cancellationToken);
            
            return Created($"api/v1/users/{registrationResult.UserId}", new {});
        }
    }
}