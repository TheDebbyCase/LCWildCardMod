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
        public Coroutine starCoroutine;
        public bool starEffect;
        public float speedMultiplier = 1.25f;
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            musicAudioObject.parent = playerHeldBy.transform;
            musicAudioObject.localPosition = new Vector3(0f, 1f, -1f);
            itemProperties.dropSFX = null;
            starCoroutine = StartCoroutine(StarCoroutine(playerHeldBy));
            playerHeldBy.DiscardHeldObject();
            this.EnableItemMeshes(false);
        }
        public IEnumerator StarCoroutine(PlayerControllerB player)
        {
            musicSource.PlayOneShot(consumeClip, 1f);
            yield return new WaitForSeconds(1.2f);
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
            yield return new WaitForSeconds(0.75f);
            this.NetworkObject.Despawn();
        }
    }
}