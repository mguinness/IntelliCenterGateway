using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliCenterGateway
{
    public class IPRequirement : IAuthorizationRequirement
    {
        public IPNetwork Network { get; }

        public IPRequirement(IPNetwork network)
        {
            Network = network;
        }
    }

    public class IPAddressHandler : AuthorizationHandler<IPRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IPAddressHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IPRequirement requirement)
        {
            var ctx = _httpContextAccessor.HttpContext;

            if (context.User.Identity.IsAuthenticated || requirement.Network.Contains(ctx.Connection.RemoteIpAddress))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
