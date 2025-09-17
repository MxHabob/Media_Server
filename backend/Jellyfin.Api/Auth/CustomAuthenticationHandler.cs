using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Api.Auth
{
    /// <summary>
    /// Custom authentication handler wrapping the legacy authentication.
    /// </summary>
    public class CustomAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IAuthService _authService;
        private readonly IUserManager _userManager; // Added for PIN authentication
        private readonly ILogger<CustomAuthenticationHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAuthenticationHandler" /> class.
        /// </summary>
        /// <param name="authService">The jellyfin authentication service.</param>
        /// <param name="userManager">The user manager service.</param>
        /// <param name="options">Options monitor.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="encoder">The url encoder.</param>
        public CustomAuthenticationHandler(
            IAuthService authService,
            IUserManager userManager, // Added dependency
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger.CreateLogger<CustomAuthenticationHandler>();
        }

        /// <inheritdoc />
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                // Only handle header-based PIN auth here to avoid interfering with JSON controller actions
                if (Request.Headers.ContainsKey("X-Jellyfin-Pin-Auth"))
                {
                    string pin = string.Empty;
                    if (Request.Headers.TryGetValue("Pin", out var pinHeader))
                    {
                        pin = pinHeader.ToString();
                    }
                    else if (Request.HasFormContentType)
                    {
                        var form = await Request.ReadFormAsync().ConfigureAwait(false);
                        pin = form["Pin"].ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(pin))
                    {
                        var remoteIp = Context.Connection.RemoteIpAddress?.ToString();
                        var user = await _userManager.AuthenticateUserByPinAsync(pin, remoteIp ?? string.Empty, true).ConfigureAwait(false);
                        if (user == null)
                        {
                            return AuthenticateResult.Fail("Invalid PIN or expired subscription.");
                        }

                        var role = user.HasPermission(PermissionKind.IsAdministrator) ? UserRoles.Administrator : UserRoles.User;
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Role, role),
                            new Claim(InternalClaimTypes.UserId, user.Id.ToString("N", CultureInfo.InvariantCulture)),
                            new Claim(InternalClaimTypes.Token, Guid.NewGuid().ToString("N"))
                        };

                        var identity = new ClaimsIdentity(claims, Scheme.Name);
                        var principal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(principal, Scheme.Name);

                        return AuthenticateResult.Success(ticket);
                    }

                    return AuthenticateResult.NoResult();
                }

                // Fallback to existing username/password or API key authentication
                var authorizationInfo = await _authService.Authenticate(Request).ConfigureAwait(false);
                if (!authorizationInfo.HasToken)
                {
                    return AuthenticateResult.NoResult();
                }

                var authRole = UserRoles.User;
                if (authorizationInfo.IsApiKey
                    || (authorizationInfo.User?.HasPermission(PermissionKind.IsAdministrator) ?? false))
                {
                    authRole = UserRoles.Administrator;
                }

                var authClaims = new[]
                {
                    new Claim(ClaimTypes.Name, authorizationInfo.User?.Username ?? string.Empty),
                    new Claim(ClaimTypes.Role, authRole),
                    new Claim(InternalClaimTypes.UserId, authorizationInfo.UserId.ToString("N", CultureInfo.InvariantCulture)),
                    new Claim(InternalClaimTypes.DeviceId, authorizationInfo.DeviceId ?? string.Empty),
                    new Claim(InternalClaimTypes.Device, authorizationInfo.Device ?? string.Empty),
                    new Claim(InternalClaimTypes.Client, authorizationInfo.Client ?? string.Empty),
                    new Claim(InternalClaimTypes.Version, authorizationInfo.Version ?? string.Empty),
                    new Claim(InternalClaimTypes.Token, authorizationInfo.Token),
                    new Claim(InternalClaimTypes.IsApiKey, authorizationInfo.IsApiKey.ToString(CultureInfo.InvariantCulture))
                };

                var authIdentity = new ClaimsIdentity(authClaims, Scheme.Name);
                var authPrincipal = new ClaimsPrincipal(authIdentity);
                var authTicket = new AuthenticationTicket(authPrincipal, Scheme.Name);

                return AuthenticateResult.Success(authTicket);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogDebug(ex, "Error authenticating with {Handler}", nameof(CustomAuthenticationHandler));
                return AuthenticateResult.NoResult();
            }
            catch (SecurityException ex)
            {
                return AuthenticateResult.Fail(ex);
            }
        }
    }
}