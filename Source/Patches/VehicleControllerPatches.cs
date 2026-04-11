using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(VehicleController))]
    public static class VehicleControllerPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo gameNetworkInstanceMethod = AccessTools.Method(typeof(GameNetworkManager), "get_Instance");
        static MethodInfo killPlayerMethod = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer), new Type[] { typeof(Vector3), typeof(bool), typeof(CauseOfDeath), typeof(int), typeof(Vector3), typeof(bool) });
        static MethodInfo exitDriverMethod = AccessTools.Method(typeof(VehicleController), nameof(VehicleController.ExitDriverSideSeat));
        static MethodInfo exitPassengerMethod = AccessTools.Method(typeof(VehicleController), nameof(VehicleController.ExitPassengerSideSeat));
        [HarmonyPatch(nameof(VehicleController.DestroyCar))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(killPlayerMethod) && codes[i - 16].Calls(gameNetworkInstanceMethod))
                {
                    List<CodeInstruction> preCode = new List<CodeInstruction>();
                    List<CodeInstruction> postCode = new List<CodeInstruction>();
                    Label skipKill = generator.DefineLabel();
                    Label skipNew = generator.DefineLabel();
                    preCode.Add(new CodeInstruction(OpCodes.Ldloc_0));
                    preCode.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    preCode.Add(new CodeInstruction(OpCodes.Brtrue_S, skipKill));
                    postCode.Add(new CodeInstruction(OpCodes.Br_S, skipNew));
                    CodeInstruction exitDriverLoad = new CodeInstruction(OpCodes.Ldarg_0);
                    exitDriverLoad.labels.Add(skipKill);
                    postCode.Add(exitDriverLoad);
                    postCode.Add(new CodeInstruction(OpCodes.Call, exitDriverMethod));
                    postCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    postCode.Add(new CodeInstruction(OpCodes.Call, exitPassengerMethod));
                    codes[i + 1].labels.Add(skipNew);
                    codes.InsertRange(i + 1, postCode);
                    codes.InsertRange(i - 16, preCode);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}