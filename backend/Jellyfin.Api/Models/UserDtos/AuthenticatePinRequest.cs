using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.UserDtos
{
    /// <summary>
    /// Request model for PIN authentication.
    /// </summary>
    public class AuthenticatePinRequest
    {
        /// <summary>
        /// Gets or sets the PIN to authenticate with.
        /// </summary>
        [Required]
        public string Pin { get; set; } = string.Empty;
    }
}


