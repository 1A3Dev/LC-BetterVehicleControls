using GameNetcodeStuff;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

namespace BetterVehicleControls.Patches
{
    [HarmonyPatch]
    internal static class Patches_PlayerController
    {
        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
        [HarmonyPrefix]
        public static bool ScrollMouse(PlayerControllerB __instance, ref CallbackContext context, bool ___throwingObject)
        {
            if (!FixesConfig.ScrollableGears.Value) return true;
            if (__instance.inTerminalMenu) return true;
            if ((!__instance.IsOwner || !__instance.isPlayerControlled || (__instance.IsServer && !__instance.isHostPlayerObject)) && !__instance.isTestingPlayer) return true;
            if (__instance.isGrabbingObjectAnimation || __instance.quickMenuManager.isMenuOpen || ___throwingObject || __instance.isTypingChat || __instance.twoHanded || __instance.activatingItem) return true;
            if (__instance.jetpackControls || __instance.disablingJetpackControls) return true;
            if (__instance.inSpecialInteractAnimation && !__instance.inVehicleAnimation) return true;

            VehicleController vehicle = Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).FirstOrDefault(x => x.localPlayerInControl);
            if (vehicle != null && vehicle.localPlayerInControl)
            {
                int gear = (int)vehicle.gear;
                float num = context.ReadValue<float>();
                if (num == 1)
                {
                    if (gear < 3)
                    {
                        vehicle.ShiftToGearAndSync(gear + 1);
                    }
                }
                else if (num == -1)
                {
                    if (gear > 1)
                    {
                        vehicle.ShiftToGearAndSync(gear - 1);
                    }
                }
                return false;
            }

            return true;
        }
    }
}