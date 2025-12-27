using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EdzesPlatform.Models
{
    [Table("workout_templates")]
    public class WorkoutTemplate : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("profile_id")]
        public string ProfileId { get; set; } = default!;

        [Column("title")]
        public string Title { get; set; } = default!;

        [Column("description")]
        public string? Description { get; set; }

        // Ugyanazt a WorkoutStep listát használjuk, mint a sima edzésnél! [cite: 230]
        [Column("steps_data")]
        public List<WorkoutStep> Steps { get; set; } = new();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}