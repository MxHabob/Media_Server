using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.SubscriptionDtos;
using Jellyfin.Server.Implementations.Subscriptions;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Subscription management controller.
    /// </summary>
    [Route("Subscriptions")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public class SubscriptionController : BaseJellyfinApiController
    {
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionController"/> class.
        /// </summary>
        /// <param name="subscriptionManager">The subscription manager.</param>
        /// <param name="userManager">The user manager.</param>
        public SubscriptionController(ISubscriptionManager subscriptionManager, IUserManager userManager)
        {
            _subscriptionManager = subscriptionManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets all active subscription configurations.
        /// </summary>
        /// <response code="200">Subscription configurations returned.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>A list of subscription configurations.</returns>
        [HttpGet("Configurations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<SubscriptionConfigurationDto>>> GetConfigurations()
        {
            var configurations = await _subscriptionManager.GetActiveConfigurationsAsync().ConfigureAwait(false);
            var result = configurations.Select(MapToDto);
            return Ok(result);
        }

        /// <summary>
        /// Gets a subscription configuration by ID.
        /// </summary>
        /// <param name="id">The configuration ID.</param>
        /// <response code="200">Subscription configuration returned.</response>
        /// <response code="404">Configuration not found.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>The subscription configuration.</returns>
        [HttpGet("Configurations/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SubscriptionConfigurationDto>> GetConfiguration([FromRoute, Required] Guid id)
        {
            var configuration = await _subscriptionManager.GetConfigurationAsync(id).ConfigureAwait(false);
            if (configuration == null)
            {
                return NotFound();
            }

            return Ok(MapToDto(configuration));
        }

        /// <summary>
        /// Creates a new subscription configuration.
        /// </summary>
        /// <param name="request">The configuration creation request.</param>
        /// <response code="201">Configuration created successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>The created configuration.</returns>
        [HttpPost("Configurations")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SubscriptionConfigurationDto>> CreateConfiguration([FromBody, Required] CreateSubscriptionConfigurationRequest request)
        {
            var configuration = new SubscriptionConfiguration
            {
                Name = request.Name,
                Description = request.Description,
                SubscriptionType = request.SubscriptionType,
                CustomDurationHours = request.CustomDurationHours,
                MaxConcurrentSessions = request.MaxConcurrentSessions,
                AllowRemoteAccess = request.AllowRemoteAccess,
                MaxBitrate = request.MaxBitrate,
                AllowTranscoding = request.AllowTranscoding,
                MaxParentalRating = request.MaxParentalRating,
                AllowDownload = request.AllowDownload,
                AllowSyncPlay = request.AllowSyncPlay,
                Price = request.Price,
                Currency = request.Currency,
                SortOrder = request.SortOrder,
                Metadata = request.Metadata
            };

            var userId = User.GetUserId();
            var created = await _subscriptionManager.CreateConfigurationAsync(configuration, userId).ConfigureAwait(false);
            
            return CreatedAtAction(nameof(GetConfiguration), new { id = created.Id }, MapToDto(created));
        }

        /// <summary>
        /// Updates an existing subscription configuration.
        /// </summary>
        /// <param name="id">The configuration ID.</param>
        /// <param name="request">The configuration update request.</param>
        /// <response code="200">Configuration updated successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="404">Configuration not found.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>The updated configuration.</returns>
        [HttpPut("Configurations/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SubscriptionConfigurationDto>> UpdateConfiguration([FromRoute, Required] Guid id, [FromBody, Required] UpdateSubscriptionConfigurationRequest request)
        {
            var existing = await _subscriptionManager.GetConfigurationAsync(id).ConfigureAwait(false);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.SubscriptionType = request.SubscriptionType;
            existing.CustomDurationHours = request.CustomDurationHours;
            existing.MaxConcurrentSessions = request.MaxConcurrentSessions;
            existing.AllowRemoteAccess = request.AllowRemoteAccess;
            existing.MaxBitrate = request.MaxBitrate;
            existing.AllowTranscoding = request.AllowTranscoding;
            existing.MaxParentalRating = request.MaxParentalRating;
            existing.AllowDownload = request.AllowDownload;
            existing.AllowSyncPlay = request.AllowSyncPlay;
            existing.Price = request.Price;
            existing.Currency = request.Currency;
            existing.IsActive = request.IsActive;
            existing.SortOrder = request.SortOrder;
            existing.Metadata = request.Metadata;

            var userId = User.GetUserId();
            var updated = await _subscriptionManager.UpdateConfigurationAsync(existing, userId).ConfigureAwait(false);
            
            return Ok(MapToDto(updated));
        }

        /// <summary>
        /// Deletes a subscription configuration.
        /// </summary>
        /// <param name="id">The configuration ID.</param>
        /// <response code="204">Configuration deleted successfully.</response>
        /// <response code="404">Configuration not found.</response>
        /// <response code="400">Configuration cannot be deleted (in use).</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>No content.</returns>
        [HttpDelete("Configurations/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteConfiguration([FromRoute, Required] Guid id)
        {
            var deleted = await _subscriptionManager.DeleteConfigurationAsync(id).ConfigureAwait(false);
            if (!deleted)
            {
                return BadRequest("Configuration cannot be deleted as it is currently in use by one or more users.");
            }

            return NoContent();
        }

        /// <summary>
        /// Creates a new user with a subscription.
        /// </summary>
        /// <param name="request">The user creation request.</param>
        /// <response code="201">User created successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>The created user and PIN.</returns>
        [HttpPost("Users")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CreateUserWithSubscriptionResponse>> CreateUserWithSubscription([FromBody, Required] CreateUserWithSubscriptionRequest request)
        {
            var (user, pin) = await _subscriptionManager.CreateUserWithSubscriptionAsync(
                request.Username, 
                request.ConfigurationId, 
                request.CustomDurationHours).ConfigureAwait(false);

            var response = new CreateUserWithSubscriptionResponse
            {
                User = _userManager.GetUserDto(user, HttpContext.GetNormalizedRemoteIP().ToString()),
                Pin = pin
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
        }

        /// <summary>
        /// Updates a user's subscription.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="request">The subscription update request.</param>
        /// <response code="200">Subscription updated successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="404">User not found.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>The updated user.</returns>
        [HttpPut("Users/{userId}/Subscription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDto>> UpdateUserSubscription([FromRoute, Required] Guid userId, [FromBody, Required] UpdateUserSubscriptionRequest request)
        {
            var user = await _subscriptionManager.UpdateUserSubscriptionAsync(userId, request.ConfigurationId, request.ExtendExisting).ConfigureAwait(false);
            var userDto = _userManager.GetUserDto(user, HttpContext.GetNormalizedRemoteIP().ToString());
            return Ok(userDto);
        }

        /// <summary>
        /// Extends a user's subscription.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="request">The extension request.</param>
        /// <response code="200">Subscription extended successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="404">User not found.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>The updated user.</returns>
        [HttpPost("Users/{userId}/Extend")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserDto>> ExtendUserSubscription([FromRoute, Required] Guid userId, [FromBody, Required] ExtendSubscriptionRequest request)
        {
            var user = await _subscriptionManager.ExtendUserSubscriptionAsync(userId, request.AdditionalHours).ConfigureAwait(false);
            var userDto = _userManager.GetUserDto(user, HttpContext.GetNormalizedRemoteIP().ToString());
            return Ok(userDto);
        }

        /// <summary>
        /// Gets subscription statistics.
        /// </summary>
        /// <response code="200">Statistics returned.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>Subscription statistics.</returns>
        [HttpGet("Statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SubscriptionStatisticsDto>> GetStatistics()
        {
            var statistics = await _subscriptionManager.GetStatisticsAsync().ConfigureAwait(false);
            var result = new SubscriptionStatisticsDto
            {
                TotalActiveSubscriptions = statistics.TotalActiveSubscriptions,
                TotalExpiredSubscriptions = statistics.TotalExpiredSubscriptions,
                TotalLifetimeSubscriptions = statistics.TotalLifetimeSubscriptions,
                SubscriptionsByType = statistics.SubscriptionsByType,
                TotalRevenue = statistics.TotalRevenue,
                AverageDurationHours = statistics.AverageDurationHours
            };
            return Ok(result);
        }

        /// <summary>
        /// Gets users with expiring subscriptions.
        /// </summary>
        /// <param name="hoursBeforeExpiration">Hours before expiration to check (default: 24).</param>
        /// <response code="200">Users returned.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>Users with expiring subscriptions.</returns>
        [HttpGet("Expiring")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetExpiringUsers([FromQuery] int hoursBeforeExpiration = 24)
        {
            var users = await _subscriptionManager.GetUsersWithExpiringSubscriptionsAsync(hoursBeforeExpiration).ConfigureAwait(false);
            var result = users.Select(u => _userManager.GetUserDto(u, HttpContext.GetNormalizedRemoteIP().ToString()));
            return Ok(result);
        }

        /// <summary>
        /// Gets a user by ID (helper method for CreatedAtAction).
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <returns>The user.</returns>
        [HttpGet("Users/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<UserDto> GetUser([FromRoute] Guid id)
        {
            var user = _userManager.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }

            var userDto = _userManager.GetUserDto(user, HttpContext.GetNormalizedRemoteIP().ToString());
            return Ok(userDto);
        }

        /// <summary>
        /// Maps a subscription configuration to DTO.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The DTO.</returns>
        private static SubscriptionConfigurationDto MapToDto(SubscriptionConfiguration configuration)
        {
            return new SubscriptionConfigurationDto
            {
                Id = configuration.Id,
                Name = configuration.Name,
                Description = configuration.Description,
                SubscriptionType = configuration.SubscriptionType,
                CustomDurationHours = configuration.CustomDurationHours,
                MaxConcurrentSessions = configuration.MaxConcurrentSessions,
                AllowRemoteAccess = configuration.AllowRemoteAccess,
                MaxBitrate = configuration.MaxBitrate,
                AllowTranscoding = configuration.AllowTranscoding,
                MaxParentalRating = configuration.MaxParentalRating,
                AllowDownload = configuration.AllowDownload,
                AllowSyncPlay = configuration.AllowSyncPlay,
                Price = configuration.Price,
                Currency = configuration.Currency,
                IsActive = configuration.IsActive,
                CreatedDate = configuration.CreatedDate,
                ModifiedDate = configuration.ModifiedDate,
                SortOrder = configuration.SortOrder,
                Metadata = configuration.Metadata
            };
        }
    }
}
