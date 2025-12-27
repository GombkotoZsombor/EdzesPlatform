using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EdzesPlatform.Models
{
    public class WorkoutStep
    {
        public string Type { get; set; } = "SteadyState";
        public int DurationSeconds { get; set; }
        public string Zone { get; set; } = "GA1";
        public int? OverridePower { get; set; }
        public int? Cadence { get; set; }
        public string Instruction { get; set; } = "";
        public int RepeatCount { get; set; } = 1;
        public int OffDurationSeconds { get; set; }
        public string OffZone { get; set; } = "KB";
        public int? OffCadence { get; set; }
    }

    [Table("workouts")]
    public class Workout : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = default!;

        [Column("profile_id")]
        public string ProfileId { get; set; } = default!;

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("title")]
        public string Title { get; set; } = default!;

        [Column("description")]
        public string? Description { get; set; }

        [Column("status")]
        public string Status { get; set; } = "planned";

        [Column("rpe")]
        public int? Rpe { get; set; }

        [Column("feeling")]
        public string? Feeling { get; set; }

        [Column("steps_data")]
        public List<WorkoutStep> Steps { get; set; } = new();

        [Column("coach_notes")]
        public string? CoachNotes { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}