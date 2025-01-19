using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LCWildCardMod
{
    public class ChangePixelJarFloaterMat : NetworkBehaviour
    {
        public Texture chosenTexture;
        void Awake()
        {
            if (chosenTexture == null)
            {
                if (WildCardMod.randomSeed == 0)
                {
                    WildCardMod.randomSeed = StartOfRound.Instance.randomMapSeed;
                }
                else if (StartOfRound.Instance == null)
                {
                    WildCardMod.randomSeed = 0;
                }
                else
                {
                    WildCardMod.randomSeed = WildCardMod.randomSeed + 1;
                }
                System.Random rng = new System.Random(WildCardMod.randomSeed);
                chosenTexture = WildCardMod.floaterTextures[rng.Next(0, WildCardMod.floaterTextures.Count)];
                this.GetComponentInChildren<ParticleSystemRenderer>().material.mainTexture = chosenTexture;
                this.GetComponentInChildren<ParticleSystemRenderer>().material.SetTexture("_EmissiveColorMap", chosenTexture);
                Debug.Log($"{this.GetComponentInChildren<ParticleSystemRenderer>().material.mainTexture.name}");
            }
        }
        void Update()
        {
            if (this.GetComponentInChildren<PhysicsProp>().isPocketed)
            {
                if (this.GetComponentInChildren<ParticleSystem>().isPlaying)
                {
                    this.GetComponentInChildren<ParticleSystem>().Stop();
                    this.GetComponentInChildren<ParticleSystem>().Clear();
                }
            }
            else
            {
                if (!this.GetComponentInChildren<ParticleSystem>().isPlaying)
                {
                    this.GetComponentInChildren<ParticleSystem>().Emit(1);
                    this.GetComponentInChildren<ParticleSystem>().Play();
                }
            }
        }
    }
}

