using GameNetcodeStuff;
using System.Collections;
using UnityEngine;
namespace LCWildCardMod.Items.Fyrus
{
    public class FyrusStar : PhysicsProp
    {
        readonly BepInEx.Logging.ManualLogSource log = WildCardMod.Log;
        public Transform musicAudioObject;
        public AudioSource musicSource;
        public AudioClip consumeClip;
        public AudioClip oofClip;
        public TrailRenderer trailRenderer;
        public Item newProperties;
        public PlayerControllerB consumedPlayer;
        public bool starEffect;
        public float speedMultiplier = 1.25f;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            newProperties = Instantiate(itemProperties);
            newProperties.dropSFX = null;
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
        }
        public IEnumerator StarCoroutine(PlayerControllerB player)
        {
            log.LogDebug($"{player.playerUsername} has consumed Fyrus Star");
            musicSource.PlayOneShot(consumeClip, 1f);
            consumedPlayer = player;
            yield return new WaitForSeconds(1.2f);
            log.LogDebug($"{player.playerUsername} has begun Fyrus Star incincibility");
            starEffect = true;
            player.movementSpeed *= speedMultiplier;
            musicSource.Play();
            trailRenderer.emitting = true;
            yield return new WaitForSeconds(21f);
            player.movementSpeed /= speedMultiplier;
            musicSource.PlayOneShot(oofClip, 1f);
            trailRenderer.emitting = false;
            musicAudioObject.parent = this.transform;
            starEffect = false;
            log.LogDebug($"{player.playerUsername}'s Fyrus Star invincibility has ended");
            yield return new WaitForSeconds(0.75f);
            if (base.IsServer)
            {
                this.NetworkObject.Despawn();
            }
        }
    }
}