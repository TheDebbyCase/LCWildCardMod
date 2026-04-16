using GameNetcodeStuff;
using HarmonyLib;
using LCWildCardMod.Items;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
namespace LCWildCardMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerControllerBPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool SavePlayerDamage(PlayerControllerB __instance, ref Vector3 force)
        {
            bool saved = __instance.SaveIfFyrus();
            if (saved)
            {
                __instance.externalForceAutoFade += force;
            }
            return !saved;
        }
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SavePlayerDamage(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            LocalBuilder overkill = null;
            LocalBuilder healthOverriden = null;
            Label? oldLabel1 = null;
            Label? oldLabel2 = null;
            Label? oldLabel3 = null;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (i - 3 < 0 || i + 6 >= codes.Count)
                {
                    continue;
                }
                if (codes[i].Calls(TranspilerHelper.allowDeath))
                {
                    Log.LogDebug("Found PlayerControllerB.AllowPlayerDeath, replacing next branch with setting new locals");
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j - 1].Branches(out _) || !codes[j].opcode.Equals(OpCodes.Ret))
                        {
                            continue;
                        }
                        for (int k = j; k < codes.Count; k++)
                        {
                            if (codes[k].Branches(out oldLabel1))
                            {
                                overkill = generator.DeclareLocal(typeof(bool));
                                healthOverriden = generator.DeclareLocal(typeof(bool));
                                List<CodeInstruction> setNewLocals = new List<CodeInstruction>();
                                codes[k] = new CodeInstruction(OpCodes.Cgt);
                                setNewLocals.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                                setNewLocals.Add(new CodeInstruction(OpCodes.Ceq));
                                setNewLocals.Add(new CodeInstruction(OpCodes.Stloc_S, overkill.LocalIndex));
                                //setNewLocals.AddRange(TranspilerHelper.DebugLoadFromThis<int>("Starting health", OpCodes.Ldfld, TranspilerHelper.playerHealth));
                                //setNewLocals.AddRange(TranspilerHelper.DebugLoad<int>("Damage", OpCodes.Ldarg_S, 1));
                                //setNewLocals.AddRange(TranspilerHelper.DebugLoad<bool>("Is Overkill?", OpCodes.Ldloc_S, overkill.LocalIndex));
                                setNewLocals.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                                setNewLocals.Add(new CodeInstruction(OpCodes.Stloc_S, healthOverriden.LocalIndex));
                                codes.InsertRange(k + 1, setNewLocals);
                                i = k + setNewLocals.Count + 1;
                                break;
                            }
                        }
                        if (overkill == null || !oldLabel1.HasValue)
                        {
                            Log.LogWarning("Unable to apply transpiler to PlayerControllerB.DamagePlayer! Halo save will not work properly!");
                            return codes;
                        }
                        break;
                    }
                }
                if (overkill == null || !oldLabel1.HasValue)
                {
                    continue;
                }
                if (!oldLabel2.HasValue)
                {
                    codes[i].Branches(out oldLabel2);
                }
                else if (!oldLabel3.HasValue && codes[i].Branches(out oldLabel3))
                {
                    Log.LogDebug("Successfully set locals, adding overkill checks");
                    List<CodeInstruction> newBranch = new List<CodeInstruction>();
                    newBranch.Add(new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex));
                    newBranch.Add(new CodeInstruction(OpCodes.Brfalse_S, oldLabel1.Value));
                    newBranch.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 1));
                    newBranch.Add(new CodeInstruction(OpCodes.Stloc_S, healthOverriden.LocalIndex));
                    //newBranch.AddRange(TranspilerHelper.DebugLoad<bool>("Vanilla critical injury, setting health override to", OpCodes.Ldloc_S, healthOverriden.LocalIndex));
                    codes.InsertRange(i + 1, newBranch);
                    i += newBranch.Count + 1;
                    List<CodeInstruction> newCode = new List<CodeInstruction>();
                    Label newLabel = generator.DefineLabel();
                    Label noSaveJump = generator.DefineLabel();
                    Label overridenJump = generator.DefineLabel();
                    CodeInstruction elseIfCode = new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex);
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (codes[j].Branches(out _))
                        {
                            i = j;
                        }
                        if (codes[j].labels.Count < 3)
                        {
                            continue;
                        }
                        Log.LogDebug("Moving first if statement's branch destinations to the new else if statement");
                        codes[j].MoveLabelsTo(elseIfCode);
                        break;
                    }
                    newCode.Add(elseIfCode);
                    newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, newLabel));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ceq));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, overkill.LocalIndex));
                    //newCode.AddRange(TranspilerHelper.DebugLoad<bool>("Checking for halo, setting Overkill to", OpCodes.Ldloc_S, overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, noSaveJump));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 1));
                    newCode.Add(new CodeInstruction(OpCodes.Stloc_S, healthOverriden.LocalIndex));
                    newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                    newCode.Add(new CodeInstruction(OpCodes.Ldc_I4_S, 100));
                    newCode.Add(new CodeInstruction(OpCodes.Stfld, TranspilerHelper.playerHealth));
                    //newCode.AddRange(TranspilerHelper.DebugString("Saved player from death. Setting health to 100"));
                    newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, healthOverriden.LocalIndex).WithLabels(newLabel, noSaveJump));
                    newCode.Add(new CodeInstruction(OpCodes.Brtrue_S, overridenJump));
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j].Calls(TranspilerHelper.mathfClamp3Int))
                        {
                            continue;
                        }
                        for (int k = j; k < codes.Count; k++)
                        {
                            if (!codes[k].StoresField(TranspilerHelper.playerHealth))
                            {
                                continue;
                            }
                            Log.LogDebug("Found health clamp, adding overriden branch after");
                            codes[k + 1].labels.Add(overridenJump);
                            break;
                        }
                        break;
                    }
                    codes.InsertRange(i + 1, newCode);
                }
                else if (codes[i].Calls(TranspilerHelper.killPlayer))
                {
                    Log.LogDebug("Found PlayerControllerB.KillPlayer, adding overkillJump jump after next branch");
                    for (int j = i; j < codes.Count; j++)
                    {
                        if (!codes[j].Branches(out _))
                        {
                            continue;
                        }
                        List<CodeInstruction> newCode = new List<CodeInstruction>();
                        Label overkillJump = generator.DefineLabel();
                        newCode.Add(new CodeInstruction(OpCodes.Ldloc_S, overkill.LocalIndex));
                        newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, overkillJump));
                        newCode.AddRange(TranspilerHelper.DebugString("Critically injuring player"));
                        codes[j + 1].MoveLabelsTo(newCode[0]);
                        for (int k = j; k < codes.Count; k++)
                        {
                            if (codes[k].Calls(TranspilerHelper.makeInjured))
                            {
                                Log.LogDebug("Found PlayerControllerB.MakeCriticallyInjured, adding overkillJump destination to next branch");
                                for (int l = k; l < codes.Count; l++)
                                {
                                    if (codes[l].Branches(out _))
                                    {
                                        codes[l + 1].labels.Add(overkillJump);
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        codes.InsertRange(j + 1, newCode);
                        break;
                    }
                    break;
                }
            }
            return codes;
        }
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool SavePlayerKill(PlayerControllerB __instance, ref Vector3 bodyVelocity, ref bool spawnBody, ref CauseOfDeath causeOfDeath)
        {
            if (!__instance.IsSaveable(out bool starSave, out SmithHalo haloRef) || causeOfDeath == CauseOfDeath.Unknown || !spawnBody)
            {
                return true;
            }
            if (starSave)
            {
                __instance.externalForceAutoFade += bodyVelocity;
            }
            else
            {
                Log.LogDebug("Running Halo Exhaust from Kill");
                haloRef.ExhaustLocal(__instance, bodyVelocity);
            }
            return false;
        }
        [HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool PreventFallDamage(PlayerControllerB __instance)
        {
            try
            {
                Cojiro cojiroRef = null;
                if (__instance.isHoldingObject && __instance.currentlyHeldObjectServer.TryGetComponent(out cojiroRef) && cojiroRef.isFloating && __instance.fallValue >= -38f)
                {
                    Log.LogDebug($"Cojiro Preventing Fall Damage");
                    __instance.fallValue = -10f;
                    __instance.fallValueUncapped = -10f;
                    cojiroRef.itemAnimator.Animator.SetBool("Floating", false);
                }
                else if (cojiroRef == null && __instance.fallValue >= -38f)
                {
                    for (int i = 0; i < __instance.ItemSlots.Length; i++)
                    {
                        if (__instance.ItemSlots[i] == null)
                        {
                            continue;
                        }
                        if (!__instance.ItemSlots[i].TryGetComponent(out cojiroRef))
                        {
                            continue;
                        }
                        if (cojiroRef.previousPlayer == __instance && cojiroRef.currentUseCooldown > 0)
                        {
                            Log.LogDebug($"Cojiro Preventing Fall Damage");
                            __instance.fallValue = -10f;
                            __instance.fallValueUncapped = -10f;
                            cojiroRef.itemAnimator.Animator.SetBool("Floating", false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(nameof(PlayerControllerB.CheckConditionsForSinkingInQuicksand))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void AntiQuicksandStar(PlayerControllerB __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            __result = !__instance.SaveIfFyrus();
        }
        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        [HarmonyWrapSafe]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> HaloSave(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(TranspilerHelper.killPlayer))
                {
                    continue;
                }
                int preKillIndex = -1;
                bool checkForPreSink = false;
                for (int j = i; j > 0; j--)
                {
                    if (!checkForPreSink && codes[j].LoadsField(TranspilerHelper.playerSinking))
                    {
                        checkForPreSink = true;
                        continue;
                    }
                    if (preKillIndex == -1 && codes[j].Branches(out _))
                    {
                        preKillIndex = j;
                    }
                }
                Label? skipLabel = null;
                Label? skipKillDestinationRef = null;
                bool checkForPostSink = false;
                for (int j = i; j < codes.Count; j++)
                {
                    if (!checkForPostSink && codes[j].LoadsField(TranspilerHelper.playerSinking))
                    {
                        checkForPostSink = true;
                        continue;
                    }
                    if (!skipKillDestinationRef.HasValue)
                    {
                        codes[j].Branches(out skipKillDestinationRef);
                    }
                    else if (checkForPreSink && checkForPostSink && codes[j].labels.Contains(skipKillDestinationRef.Value))
                    {
                        skipLabel = generator.DefineLabel();
                        codes[j].labels.Add(skipLabel.Value);
                        break;
                    }
                }
                if (!checkForPreSink || !checkForPostSink)
                {
                    continue;
                }
                i = preKillIndex;
                List<CodeInstruction> newCode = new List<CodeInstruction>();
                Label killLabel = generator.DefineLabel();
                newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                newCode.Add(new CodeInstruction(OpCodes.Call, TranspilerHelper.haloSave));
                newCode.Add(new CodeInstruction(OpCodes.Brfalse_S, killLabel));
                newCode.Add(new CodeInstruction(OpCodes.Ldarg_S, 0));
                newCode.Add(new CodeInstruction(OpCodes.Ldc_R4, 0f));
                newCode.Add(new CodeInstruction(OpCodes.Stfld, TranspilerHelper.playerSinking));
                newCode.Add(new CodeInstruction(OpCodes.Br_S, skipLabel.Value));
                codes[i + 1].labels.Add(killLabel);
                codes.InsertRange(i + 1, newCode);
                break;
            }
            return codes;
        }
    }
}