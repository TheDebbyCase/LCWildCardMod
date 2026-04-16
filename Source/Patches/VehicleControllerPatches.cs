using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(VehicleController))]
    public static class VehicleControllerPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(VehicleController.DestroyCar))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(TranspilerHelper.killPlayer) && codes[i - 16].Calls(TranspilerHelper.gameNetworkInstance))
                {
                    List<CodeInstruction> preCode = new List<CodeInstruction>();
                    List<CodeInstruction> postCode = new List<CodeInstruction>();
                    Label skipKill = generator.DefineLabel();
                    Label skipNew = generator.DefineLabel();
                    preCode.Add(new CodeInstruction(OpCodes.Ldloc_S, 0));
                    preCode.Add(new CodeInstruction(OpCodes.Ldnull));
                    preCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.anySave));
                    preCode.Add(new CodeInstruction(OpCodes.Brtrue_S, skipKill));
                    postCode.Add(new CodeInstruction(OpCodes.Br_S, skipNew));
                    CodeInstruction exitDriverLoad = new CodeInstruction(OpCodes.Ldarg_S, 0);
                    exitDriverLoad.labels.Add(skipKill);
                    postCode.Add(exitDriverLoad);
                    postCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.exitDriver));
                    postCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    postCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.exitPassenger));
                    codes[i + 1].labels.Add(skipNew);
                    codes.InsertRange(i + 1, postCode);
                    codes.InsertRange(i - 16, preCode);
                    break;
                }
            }
            return codes;
        }
    }
}