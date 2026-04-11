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
    [HarmonyPatch(typeof(JesterAI))]
    public static class JesterAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        static MethodInfo haloSaveMethod = AccessTools.Method(typeof(Extensions), nameof(Extensions.SaveIfHalo), new Type[] { typeof(PlayerControllerB) });
        static MethodInfo jesterSwitchMethod = AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourState), new Type[] { typeof(int) });
        static MethodInfo inequalityMethod = AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) });
        [HarmonyPatch(nameof(JesterAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode.Equals(OpCodes.Ldnull) && codes[i - 1].IsLdloc() && codes[i + 1].Calls(inequalityMethod) && codes[i + 2].Branches(out _))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, haloSaveMethod));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    newCode.Add(new CodeInstruction(OpCodes.Call, jesterSwitchMethod));
                    newCode.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i + 3].labels.Add(newLabel);
                    codes.InsertRange(i + 3, newCode);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}