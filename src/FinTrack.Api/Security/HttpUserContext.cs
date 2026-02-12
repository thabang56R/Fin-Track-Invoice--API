using System.Security.Claims;
using FinTrack.Infrastructure.Auditing;

namespace FinTrack.Api.Security;

public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _http;
    public HttpUserContext(IHttpContextAccessor http) => _http = http;

    public Guid? UserId
    {
        get
        {
            var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }
}
