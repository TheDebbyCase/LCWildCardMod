using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(CadaverGrowthAI))]
    public class CadaverGrowthAIPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(CadaverGrowthAI.CurePlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool CureMore(CadaverGrowthAI __instance)
        {
            if (__instance.playerInfections[(int)GameNetworkManager.Instance.localPlayerController.playerClientId].infected)
            {
                __instance.numberOfInfected--;
            }
            HUDManager.Instance.cadaverFilter = 0f;
            SoundManager.Instance.alternateEarsRinging = false;
            SoundManager.Instance.earsRingingTimer = 0f;
            return true;
        }
        [HarmonyPatch(nameof(CadaverGrowthAI.BurstFromPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SaveFromBurst(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].LoadsField(TranspilerHelper.hasBurst))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label resumeBurstLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Ldnull));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.anySave));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, resumeBurstLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Ldfld, TranspilerHelper.playerClientId));
                    newCode.Add(new CodeInstruction(OpCodes.Conv_I4));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.cadaverCure));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Ldfld, TranspilerHelper.playerClientId));
                    newCode.Add(new CodeInstruction(OpCodes.Conv_I4));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.cadaverCureRPC));
                    newCode.Add(new CodeInstruction(OpCodes.Ret));
                    codes[i + 2].labels.Add(resumeBurstLabel);
                    codes.InsertRange(i + 2, newCode);
                    break;
                }
            }
            return codes;
        }
    }
}