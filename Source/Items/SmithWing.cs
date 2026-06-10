using LCWildCardMod.Utils;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class SmithWing : WildCardProp
    {
        [Space(3f)]
        [Header("SmithWing")]
        [Space(3f)]
        [SerializeField]
        private float speedMultiplier = 1.25f;
        private float inverseSpeedMultiplier = 0f;
        private bool doingSpeed = false;
        private bool wasEnemy = false;
        public override void OnDestroy()
        {
            if (doingSpeed)
            {
                if (wasEnemy)
                {
                    LastEnemyHeldBy.agent.speed *= inverseSpeedMultiplier;
                }
                else
                {
                    LastPlayerHeldBy.MultiplyPlayerSpeed(inverseSpeedMultiplier);
                }
            }
            base.OnDestroy();
        }
        public override void Update()
        {
            base.Update();
            if ((isHeld && !isPocketed) || isHeldByEnemy)
            {
                if (!doingSpeed)
                {
                    inverseSpeedMultiplier = 1f / speedMultiplier;
                    doingSpeed = true;
                    if (isHeld)
                    {
                        LastPlayerHeldBy.MultiplyPlayerSpeed(speedMultiplier);
                        return;
                    }
                    wasEnemy = true;
                    LastEnemyHeldBy.agent.speed *= speedMultiplier;
                    return;
                }
                return;
            }
            if (!doingSpeed)
            {
                return;
            }
            doingSpeed = false;
            if (!wasEnemy)
            {
                LastPlayerHeldBy.MultiplyPlayerSpeed(inverseSpeedMultiplier);
                return;
            }
            wasEnemy = false;
            LastEnemyHeldBy.agent.speed *= inverseSpeedMultiplier;
        }
    }
}