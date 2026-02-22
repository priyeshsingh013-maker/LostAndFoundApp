using System.ComponentModel.DataAnnotations;

namespace LostAndFoundApp.Models
{
    /// <summary>
    /// Stores Active Directory group names for user synchronization.
    /// </summary>
    public class AdGroup
    {
        public int Id { get; set; }

        [Required, StringLength(256)]
        [Display(Name = "AD Group Name")]
        public string GroupName { get; set; } = string.Empty;

        [Display(Name = "Date Added")]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}
