using HarmonyLib;

namespace BetterVehicleControls.Patches
{
    [HarmonyPatch]
    internal static class Patches_VehicleController
    {
        [HarmonyPatch(typeof(VehicleController), "TryIgnition")]
        [HarmonyPrefix]
        public static void TryIgnition(ref float ___chanceToStartIgnition)
        {
            if (FixesConfig.ChanceToStartIgnition.Value > 0)
            {
                ___chanceToStartIgnition = FixesConfig.ChanceToStartIgnition.Value;
            }
        }

        [HarmonyPatch(typeof(VehicleController), "RemoveKeyFromIgnition")]
        [HarmonyPostfix]
        public static void RemoveKeyFromIgnition(VehicleController __instance)
        {
            if (FixesConfig.AutoSwitchToParked.Value && __instance.localPlayerInControl)
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
        public static void GetVehicleInput(VehicleController __instance, ref float ___steeringWheelAnimFloat)
        {
            if (!__instance.localPlayerInControl)
            {
                return;
            }

            if (!FixesConfig.VanillaControls.Value)
            {
                if (__instance.testingVehicleInEditor)
                {
                    __instance.brakePedalPressed = __instance.input.actions.FindAction("Jump", false).ReadValue<float>() > 0;
                }
                else
                {
                    __instance.brakePedalPressed = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Jump", false).ReadValue<float>() > 0;
                }

                if (FixesConfig.AutoSwitchDriveReverse.Value)
                {
                    __instance.drivePedalPressed = __instance.moveInputVector.y > 0.1f || __instance.moveInputVector.y < -0.1f;
                    if (__instance.drivePedalPressed && __instance.gear != CarGearShift.Park)
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
                    __instance.drivePedalPressed = (__instance.gear != CarGearShift.Reverse && __instance.moveInputVector.y > 0.1f) || (__instance.gear != CarGearShift.Drive && __instance.moveInputVector.y < -0.1f);
                }
            }

            if (FixesConfig.AutoSwitchFromParked.Value && (__instance.drivePedalPressed || (FixesConfig.VanillaControls.Value && __instance.brakePedalPressed)) && __instance.gear == CarGearShift.Park)
            {
                int expectedGear = __instance.moveInputVector.y > 0.1f ? (int)CarGearShift.Drive : (int)CarGearShift.Reverse;
                __instance.ShiftToGearAndSync(expectedGear);
            }

            if (__instance.moveInputVector.x == 0f && FixesConfig.RecenterWheel.Value)
            {
                __instance.steeringInput = 0f;
                __instance.steeringAnimValue = __instance.steeringInput;
                ___steeringWheelAnimFloat = __instance.steeringAnimValue;
            }
        }
    }
}