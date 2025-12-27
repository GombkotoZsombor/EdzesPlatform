namespace EdzesPlatform.Models
{
    public class ZoneLimits
    {
        public int PowerMin { get; set; }
        public int PowerMax { get; set; }
        public int HrMin { get; set; }
        public int HrMax { get; set; }
    }

    public class ZoneSnapshotData
    {
        public ZoneLimits KB { get; set; } = new();
        public ZoneLimits GA1 { get; set; } = new();
        public ZoneLimits GA2 { get; set; } = new();
        public ZoneLimits EB { get; set; } = new();
        public ZoneLimits VO2 { get; set; } = new();
        public ZoneLimits SB { get; set; } = new();
        public ZoneLimits K3 { get; set; } = new();
    }
}