using Unity.Netcode;
using UnityEngine;
namespace LCWildCardMod.Items
{
    public class VulpixieItem : WildCardProp
    {
        [Space(3f)]
        [Header("VulpixieItem")]
        [Space(3f)]
        [SerializeField]
        private Transform meshTransform = null;
        [SerializeField]
        private Vector2 scaleMinMax = new Vector2(0.5f, 1.5f);
        private Vector2Int scaleMinMaxRound = Vector2Int.zero;
        private Vector3 meshScale = Vector3.one;
        private Vector2 ScaleMinMax
        {
            set
            {
                scaleMinMax = value;
                scaleMinMaxRound = new Vector2Int(Mathf.RoundToInt(value.x * 10f), Mathf.RoundToInt(value.y * 10f) + 1);
            }
        }
        public override void Start()
        {
            base.Start();
            ScaleMinMax = scaleMinMax;
        }
        public void RandomizeScale()
        {
            meshScale = meshTransform.localScale;
            if (!IsOwner)
            {
                return;
            }
            SetScale(Vector3.Scale(meshScale, new Vector3(Random.Next(scaleMinMaxRound.x, scaleMinMaxRound.y) , Random.Next(scaleMinMaxRound.x, scaleMinMaxRound.y), Random.Next(scaleMinMaxRound.x, scaleMinMaxRound.y)) * 0.1f));
        }
        public void ResetScale()
        {
            if (!IsOwner)
            {
                return;
            }
            SetScale(meshScale);
        }
        private void SetScale(Vector3 scale, bool networked = true)
        {
            meshTransform.localScale = scale;
            if (!networked)
            {
                return;
            }
            SetScaleRpc(scale);
        }
        [Rpc(SendTo.NotMe)]
        private void SetScaleRpc(Vector3 scale)
        {
            SetScale(scale, false);
        }
    }
}