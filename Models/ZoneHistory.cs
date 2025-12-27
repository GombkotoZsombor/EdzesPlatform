using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EdzesPlatform.Models
{
    [Table("zone_history")]
    public class ZoneHistory : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("profile_id")]
        public string ProfileId { get; set; } = default!;

        [Column("valid_from")]
        public DateTime ValidFrom { get; set; }

        [Column("zones_data")]
        public ZoneSnapshotData ZonesData { get; set; } = new();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}