using System.ComponentModel.DataAnnotations;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Api.Models.UserDtos
{
    /// <summary>
    /// Request model for generating PINs.
    /// </summary>
    public class GeneratePinsRequest
    {
        /// <summary>
        /// Gets or sets the number of PINs to generate.
        /// </summary>
        [Required]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the subscription type for the generated PINs.
        /// </summary>
        [Required]
        public SubscriptionType SubscriptionType { get; set; }
    }
}


