using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LCWildCardMod.Items.Fyrus
{
    public class FyrusStar : PhysicsProp
    {
        BepInEx.Logging.ManualLogSource Log => WildCardMod.Instance.Log;
        internal static List<FyrusStar> allSpawnedStars = new List<FyrusStar>();
        public Transform musicAudioObject;
        public AudioSource musicSource;
        public AudioClip consumeClip;
        public AudioClip oofClip;
        public TrailRenderer trailRenderer;
        public float speedMultiplier = 1.25f;
        internal Item newProperties;
        internal PlayerControllerB affectingPlayer = null;
        internal float hitCooldownMax = 0.5f;
        internal float hitCooldown;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            allSpawnedStars.Add(this);
            hitCooldown = hitCooldownMax;
            newProperties = Instantiate(itemProperties);
            newProperties.dropSFX = null;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            allSpawnedStars.Remove(this);
            if (affectingPlayer != null)
            {
                affectingPlayer.movementSpeed /= speedMultiplier;
            }
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            affectingPlayer = playerHeldBy;
            musicAudioObject.parent = affectingPlayer.transform;
            musicAudioObject.localPosition = new Vector3(0f, 1f, -1f);
            itemProperties = newProperties;
            affectingPlayer.DiscardHeldObject();
            EnableItemMeshes(false);
            grabbable = false;
            StartCoroutine(StarCoroutine());
        }
        public override void Update()
        {
            base.Update();
            if (affectingPlayer == null)
            {
                return;
            }
            if (hitCooldown <= 0f)
            {
                return;
            }
            hitCooldown -= Time.deltaTime;
        }
        internal IEnumerator StarCoroutine()
        {
            Log.LogDebug($"{affectingPlayer.playerUsername} has consumed Fyrus Star");
            musicSource.PlayOneShot(consumeClip, 1f);
            yield return new WaitForSeconds(1.2f);
            Log.LogDebug($"{affectingPlayer.playerUsername} has begun Fyrus Star invincibility");
            affectingPlayer.movementSpeed *= speedMultiplier;
            musicSource.Play();
            trailRenderer.emitting = true;
            yield return new WaitForSeconds(21f);
            affectingPlayer.movementSpeed /= speedMultiplier;
            musicSource.PlayOneShot(oofClip, 1f);
            trailRenderer.emitting = false;
            musicAudioObject.parent = transform;
            Log.LogDebug($"{affectingPlayer.playerUsername}'s Fyrus Star invincibility has ended");
            affectingPlayer = null;
            yield return new WaitForSeconds(0.75f);
            if (base.IsServer)
            {
                NetworkObject.Despawn();
            }
        }
    }
}