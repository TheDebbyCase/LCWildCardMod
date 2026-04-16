using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(BushWolfEnemy))]
    public static class BushWolfEnemyPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(BushWolfEnemy.OnCollideWithPlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FyrusStarEffect(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            LocalBuilder playerLocal = null;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(TranspilerHelper.collision) && codes[i + 1].opcode.Equals(OpCodes.Ldnull) && codes[i + 2].Calls(TranspilerHelper.inequality) && codes[i + 3].Branches(out _))
                {
                    List<CodeInstruction> newLocalDefineCode = new List<CodeInstruction>();
                    playerLocal = generator.DeclareLocal(typeof(PlayerControllerB));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 1));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldfld, TranspilerHelper.foxInKill));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.collision));
                    newLocalDefineCode.Add(new CodeInstruction(OpCodes.Stloc_S, playerLocal.LocalIndex));
                    codes[i] = new CodeInstruction(OpCodes.Ldloc_S, playerLocal.LocalIndex);
                    codes[i].labels = new List<Label>(codes[i - 5].labels);
                    codes.InsertRange(i - 17, newLocalDefineCode);
                    codes.RemoveRange((i + newLocalDefineCode.Count) - 5, 5);
                }
                if (playerLocal != null && codes[i].IsLdloc() && codes[i + 1].Branches(out Label? notFlagJump) && codes[i + 2].Calls(TranspilerHelper.gameNetworkInstance))
                {
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label notSavedLabel = generator.DefineLabel();
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, playerLocal.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.anySave));
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, notSavedLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.foxCancelReel));
                    codes[i + 2].labels.Add(notSavedLabel);
                    codes.InsertRange(i + 2, newCode);
                    //temp
                    newCode.Clear();
                    newCode.AddRange(TranspilerHelper.DebugLoad<bool>("Tried killing", OpCodes.Ldloc_S, 1, notFlagJump.Value));
                    newCode.AddRange(TranspilerHelper.DebugPlayerName(OpCodes.Ldloc_S, playerLocal.LocalIndex));
                    newCode.AddRange(TranspilerHelper.DebugLoadFromThis<bool>("Dragging", OpCodes.Ldfld, TranspilerHelper.foxDragging));
                    codes.InsertRange(codes.Count - 1, newCode);
                    codes.Last().labels.Remove(notFlagJump.Value);
                    //temp
                    break;
                }
            }
            return codes;
        }
    }
}