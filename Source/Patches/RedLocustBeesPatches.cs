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
    [HarmonyPatch(typeof(RedLocustBees))]
    public static class RedLocustBeesPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo beeKillMethod = AccessTools.Method(typeof(RedLocustBees), nameof(RedLocustBees.BeeKillPlayerOnLocalClient), new Type[] { typeof(int) });
        static FieldInfo zapModeField = AccessTools.Field(typeof(RedLocustBees), nameof(RedLocustBees.beesZappingMode));
        [HarmonyPatch(nameof(RedLocustBees.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            Label? newLabel = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(beeKillMethod) && codes[i - 6].Branches(out _) && !codes[i - 7].Calls(haloSaveMethod))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, newLabel.Value));
                    codes.InsertRange(i - 5, newCode);
                }
                if (newLabel.HasValue && codes[i].LoadsField(zapModeField))
                {
                    codes[i - 1].labels.Add(newLabel.Value);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}