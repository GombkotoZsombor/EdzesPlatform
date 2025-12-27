using System.Text;
using EdzesPlatform.Models;
using System.Globalization;

namespace EdzesPlatform.Services
{
    public class WorkoutExportService
    {
        // ---------------------------------------------------------
        // 1. INTERVALS.ICU / SZÖVEGES FORMÁTUM GENERÁLÁSA
        // ---------------------------------------------------------
        public string GenerateIntervalsIcuText(Workout workout, ZoneSnapshotData zones, bool useHeartRate)
        {
            var sb = new StringBuilder();

            // Fejléc
            sb.AppendLine("Category: Imported");
            sb.AppendLine();

            foreach (var step in workout.Steps)
            {
                // Cél érték (Watt vagy Pulzus)
                string targetUnit = useHeartRate ? "bpm" : "W";
                int targetValue = useHeartRate
                    ? GetTargetHr(step.Zone, zones)
                    : GetTargetWatts(step.Zone, step.OverridePower, zones);

                // Fordulat (csak ha van és reális)
                string cadenceText = (step.Cadence.HasValue && step.Cadence.Value > 40 && step.Cadence.Value < 200)
                    ? $" {step.Cadence.Value}rpm"
                    : "";

                // Instrukció
                string instruction = string.IsNullOrWhiteSpace(step.Instruction) ? "" : $" {step.Instruction}";

                if (step.Type == "SteadyState")
                {
                    // Pl: - 10m 200W 90rpm Szöveg
                    sb.AppendLine($"- {FormatTime(step.DurationSeconds)} {targetValue}{targetUnit}{cadenceText}{instruction}");
                    string hrText = "";
                    int targetHr = GetTargetHr(step.Zone, zones); // Ezt a függvényt (lásd lent) használni kell itt is
                    if (targetHr > 0) hrText = $" [HR: {targetHr} bpm]";

                    // Az üzenetbe belefűzzük a HR-t
                    string message = $"{step.Instruction}{hrText}".Trim();

                    if (!string.IsNullOrWhiteSpace(message))
                        sb.AppendLine($"            <textEvent timeoffset=\"0\" message=\"{EscapeXml(message)}\"/>");
                }
                else if (step.Type == "Intervals")
                {
                    // Pihenő adatok
                    int offValue = useHeartRate
                        ? GetTargetHr(step.OffZone, zones)
                        : GetTargetWatts(step.OffZone, null, zones);

                    string offCadenceText = (step.OffCadence.HasValue && step.OffCadence.Value > 40 && step.OffCadence.Value < 200)
                        ? $" {step.OffCadence.Value}rpm"
                        : "";

                    // Pl: 5x 3m 300W 2m 140W
                    sb.AppendLine($"{step.RepeatCount}x {FormatTime(step.DurationSeconds)} {targetValue}{targetUnit}{cadenceText} {FormatTime(step.OffDurationSeconds)} {offValue}{targetUnit}{offCadenceText}{instruction}");
                }
            }

            return sb.ToString();
        }

        // ---------------------------------------------------------
        // 2. WAHOO / ZWIFT (.ZWO) XML GENERÁLÁSA
        // Ez a "fit kompatibilis" fájl, amit a Wahoo elfogad.
        // ---------------------------------------------------------
        public string GenerateZwoXml(Workout workout, ZoneSnapshotData zones, int referenceFtp)
        {
            // Biztonsági ellenőrzés az FTP-re
            if (referenceFtp <= 0) referenceFtp = 250;

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<workout_file>");
            sb.AppendLine("    <author>EdzesPlatform</author>");
            sb.AppendLine($"    <name>{EscapeXml(workout.Title)}</name>");
            sb.AppendLine($"    <description>{EscapeXml(workout.Description ?? "Edzés")}</description>");
            sb.AppendLine("    <sportType>bike</sportType>");
            sb.AppendLine("    <tags><tag name=\"intervals\"/></tags>");
            sb.AppendLine("    <category>Imported</category>");
            sb.AppendLine("    <workout>");

            foreach (var step in workout.Steps)
            {
                if (step.Type == "SteadyState")
                {
                    // Kiszámoljuk az abszolút wattot, majd átváltjuk FTP %-ra (0.00 formátum)
                    int targetWatts = GetTargetWatts(step.Zone, step.OverridePower, zones);
                    double powerRatio = (double)targetWatts / referenceFtp;

                    sb.AppendLine($"        <SteadyState Duration=\"{step.DurationSeconds}\" Power=\"{FormatDouble(powerRatio)}\">");

                    if (step.Cadence > 0)
                        sb.AppendLine($"            <Cadence>{step.Cadence}</Cadence>");

                    if (!string.IsNullOrWhiteSpace(step.Instruction))
                        sb.AppendLine($"            <textEvent timeoffset=\"0\" message=\"{EscapeXml(step.Instruction)}\"/>");

                    sb.AppendLine("        </SteadyState>");
                }
                else if (step.Type == "Intervals")
                {
                    int onWatts = GetTargetWatts(step.Zone, step.OverridePower, zones);
                    int offWatts = GetTargetWatts(step.OffZone, null, zones);

                    double onRatio = (double)onWatts / referenceFtp;
                    double offRatio = (double)offWatts / referenceFtp;

                    sb.AppendLine($"        <IntervalsT Repeat=\"{step.RepeatCount}\" OnDuration=\"{step.DurationSeconds}\" OffDuration=\"{step.OffDurationSeconds}\" OnPower=\"{FormatDouble(onRatio)}\" OffPower=\"{FormatDouble(offRatio)}\">");

                    if (step.Cadence > 0) sb.AppendLine($"            <Cadence>{step.Cadence}</Cadence>");
                    if (step.OffCadence > 0) sb.AppendLine($"            <CadenceResting>{step.OffCadence}</CadenceResting>");

                    if (!string.IsNullOrWhiteSpace(step.Instruction))
                        sb.AppendLine($"            <textEvent timeoffset=\"0\" message=\"{EscapeXml(step.Instruction)}\"/>");

                    sb.AppendLine("        </IntervalsT>");
                }
            }

            sb.AppendLine("    </workout>");
            sb.AppendLine("</workout_file>");

            return sb.ToString();
        }

        // ---------------------------------------------------------
        // SEGÉDFÜGGVÉNYEK
        // ---------------------------------------------------------

        private int GetTargetWatts(string zoneName, int? overrideWatts, ZoneSnapshotData zones)
        {
            if (overrideWatts.HasValue && overrideWatts.Value > 0) return overrideWatts.Value;

            var limits = GetLimits(zoneName, zones);
            if (limits.PowerMin == 0 && limits.PowerMax == 0) return 100;

            // Ha a felhasználó "végtelen" (pl. 9999) maxot adott meg Sprinthez,
            // akkor a zóna ALJÁT vesszük, nem az átlagot.
            if (limits.PowerMax > 2000)
            {
                return limits.PowerMin;
            }

            return (limits.PowerMin + limits.PowerMax) / 2;
        }

        private int GetTargetHr(string zoneName, ZoneSnapshotData zones)
        {
            var limits = GetLimits(zoneName, zones);
            if (limits.HrMin == 0 && limits.HrMax == 0) return 0;

            // Pulzusnál is szűrjük az irreális maxot
            if (limits.HrMax > 250)
            {
                return limits.HrMin;
            }

            return (limits.HrMin + limits.HrMax) / 2;
        }

        private ZoneLimits GetLimits(string zoneName, ZoneSnapshotData zones)
        {
            return zoneName switch
            {
                "KB" => zones.KB,
                "GA1" => zones.GA1,
                "GA2" => zones.GA2,
                "EB" => zones.EB,
                "VO2" => zones.VO2,
                "SB" => zones.SB,
                "K3" => zones.K3,
                _ => new ZoneLimits()
            };
        }

        private string FormatTime(int seconds)
        {
            // Intervals formátumhoz (10m, 90s)
            if (seconds % 60 == 0) return $"{seconds / 60}m";
            return $"{seconds}s";
        }

        private string FormatDouble(double value)
        {
            // XML-hez tizedesponttal (pl. 0.75)
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
        // WorkoutExportService.cs bővítése
        public byte[] GenerateCsvExport(List<Workout> workouts)
        {
            var sb = new StringBuilder();

            // 1. Fejléc (BOM az Excel miatt fontos az elején)
            sb.AppendLine("Dátum;Edzés neve;Zónák/Blokkok;Összidő (perc);RPE;Érzés;Megjegyzés");

            foreach (var w in workouts)
            {
                // Adatok előkészítése, pontosvesszők cseréje, hogy ne törje szét a CSV-t
                string date = w.Date.ToString("yyyy.MM.dd");
                string title = CleanForCsv(w.Title);
                string desc = CleanForCsv(w.Description ?? "");
                string feeling = w.Feeling ?? "-";
                string rpe = w.Rpe.HasValue ? w.Rpe.ToString() : "-";

                // Összidő számítása a lépésekből
                int totalSeconds = w.Steps.Sum(s => s.Type == "Intervals"
                    ? (s.DurationSeconds + s.OffDurationSeconds) * s.RepeatCount
                    : s.DurationSeconds);
                string durationMin = (totalSeconds / 60).ToString();

                // Blokkok rövid összefoglalása (pl. "10p GA1, 4x 3p EB")
                string summary = GenerateShortSummary(w.Steps);

                sb.AppendLine($"{date};{title};{summary};{durationMin};{rpe};{feeling};{desc}");
            }

            // UTF-8 BOM hozzáadása, hogy az Excel felismerje az ékezeteket
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        // Segédfüggvények
        private string CleanForCsv(string input)
        {
            // Sortörések és pontosvesszők cseréje
            return input.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
        }

        private string GenerateShortSummary(List<WorkoutStep> steps)
        {
            var parts = new List<string>();
            foreach (var s in steps)
            {
                string dur = s.DurationSeconds >= 60 ? $"{s.DurationSeconds / 60}p" : $"{s.DurationSeconds}mp";
                if (s.Type == "Intervals")
                {
                    parts.Add($"{s.RepeatCount}x {dur} {s.Zone}");
                }
                else
                {
                    parts.Add($"{dur} {s.Zone}");
                }
            }
            return CleanForCsv(string.Join(", ", parts));
        }
    }
}