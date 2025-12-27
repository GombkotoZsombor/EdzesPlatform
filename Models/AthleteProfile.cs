using System;
using Supabase.Postgrest.Attributes; // Ennek most már működnie kell az új csomaggal
using Supabase.Postgrest.Models;


namespace EdzesPlatform.Models
{
    [Table("profiles")]
    public class AthleteProfile : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("user_id")]
        public string UserId { get; set; } = default!;

        [Column("name")]
        public string Name { get; set; } = default!;

        [Column("birth_date")]
        public DateTime? BirthDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}