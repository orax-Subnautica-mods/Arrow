using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using Nautilus.Assets;
using Nautilus.Utility;
using Nautilus.Handlers;
using UnityEngine;

using System.Reflection;
using System.IO;
using System.Collections.Generic;

namespace Arrow;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.snmodding.nautilus")]
public class Plugin : BaseUnityPlugin
{
    public new static ManualLogSource Logger { get; private set; }
    public static Plugin Instance { get; private set; }

    public static Assembly ModAssembly { get; } = Assembly.GetExecutingAssembly();
    public static string ModPath { get; } = Path.GetDirectoryName(ModAssembly.Location);
    public static string AssetsFolder { get; } = Path.Combine(ModPath, "Assets");

    private static string AssetBundleFileName { get; } = "arrow";

    public static AssetBundle AssetBundle { get; private set; }
    public static ArrowModOptions ModOptions { get; private set; }
    public static Dictionary<string, Arrow> ArrowsList { get; set; } = new Dictionary<string, Arrow>();

    public PrefabInfo MyPrefabInfo { get; set; }

    public Plugin()
    {
        Instance = this;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
    private void Awake()
    {
        // set project-scoped logger instance
        Logger = base.Logger;

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        // Keep in mind that the assetbundle can only be open in one place at a time, so keep a reference.
        // This method assumes you have a folder named "Assets" in your mod's plugin folder.
        // The second parameter needs to be the name of the asset bundle file (usually they don't have file extensions).
        AssetBundle = AssetBundleLoadingUtils.LoadFromAssetsFolder(ModAssembly, AssetBundleFileName);

        ModOptions = new ArrowModOptions();
        OptionsPanelHandler.RegisterModOptions(ModOptions);

        Arrow.LoadAssets();

        // create arrows
        // They will be constructable with the builder.
        for (int i = 1; i <= ModOptions.NumberOfArrows; i++)
        {
            new Arrow(i.ToString());
        }
    }

    public ConfigEntry<T> ConfigBind<T>(string section, string key, T defaultValue, string description)
    {
        return Config.Bind(new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description, null));
    }
}
