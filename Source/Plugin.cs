
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using HarmonyModBuggos;
using ArchipelagoBuggos;

namespace HarmonyModPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        InternalLogger.SetLogger(Log);
        InternalLogger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        ApConfig.Init(Config);

        
        SaveSwapper.Run();
        CheckSender.Init();

        ApClient.Create();

        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
    }
}
