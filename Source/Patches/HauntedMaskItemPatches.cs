using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(HauntedMaskItem))]
    public static class HauntedMaskItemPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo allowDeathMethod = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.AllowPlayerDeath));
        static FieldInfo maskHeldField = AccessTools.Field(typeof(HauntedMaskItem), nameof(HauntedMaskItem.previousPlayerHeldBy));
        [HarmonyPatch(nameof(HauntedMaskItem.FinishAttaching))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            List<CodeInstruction> newCodes = new List<CodeInstruction>();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(allowDeathMethod) && codes[i + 1].Branches(out Label? label) && label.HasValue)
                {
                    newCodes.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
                    newCodes.Add(new CodeInstruction(OpCodes.Ceq));
                    newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newCodes.Add(new CodeInstruction(OpCodes.Ldfld, maskHeldField));
                    newCodes.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    newCodes.Add(new CodeInstruction(OpCodes.Or));
                    codes[i + 1] = new CodeInstruction(OpCodes.Brfalse_S, label.Value);
                    codes.InsertRange(i + 1, newCodes);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}