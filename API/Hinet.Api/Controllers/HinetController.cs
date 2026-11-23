using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hinet.Controllers
{
    [ApiController]
    [Authorize]
    public class HinetController : ControllerBase
    {
        public HinetController() { }

        protected Guid? UserId
        {
            get
            {
                var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(id, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }
        protected string FullName
        {
            get
            {
                return User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            }
        }


        protected List<string>? Roles
        {
            get
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var rs = new List<string>();
                if (!string.IsNullOrWhiteSpace(role))
                {
                    rs = role.Split(",").ToList();
                }
                return rs;
            }
        }

        protected bool HasRole(string role)
        {
            var lstRole = Roles;
            return lstRole != null && lstRole.Any() && lstRole.Contains(role);
        }

        protected string Uri
        {
            get
            {
                var uriBuilder = new UriBuilder(Request.Scheme, Request.Host.Host, Request.Host.Port ?? -1);
                if (uriBuilder.Uri.IsDefaultPort)
                {
                    uriBuilder.Port = -1;
                }
                return uriBuilder.Uri.AbsoluteUri;
            }

        }

        protected IEnumerable<string> ModelStateError
                => ModelState.Values.SelectMany(v => v.Errors.Select(x => x.ErrorMessage));

    }


}