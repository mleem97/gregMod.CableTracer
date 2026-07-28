using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using Event = UnityEngine.Event;
using EventType = UnityEngine.EventType;

namespace CableTracer
{
    public class TracerManager : MonoBehaviour
    {
        private const float RaycastDistance = 6f;
        // Maximum distance (metres) to the nearest cable endpoint to count as a hit
        private const float MaxEndpointSnap  = 1.5f;

        private int        _highlightedCableId = -1;
        private GameObject _highlightGO;
        private LineRenderer _lr;
        // pending highlight data stored in fields to avoid Vector3[] method parameters
        // (Il2CppInterop cannot marshal managed arrays as injected method params)
        private int        _pendingId;
        private Vector3[]  _pendingPoints;
        // pulsate state
        private float   _pulseT;
        private Color   _baseColor;
        private Color[] _pulsateColors;

        public void Awake()
        {
            var cfg = CableTracerConfig.Current;
            MelonLogger.Msg($"[CableTracer] TracerManager ready. {cfg.ClickModifier}+{cfg.ClickButton}-click to trace a cable.");
        }

        public void OnGUI()
        {
            // Use Event.current so we work with both legacy and new Input System
            Event e = Event.current;
            if (e == null) return;
            if (e.type == EventType.MouseDown && ButtonMatches(e) && ModifierPressed(e))
            {
                TryTrace();
                e.Use();
            }
        }

        private void TryTrace()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Aim from screen centre (FPS crosshair), same approach as PortLabels
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Vector3 hitPos = Physics.Raycast(ray, out RaycastHit hit, RaycastDistance)
                ? hit.point
                : ray.GetPoint(RaycastDistance);

            var wis = WaypointInitializationSystem.Instance;
            if (wis == null)
            {
                MelonLogger.Warning("[CableTracer] WaypointInitializationSystem not ready.");
                return;
            }

            var allCables = wis.GetAllCables();
            if (allCables == null || allCables.Count == 0)
            {
                MelonLogger.Msg("[CableTracer] No cables found.");
                return;
            }

            // Find the cable whose nearest endpoint is closest to the aim point
            int       bestId     = -1;
            float     minDist    = float.MaxValue;
            Vector3[] bestPoints = null;

            for (int c = 0; c < allCables.Count; c++)
            {
                var cable = allCables[c];
                var waypoints = cable.Waypoints;
                if (waypoints == null || waypoints.Count == 0) continue;

                float d1 = Vector3.Distance(hitPos, waypoints[0]);
                float d2 = Vector3.Distance(hitPos, waypoints[waypoints.Count - 1]);
                float d  = Mathf.Min(d1, d2);
                if (d < minDist)
                {
                    minDist = d;
                    bestId  = cable.CableID;
                    // Copy points into managed memory immediately — the Il2Cpp object
                    // can be GC'd before HighlightCable runs if we hold the reference.
                    var pts = new Vector3[waypoints.Count];
                    for (int i = 0; i < waypoints.Count; i++) pts[i] = waypoints[i];
                    bestPoints = pts;
                }
            }

            if (bestId < 0 || minDist > MaxEndpointSnap)
            {
                if (_highlightedCableId >= 0)
                {
                    ClearHighlight();
                    MelonLogger.Msg("[CableTracer] Highlight cleared.");
                }
                else
                {
                    MelonLogger.Msg($"[CableTracer] No cable endpoint within {MaxEndpointSnap}m " +
                                   $"(closest was {minDist:F2}m).");
                }
                return;
            }

            // Toggle: same cable → clear; different cable → switch highlight
            if (_highlightedCableId == bestId)
            {
                ClearHighlight();
                MelonLogger.Msg($"[CableTracer] Cleared highlight for cable {bestId}.");
                return;
            }

            _pendingId     = bestId;
            _pendingPoints = bestPoints;
            HighlightCable();
        }

        private void HighlightCable()
        {
            int       cableId = _pendingId;
            Vector3[] pts     = _pendingPoints;

            ClearHighlight();

            if (pts == null || pts.Length < 2)
            {
                MelonLogger.Warning($"[CableTracer] Cable {cableId} has <2 waypoints.");
                return;
            }

            _highlightGO = new GameObject("CableTracer_Highlight");
            UnityEngine.Object.DontDestroyOnLoad(_highlightGO);

            float lineWidth = CableTracerConfig.Current.LineThickness;
            string colorHex = CableTracerConfig.Current.HighlightColor ?? "#FFD900";
            if (!colorHex.StartsWith("#")) colorHex = "#" + colorHex;
            if (!ColorUtility.TryParseHtmlString(colorHex, out Color col))
                col = new Color(1f, 0.85f, 0f, 1f); // fallback yellow

            _lr = _highlightGO.AddComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.startWidth    = lineWidth;
            _lr.endWidth      = lineWidth;

            // Unlit — works with all render pipelines
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color      = col;
            _lr.material   = mat;
            _lr.startColor = col;
            _lr.endColor   = col;

            _lr.positionCount = pts.Length;
            for (int i = 0; i < pts.Length; i++)
                _lr.SetPosition(i, pts[i]);

            // Cache pulsate state
            _pulseT        = 0f;
            _baseColor     = col;
            _pulsateColors = null;
            BuildPulsateColors();

            _highlightedCableId = cableId;
            MelonLogger.Msg($"[CableTracer] Highlighted cable {cableId} ({pts.Length} waypoints).");
        }

        public void Update()
        {
            if (_highlightGO == null || _lr == null) return;
            if (!(CableTracerConfig.Current.Pulsate)) return;

            float speed = CableTracerConfig.Current.PulsateSpeed;
            _pulseT += Time.deltaTime * speed;

            Color c;
            if (_pulsateColors != null && _pulsateColors.Length >= 2)
            {
                // Cycle smoothly through the list of colors
                float pos  = _pulseT % _pulsateColors.Length;
                int   idx  = (int)pos % _pulsateColors.Length;
                int   next = (idx + 1) % _pulsateColors.Length;
                c = Color.Lerp(_pulsateColors[idx], _pulsateColors[next], pos - idx);
            }
            else
            {
                // Pulse brightness of the base highlight color
                float bright = Mathf.Lerp(0.25f, 1f, Mathf.Sin(_pulseT * Mathf.PI * 2f) * 0.5f + 0.5f);
                Color.RGBToHSV(_baseColor, out float h, out float s, out float v);
                c = Color.HSVToRGB(h, s, v * bright);
                c.a = _baseColor.a;
            }

            _lr.startColor      = c;
            _lr.endColor        = c;
            _lr.material.color  = c;
        }

        private void BuildPulsateColors()
        {
            string raw = CableTracerConfig.Current.PulsateColors ?? "";
            if (string.IsNullOrWhiteSpace(raw)) return;
            var parts  = raw.Split(',');
            var result = new System.Collections.Generic.List<Color>();
            foreach (var part in parts)
            {
                string hex = part.Trim();
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (ColorUtility.TryParseHtmlString(hex, out Color parsed))
                    result.Add(parsed);
            }
            if (result.Count >= 2) _pulsateColors = result.ToArray();
        }

        private static bool ModifierPressed(Event e)
        {
            switch ((CableTracerConfig.Current.ClickModifier ?? "Shift").ToLowerInvariant())
            {
                case "ctrl":  return e.control;
                case "alt":   return e.alt;
                case "none":  return true;
                default:      return e.shift; // "shift"
            }
        }

        private static bool ButtonMatches(Event e)
        {
            switch ((CableTracerConfig.Current.ClickButton ?? "Left").ToLowerInvariant())
            {
                case "right":  return e.button == 1;
                case "middle": return e.button == 2;
                default:       return e.button == 0; // "left"
            }
        }

        private void ClearHighlight()
        {
            if (_highlightGO != null)
            {
                UnityEngine.Object.Destroy(_highlightGO);
                _highlightGO = null;
            }
            _lr             = null;
            _pulsateColors  = null;
            _highlightedCableId = -1;
        }

        public TracerManager(IntPtr ptr) : base(ptr) { }

        public void OnDestroy() => ClearHighlight();
    }
}
