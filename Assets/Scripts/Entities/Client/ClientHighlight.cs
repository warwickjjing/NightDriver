using UnityEngine;

namespace NightDriver.Client
{
    /// <summary>
    /// 현재 콜의 활성 손님이고 운행이 끝나기 전일 때만 렌더러에 발광 색을 적용합니다.
    /// URP/Built-in Lit 등 <c>_EmissionColor</c> 프로퍼티가 있는 머티리얼에서 효과가 납니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClientHighlight : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] targetRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private Color highlightEmission = new Color(0.25f, 0.55f, 0.95f, 1f);

        private MaterialPropertyBlock mpb;
        private ClientBehaviour clientBehaviour;

        private void Awake()
        {
            mpb = new MaterialPropertyBlock();
            clientBehaviour = GetComponentInParent<ClientBehaviour>();
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                return;

            var current = ClientRegistry.CurrentClientObject;
            bool isThisClient = current != null
                && (current == gameObject
                    || transform.IsChildOf(current.transform));

            bool show = isThisClient
                && (clientBehaviour == null || !clientBehaviour.TripFinished);

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var r = targetRenderers[i];
                if (r == null) continue;
                var mat = r.sharedMaterial;
                if (mat == null || !mat.HasProperty(EmissionColorId))
                    continue;

                r.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionColorId, show ? highlightEmission : Color.black);
                r.SetPropertyBlock(mpb);
            }
        }

        private void OnDisable()
        {
            if (targetRenderers == null || mpb == null)
                return;
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var r = targetRenderers[i];
                if (r == null) continue;
                var mat = r.sharedMaterial;
                if (mat == null || !mat.HasProperty(EmissionColorId))
                    continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionColorId, Color.black);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
