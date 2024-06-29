using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BetterVehicleControls
{
    [BepInPlugin(modGUID, "BetterVehicleControls", modVersion)]
    internal class PluginLoader : BaseUnityPlugin
    {
        internal const string modGUID = "Dev1A3.BetterVehicleControls";
        internal const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static bool initialized;

        public static PluginLoader Instance { get; private set; }

        internal static ManualLogSource logSource;

        private void Awake()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            Instance = this;
            logSource = Logger;

            FixesConfig.InitConfig();

            Assembly patches = Assembly.GetExecutingAssembly();
            harmony.PatchAll(patches);
        }

        public void BindConfig<T>(ref ConfigEntry<T> config, string section, string key, T defaultValue, string description = "")
        {
            config = Config.Bind<T>(section, key, defaultValue, description);
        }
    }
    internal class FixesConfig
    {
        internal static ConfigEntry<bool> AutoSwitchDriveReverse;
        internal static ConfigEntry<bool> AutoSwitchFromParked;
        internal static ConfigEntry<bool> AutoSwitchToParked;
        internal static ConfigEntry<bool> RecenterWheel;
        internal static void InitConfig()
        {
            PluginLoader.Instance.BindConfig(ref AutoSwitchDriveReverse, "Settings", "Automatic Gearbox", true, "Should the gear automatically switch between drive & reverse when pressing the forward/backwards buttons?");
            PluginLoader.Instance.BindConfig(ref AutoSwitchFromParked, "Settings", "Automatic Handbrake Release", false, "Should the gear automatically switch to drive/reverse from parked?");
            PluginLoader.Instance.BindConfig(ref AutoSwitchToParked, "Settings", "Automatic Handbrake Pull", false, "Should the gear automatically switch to parked when the key is taken from the ignition?");
            PluginLoader.Instance.BindConfig(ref RecenterWheel, "Settings", "Automatically Center Wheel", false, "Should the wheel be automatically re-centered?");
        }
    }
}