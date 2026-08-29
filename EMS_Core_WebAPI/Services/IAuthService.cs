using EMS_Core_WebAPI.Models;
namespace EMS_Core_WebAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(Login dto, CancellationToken cancellationToken = default);
    }
}
