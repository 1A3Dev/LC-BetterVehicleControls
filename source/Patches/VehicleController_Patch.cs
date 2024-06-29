using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetterVehicleControls.Patches
{
    [HarmonyPatch]
    internal static class Patches_VehicleController
    {
        //[HarmonyPatch(typeof(VehicleController), "TryIgnition")]
        //[HarmonyPrefix]
        //public static void TryIgnition(ref float ___chanceToStartIgnition)
        //{
        //    ___chanceToStartIgnition = 100f;
        //}

        [HarmonyPatch(typeof(VehicleController), "RemoveKeyFromIgnition")]
        [HarmonyPostfix]
        public static void RemoveKeyFromIgnition(VehicleController __instance)
        {
            if (FixesConfig.AutomaticPark.Value && __instance.localPlayerInControl)
            {
                int expectedGear = (int)CarGearShift.Park;
                if ((int)__instance.gear != expectedGear)
                {
                    __instance.ShiftToGearAndSync(expectedGear);
                }
            }
        }

        [HarmonyPatch(typeof(VehicleController), "GetVehicleInput")]
        [HarmonyPostfix]
        public static void GetVehicleInput(VehicleController __instance)
        {
            if (!__instance.localPlayerInControl)
            {
                return;
            }

            if (__instance.testingVehicleInEditor)
            {
                __instance.brakePedalPressed = __instance.input.actions.FindAction("Jump", false).ReadValue<float>() > 0;
            }
            else
            {
                __instance.brakePedalPressed = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Jump", false).ReadValue<float>() > 0;
            }

            if (FixesConfig.AutomaticGears.Value)
            {
                __instance.drivePedalPressed = __instance.moveInputVector.y > 0.1f || __instance.moveInputVector.y < -0.1f;
                if (__instance.drivePedalPressed)
                {
                    int expectedGear = __instance.moveInputVector.y > 0.1f ? (int)CarGearShift.Drive : (int)CarGearShift.Reverse;
                    if ((int)__instance.gear != expectedGear)
                    {
                        __instance.ShiftToGearAndSync(expectedGear);
                    }
                }
            }
            else
            {
                __instance.drivePedalPressed = (__instance.gear == CarGearShift.Drive && __instance.moveInputVector.y > 0.1f) || (__instance.gear == CarGearShift.Reverse && __instance.moveInputVector.y < -0.1f);
            }
        }
    }
}