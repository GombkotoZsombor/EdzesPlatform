using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdzesPlatform.Models;
using Supabase;
using System.Linq; // Fontos a First/FirstOrDefault miatt

namespace EdzesPlatform.Services
{
    public class DataService
    {
        private readonly Client _client;

        public DataService(Client client)
        {
            _client = client;
        }

        // --- PROFIL KEZELÉS ---

        // Összes profil lekérése
        public async Task<List<AthleteProfile>> GetProfilesAsync()
        {
            var response = await _client.From<AthleteProfile>().Get();
            return response.Models;
        }

        // Új profil létrehozása
        public async Task CreateProfileAsync(string name, DateTime? birthDate)
        {
            var newProfile = new AthleteProfile
            {
                Name = name,
                BirthDate = birthDate,
                // UserId-t most üresen hagyjuk a teszteléshez, élesben az auth.uid kell ide
            };

            await _client.From<AthleteProfile>().Insert(newProfile);
        }

        // Profil törlése
        public async Task DeleteProfileAsync(string id)
        {
            await _client.From<AthleteProfile>().Where(x => x.Id == id).Delete();
        }

        // --- ZÓNA TÖRTÉNET KEZELÉS ---

        // Egy profilhoz tartozó összes mérés lekérése
        public async Task<List<ZoneHistory>> GetZoneHistoryForProfileAsync(string profileId)
        {
            var response = await _client.From<ZoneHistory>()
                .Where(x => x.ProfileId == profileId)
                .Order("valid_from", Supabase.Postgrest.Constants.Ordering.Descending) // Legfrissebb elől
                .Get();
            return response.Models;
        }

        // Új mérés rögzítése
        public async Task AddZoneSnapshotAsync(ZoneHistory history)
        {
            await _client.From<ZoneHistory>().Insert(history);
        }

        // --- EDZÉS NAPTÁR KEZELÉS ---

        // Adott időszakra eső edzések lekérése (pl. egy hónap)
        public async Task<List<Workout>> GetWorkoutsAsync(string profileId, DateTime start, DateTime end)
        {
            // A Supabase dátum formátuma: yyyy-MM-dd
            string startStr = start.ToString("yyyy-MM-dd");
            string endStr = end.ToString("yyyy-MM-dd");

            var response = await _client.From<Workout>()
                .Where(x => x.ProfileId == profileId)
                .Filter("date", Supabase.Postgrest.Constants.Operator.GreaterThanOrEqual, startStr)
                .Filter("date", Supabase.Postgrest.Constants.Operator.LessThanOrEqual, endStr)
                .Order("date", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }

        // Üres edzés létrehozása egy adott napra - JAVÍTVA IDŐZÓNÁRA
        public async Task<Workout> CreateEmptyWorkoutAsync(string profileId, DateTime date)
        {
            var newWorkout = new Workout
            {
                ProfileId = profileId,
                // JAVÍTÁS: Hozzáadunk 12 órát. Így ha az időzóna el is tolja +/- pár órát,
                // akkor is ugyanazon a napon marad a dátum.
                Date = date.Date.AddHours(12),
                Title = "Új edzés",
                Status = "planned",
                Steps = new List<WorkoutStep>()
            };

            var response = await _client.From<Workout>().Insert(newWorkout);
            return response.Models.First();
        }

        // Edzés törlése
        public async Task DeleteWorkoutAsync(string workoutId)
        {
            await _client.From<Workout>().Where(x => x.Id == workoutId).Delete();
        }

        // --- EDZÉS SZERKESZTÉS ---

        // Egy konkrét edzés betöltése ID alapján
        public async Task<Workout?> GetWorkoutByIdAsync(string workoutId)
        {
            var response = await _client.From<Workout>()
                .Where(x => x.Id == workoutId)
                .Get();
            return response.Models.FirstOrDefault();
        }

        // Meglévő edzés frissítése (Mentés)
        public async Task UpdateWorkoutAsync(Workout workout)
        {
            workout.Date = workout.Date.Date.AddHours(12);
            await _client.From<Workout>().Update(workout);
        }
        // --- SABLON KEZELÉS (TEMPLATE LIBRARY) ---

        // Sablonok lekérése profilhoz
       

        // Jelenlegi edzés mentése sablonként
       

        // Sablon betöltése egy meglévő edzésbe (felülírja a lépéseket!)
        public async Task LoadTemplateIntoWorkoutAsync(string workoutId, WorkoutTemplate template)
        {
            // 1. Lekérjük az edzést
            var workout = await GetWorkoutByIdAsync(workoutId);
            if (workout == null) return;

            // 2. Felülírjuk az adatait a sablonnal
            workout.Title = template.Title; // Opcionális: a nevét is átírjuk
            workout.Description = template.Description;
            workout.Steps = new List<WorkoutStep>(template.Steps); // Fontos: Új lista példány!

            // 3. Mentés
            await UpdateWorkoutAsync(workout);
        }
        // ... a fájl többi része változatlan ...

        // --- SABLON KEZELÉS (TEMPLATE LIBRARY) ---

        // Sablonok lekérése profilhoz (EZ MÁR MEGVAN, CSAK HAGYD BENT)
        public async Task<List<WorkoutTemplate>> GetTemplatesAsync(string profileId)
        {
            var response = await _client.From<WorkoutTemplate>()
                .Where(x => x.ProfileId == profileId)
                .Order("title", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();
            return response.Models;
        }

        // Jelenlegi edzés mentése sablonként (EZ IS MEGVAN)
        public async Task SaveAsTemplateAsync(Workout workout)
        {
            var template = new WorkoutTemplate
            {
                ProfileId = workout.ProfileId,
                Title = workout.Title,
                Description = workout.Description,
                Steps = workout.Steps
            };
            await _client.From<WorkoutTemplate>().Insert(template);
        }

        // --- EZT AZ ÚJ FÜGGVÉNYT ADD HOZZÁ: ---
        // Sablon törlése
        public async Task DeleteTemplateAsync(string templateId)
        {
            await _client.From<WorkoutTemplate>().Where(x => x.Id == templateId).Delete();
        }
        // --------------------------------------

       
    }
}