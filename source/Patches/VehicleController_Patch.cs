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
        [HarmonyPrefix]
        public static void RemoveKeyFromIgnition(VehicleController __instance)
        {
            if (FixesConfig.AutomaticPark.Value)
            {
                int expectedGear = (int)CarGearShift.Park;
                if ((int)__instance.gear != expectedGear)
                {
                    __instance.ShiftToGearAndSync(expectedGear);
                }
            }
        }

        [HarmonyPatch(typeof(VehicleController), "GetVehicleInput")]
        [HarmonyPrefix]
        public static bool GetVehicleInput(VehicleController __instance)
        {
            if (!__instance.localPlayerInControl)
            {
                return false;
            }

            InputActionAsset inputActions;
            if (__instance.testingVehicleInEditor)
            {
                inputActions = __instance.input.actions;
            }
            else
            {
                inputActions = IngamePlayerSettings.Instance.playerInput.actions;
            }
            __instance.moveInputVector = inputActions.FindAction("Move", false).ReadValue<Vector2>();

            float num = __instance.steeringWheelTurnSpeed;
            __instance.steeringInput = Mathf.Clamp(__instance.steeringInput + __instance.moveInputVector.x * num * Time.deltaTime, -3f, 3f);
            if (Mathf.Abs(__instance.moveInputVector.x) > 0.1f)
            {
                __instance.steeringWheelAudio.volume = Mathf.Lerp(__instance.steeringWheelAudio.volume, Mathf.Abs(__instance.moveInputVector.x), 5f * Time.deltaTime);
            }
            else
            {
                __instance.steeringWheelAudio.volume = Mathf.Lerp(__instance.steeringWheelAudio.volume, 0f, 5f * Time.deltaTime);
            }
            __instance.steeringAnimValue = __instance.moveInputVector.x;

            __instance.brakePedalPressed = inputActions.FindAction("Jump", false).ReadValue<float>() > 0;
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

            return false;
        }
    }
}