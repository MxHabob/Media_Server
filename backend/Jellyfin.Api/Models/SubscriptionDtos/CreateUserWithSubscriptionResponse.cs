using MediaBrowser.Model.Dto;

namespace Jellyfin.Api.Models.SubscriptionDtos
{
    /// <summary>
    /// Response after creating a user with a subscription.
    /// </summary>
    public class CreateUserWithSubscriptionResponse
    {
        /// <summary>
        /// Gets or sets the created user.
        /// </summary>
        public UserDto User { get; set; } = null!;

        /// <summary>
        /// Gets or sets the generated PIN for the user.
        /// </summary>
        public string Pin { get; set; } = string.Empty;
    }
}
