using HarmonyLib;
using LCWildCardMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace LCWildCardMod.Patches
{
    public static class NecessaryPatches
    {
        static BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool StartOfRound_EndRoundInvoke()
        {
            try
            {
                EventsClass.RoundEnded();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartDisconnect))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool GameNetworkManager_EndRoundInvoke()
        {
            try
            {
                EventsClass.RoundEnded();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
            return true;
        }
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.waitForMainEntranceTeleportToSpawn))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void RoundManager_StartRoundInvoke()
        {
            try
            {
                EventsClass.RoundStarted();
            }
            catch (Exception exception)
            {
                Log.LogError(exception);
            }
        }
        [HarmonyPatch(typeof(InitializeGame), nameof(InitializeGame.Start))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void InitializeGame_ClearTranspilerHelper()
        {
            Traverse traverse = Traverse.Create(typeof(HarmonyHelper));
            List<string> fields = traverse.Fields();
            for (int i = 0; i < fields.Count; i++)
            {
                Traverse field = traverse.Field(fields[i]);
                if (field.GetValueType() != typeof(MethodInfo) && field.GetValueType() != typeof(FieldInfo))
                {
                    continue;
                }
                field.SetValue(null);
            }
        }
        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UpdateHealthUI))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool Temp(ref int health)
        {
            Log.LogDebug($"UPDATING HEALTH UI WITH {health}");
            return true;
        }
    }
}