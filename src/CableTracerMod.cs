using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ModCoverage.Tests")]

[assembly: MelonInfo(typeof(CableTracer.CableTracerMod), CableTracer.MyPluginInfo.PLUGIN_NAME, CableTracer.MyPluginInfo.PLUGIN_VERSION, "tindolt")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace CableTracer
{
    public class CableTracerMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<TracerManager>();

            CableTracerConfig.Load();
            var cfg = CableTracerConfig.Current;

            LoggerInstance.Msg($"Cable Tracer {MyPluginInfo.PLUGIN_VERSION} loaded. " +
                $"{cfg.ClickModifier}+{cfg.ClickButton}-click a cable end to highlight its path.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (Object.FindObjectOfType<TracerManager>() == null)
            {
                var go = new GameObject("CableTracerManager");
                go.AddComponent<TracerManager>();
                Object.DontDestroyOnLoad(go);
            }
        }
    }
}
