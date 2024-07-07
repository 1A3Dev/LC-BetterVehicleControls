using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public static bool centerKeyPressed = false;

        [HarmonyPatch(typeof(VehicleController), "ActivateControl")]
        [HarmonyPostfix]
        public static void ActivateControl(VehicleController __instance)
        {
            __instance.setControlTips = true;
            InputActionAsset inputActionAsset = __instance.testingVehicleInEditor ? __instance.input.actions : IngamePlayerSettings.Instance.playerInput.actions;
            
            inputActionAsset.FindAction("Jump", false).performed -= __instance.DoTurboBoost;
            PluginLoader.VehicleControlsInstance.TurboKey.performed += __instance.DoTurboBoost;

            PluginLoader.VehicleControlsInstance.GearShiftForwardKey.performed += ChangeGear_Forward;
            PluginLoader.VehicleControlsInstance.GearShiftBackwardKey.performed += ChangeGear_Backward;
            PluginLoader.VehicleControlsInstance.ActivateMagnetKey.performed += ActivateMagnet;

            centerKeyPressed = false;
        }

        [HarmonyPatch(typeof(VehicleController), "DisableControl")]
        [HarmonyPostfix]
        public static void DisableControl(VehicleController __instance)
        {
            InputActionAsset inputActionAsset = __instance.testingVehicleInEditor ? __instance.input.actions : IngamePlayerSettings.Instance.playerInput.actions;
            PluginLoader.VehicleControlsInstance.TurboKey.performed -= __instance.DoTurboBoost;
            PluginLoader.VehicleControlsInstance.GearShiftForwardKey.performed -= ChangeGear_Forward;
            PluginLoader.VehicleControlsInstance.GearShiftBackwardKey.performed -= ChangeGear_Backward;
            PluginLoader.VehicleControlsInstance.ActivateMagnetKey.performed -= ActivateMagnet;
        }

        [HarmonyPatch(typeof(VehicleController), "GetVehicleInput")]
        [HarmonyPostfix]
        public static void GetVehicleInput(VehicleController __instance, ref float ___steeringWheelAnimFloat)
        {
            if (!__instance.localPlayerInControl)
            {
                centerKeyPressed = false;
                return;
            }

            __instance.brakePedalPressed = PluginLoader.VehicleControlsInstance.BrakePedalKey.IsPressed();

            int targetDirection = 0;
            if (PluginLoader.VehicleControlsInstance.GasPedalKey.IsPressed())
            {
                __instance.drivePedalPressed = true;
            }
            else if (PluginLoader.VehicleControlsInstance.MoveForwardsKey.IsPressed())
            {
                targetDirection = 1;
                __instance.drivePedalPressed = true;
            }
            else if (PluginLoader.VehicleControlsInstance.MoveBackwardsKey.IsPressed())
            {
                targetDirection = 2;
                __instance.drivePedalPressed = true;
            }
            else
            {
                __instance.drivePedalPressed = false;
            }

            if (__instance.drivePedalPressed && (
                (FixesConfig.AutoSwitchFromParked.Value && __instance.gear == CarGearShift.Park) ||
                (FixesConfig.AutoSwitchDriveReverse.Value && __instance.gear != CarGearShift.Park && targetDirection != 0)
            ))
            {
                int expectedGear = targetDirection != 2 ? (int)CarGearShift.Drive : (int)CarGearShift.Reverse;
                if ((int)__instance.gear != expectedGear)
                {
                    __instance.ShiftToGearAndSync(expectedGear);
                }
            }

            if (!centerKeyPressed && PluginLoader.VehicleControlsInstance.WheelCenterKey.triggered)
            {
                centerKeyPressed = true;
            }

            if (__instance.moveInputVector.x == 0f && (FixesConfig.RecenterWheel.Value || centerKeyPressed))
            {
                if (FixesConfig.RecenterWheelSpeed.Value < 0f)
                {
                    __instance.steeringInput = Mathf.MoveTowards(__instance.steeringInput, 0, __instance.steeringWheelTurnSpeed * Time.deltaTime);
                }
                else if (FixesConfig.RecenterWheelSpeed.Value > 0f)
                {
                    __instance.steeringInput = Mathf.MoveTowards(__instance.steeringInput, 0, FixesConfig.RecenterWheelSpeed.Value * Time.deltaTime);
                }
                else
                {
                    __instance.steeringInput = __instance.moveInputVector.x;
                    __instance.steeringAnimValue = __instance.steeringInput;
                    ___steeringWheelAnimFloat = __instance.steeringAnimValue;
                }

                if (centerKeyPressed && __instance.steeringInput == 0f)
                {
                    centerKeyPressed = false;
                }
            }
        }

        [HarmonyPatch(typeof(VehicleController), "SetCarEffects")]
        [HarmonyPrefix]
        public static void SetCarEffects(VehicleController __instance, ref float setSteering)
        {
            setSteering = 0f;
            __instance.steeringWheelAnimFloat = __instance.steeringInput / 6f;
        }

        public static void ChangeGear_Forward(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            VehicleController vehicle = Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).FirstOrDefault(x => x.localPlayerInControl);
            if (vehicle != null && vehicle.localPlayerInControl)
            {
                int gear = (int)vehicle.gear;
                if (gear < 3)
                {
                    vehicle.ShiftToGearAndSync(gear + 1);
                }
            }
        }
        public static void ChangeGear_Backward(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            VehicleController vehicle = Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None).FirstOrDefault(x => x.localPlayerInControl);
            if (vehicle != null && vehicle.localPlayerInControl)
            {
                int gear = (int)vehicle.gear;
                if (gear > 1)
                {
                    vehicle.ShiftToGearAndSync(gear - 1);
                }
            }
        }

        public static void ActivateMagnet(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            GameObject magnetLever = GameObject.Find("Environment/HangarShip/MagnetLever");
            if (magnetLever == null) return;

            AnimatedObjectTrigger magnetTrigger = magnetLever.GetComponent<AnimatedObjectTrigger>();
            if (magnetTrigger == null) return;

            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (Vector3.Distance(player.transform.position, StartOfRound.Instance.magnetPoint.position) >= 10f) return;

            if (magnetTrigger.boolValue) return; // Only allow turning on the magnet

            magnetTrigger.TriggerAnimation(player);
            string newState = magnetTrigger.boolValue ? "on" : "off";
            HUDManager.Instance.AddChatMessage($"You turned {newState} the magnet!");
        }

        [HarmonyPatch(typeof(VehicleController), "AddTurboBoost")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AddTurboBoost(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
            {
                if (!alreadyReplaced && instruction.opcode == OpCodes.Ldc_I4_5)
                {
                    alreadyReplaced = true;
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PluginLoader), "maxTurboBoosts"));
                    newInstructions.Add(codeInstruction);
                    continue;
                }

                newInstructions.Add(instruction);
            }

            if (!alreadyReplaced) PluginLoader.logSource.LogWarning("AddTurboBoost failed to replace maxTurboBoosts");

            return newInstructions.AsEnumerable();
        }

        [HarmonyPatch(typeof(VehicleController), "ReactToDamage")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ReactToDamage(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
            {
                if (!alreadyReplaced && instruction.opcode == OpCodes.Ldc_R4 && instruction.operand?.ToString() == "5")
                {
                    alreadyReplaced = true;
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PluginLoader), "maxTurboBoosts"));
                    newInstructions.Add(codeInstruction);

                    CodeInstruction codeInstructionConv = new CodeInstruction(OpCodes.Conv_R4);
                    newInstructions.Add(codeInstructionConv);
                    continue;
                }

                newInstructions.Add(instruction);
            }

            if (!alreadyReplaced) PluginLoader.logSource.LogWarning("ReactToDamage failed to replace maxTurboBoosts");

            return newInstructions.AsEnumerable();
        }
    }
}