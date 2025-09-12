using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.UserDtos
{
    public class AuthenticatePinRequest
    {
        [Required]
        public string Pin { get; set; } = string.Empty;
    }
}


