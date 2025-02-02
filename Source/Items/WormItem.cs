namespace LCWildCardMod.Items
{
    public class WormItem : ThrowableNoisemaker
    {
        public override void BeginMusic()
        {
            base.BeginMusic();
            if (isCollected == 1)
            {
                triggerAnimator.SetBool("OnFloor", false);
            }
        }
        public override void EquipItem()
        {
            base.EquipItem();
            triggerAnimator.SetBool("IsHeld", true);
            triggerAnimator.SetBool("OnFloor", false);
            FaceDirection("Left");
        }
        public override void PocketItem()
        {
            base.PocketItem();
            triggerAnimator.SetBool("IsHeld", false);
            FaceDirection("Forward");
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            triggerAnimator.SetBool("IsHeld", false);
            FaceDirection("Forward");
        }
        public override void OnHitGround()
        {
            base.OnHitGround();
            triggerAnimator.SetBool("IsThrown", false);
        }
        public override void Throw()
        {
            base.Throw();
            triggerAnimator.SetBool("IsHeld", false);
            triggerAnimator.SetBool("IsThrown", true);
            FaceDirection("Forward");
        }
        public void FaceDirection(string direction)
        {
            switch (direction)
            {
                case "Left":
                    {
                        if (triggerAnimator.GetBool("LookingRight"))
                        {
                            FaceDirection("Forward");
                            triggerAnimator.SetBool("LookingRight", false);
                            itemAnimator.SetTrigger("LookLeft");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingLeft", true);
                        }
                        else
                        {
                            itemAnimator.SetTrigger("LookLeft");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingLeft", true);
                        }
                        break;
                    }
                case "Right":
                    {
                        if (triggerAnimator.GetBool("LookingLeft"))
                        {
                            FaceDirection("Forward");
                            triggerAnimator.SetBool("LookingLeft", false);
                            itemAnimator.SetTrigger("LookRight");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingRight", true);
                        }
                        else
                        {
                            itemAnimator.SetTrigger("LookRight");
                            triggerAnimator.SetBool("LookingForward", false);
                            triggerAnimator.SetBool("LookingRight", true);
                        }
                        break;
                    }
                case "Forward":
                    {
                        if (!triggerAnimator.GetBool("LookingForward"))
                        {
                            itemAnimator.SetTrigger("LookForward");
                            triggerAnimator.SetBool("LookingForward", true);
                            triggerAnimator.SetBool("LookingLeft", false);
                            triggerAnimator.SetBool("LookingRight", false);
                        }
                        break;
                    }
            }
        }
    }
}