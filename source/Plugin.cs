using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

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
        internal static MethodInfo SetWheelFriction = AccessTools.Method(typeof(VehicleController), "SetWheelFriction");

        internal static ConfigEntry<bool> AutomaticGears;
        internal static ConfigEntry<bool> AutomaticPark;
        internal static void InitConfig()
        {
            PluginLoader.Instance.BindConfig(ref AutomaticGears, "Settings", "Automatic Gearbox", true, "Should the gears automatically switch between drive & reverse based on key press?");
            PluginLoader.Instance.BindConfig(ref AutomaticPark, "Settings", "Automatic Park", true, "Should the gear automatically switch to parked when the key is taken out?");
        }
    }
}