using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace CableTracer
{
    public class CableTracerConfig
    {
        private static readonly string ConfigPath =
            Path.Combine(MelonEnvironment.UserDataDirectory, "CableTracer.json");

        // Singleton loaded at startup
        public static CableTracerConfig Current { get; private set; } = new CableTracerConfig();

        // ── Appearance ──────────────────────────────────────────────────────────
        [JsonProperty("HighlightColor")]
        public string HighlightColor { get; set; } = "#FFD900";

        [JsonProperty("LineThickness")]
        public float LineThickness { get; set; } = 0.05f;

        // ── Input ────────────────────────────────────────────────────────────────
        [JsonProperty("ClickModifier")]
        public string ClickModifier { get; set; } = "Shift";

        [JsonProperty("ClickButton")]
        public string ClickButton { get; set; } = "Left";

        // ── Pulsate ──────────────────────────────────────────────────────────────
        [JsonProperty("Pulsate")]
        public bool Pulsate { get; set; } = false;

        [JsonProperty("PulsateSpeed")]
        public float PulsateSpeed { get; set; } = 1.0f;

        [JsonProperty("PulsateColors")]
        public string PulsateColors { get; set; } = "";

        // ── Load / Save ──────────────────────────────────────────────────────────
        public static void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                Current = new CableTracerConfig();
                Save();
                return;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                Current = JsonConvert.DeserializeObject<CableTracerConfig>(json)
                          ?? new CableTracerConfig();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[CableTracer] Failed to read config, using defaults: {ex.Message}");
                Current = new CableTracerConfig();
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(Current, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[CableTracer] Failed to save config: {ex.Message}");
            }
        }
    }
}
