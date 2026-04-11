using GameNetcodeStuff;
using LCWildCardMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace LCWildCardMod.Items.Fyrus
{
    public class FyrusStar : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        public Transform musicAudioObject;
        public AudioSource musicSource;
        public AudioClip consumeClip;
        public AudioClip oofClip;
        public TrailRenderer trailRenderer;
        public float speedMultiplier = 1.25f;
        internal Item newProperties;
        internal static Dictionary<ulong, bool> playersEffect = new Dictionary<ulong, bool>();
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            newProperties = Instantiate(itemProperties);
            newProperties.dropSFX = null;
            EventsClass.OnRoundStart += SetPlayersDict;
            EventsClass.OnRoundEnd += ClearPlayersDict;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            EventsClass.OnRoundStart -= SetPlayersDict;
            EventsClass.OnRoundEnd -= ClearPlayersDict;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            musicAudioObject.parent = playerHeldBy.transform;
            musicAudioObject.localPosition = new Vector3(0f, 1f, -1f);
            itemProperties = newProperties;
            StartCoroutine(StarCoroutine(playerHeldBy));
            playerHeldBy.DiscardHeldObject();
            this.EnableItemMeshes(false);
            this.grabbable = false;
        }
        internal IEnumerator StarCoroutine(PlayerControllerB player)
        {
            Log.LogDebug($"{player.playerUsername} has consumed Fyrus Star");
            musicSource.PlayOneShot(consumeClip, 1f);
            playersEffect[player.playerSteamId] = true;
            yield return new WaitForSeconds(1.2f);
            Log.LogDebug($"{player.playerUsername} has begun Fyrus Star invincibility");
            player.movementSpeed *= speedMultiplier;
            musicSource.Play();
            trailRenderer.emitting = true;
            yield return new WaitForSeconds(21f);
            player.movementSpeed /= speedMultiplier;
            musicSource.PlayOneShot(oofClip, 1f);
            trailRenderer.emitting = false;
            playersEffect[player.playerSteamId] = false;
            musicAudioObject.parent = this.transform;
            Log.LogDebug($"{player.playerUsername}'s Fyrus Star invincibility has ended");
            yield return new WaitForSeconds(0.75f);
            if (base.IsServer)
            {
                this.NetworkObject.Despawn();
            }
        }
        internal void SetPlayersDict()
        {
            if (playersEffect.Count > 0)
            {
                return;
            }
            PlayerControllerB[] players = RoundManager.Instance.playersManager.allPlayerScripts;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerControllerB player = players[i];
                if (player == null)
                {
                    continue;
                }
                if (player.playerSteamId == 0)
                {
                    continue;
                }
                playersEffect.TryAdd(player.playerSteamId, false);
            }
        }
        internal void ClearPlayersDict()
        {
            playersEffect.Clear();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasPlayerConsumedStar(PlayerControllerB player)
        {
            return playersEffect.TryGetValue(player.playerSteamId, out bool effect) && effect;
        }
    }
}