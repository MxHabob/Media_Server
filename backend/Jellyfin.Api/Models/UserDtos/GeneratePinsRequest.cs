using System.ComponentModel.DataAnnotations;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Api.Models.UserDtos
{
    public class GeneratePinsRequest
    {
        [Required]
        public int Count { get; set; }

        [Required]
        public SubscriptionType SubscriptionType { get; set; }
    }
}


