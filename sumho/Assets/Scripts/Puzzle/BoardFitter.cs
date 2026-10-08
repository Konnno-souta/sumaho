using UnityEngine;

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// UI 上の「盤面の置き場所」(RectTransform) にぴったり収まるよう、盤面の位置と大きさを合わせる。
    /// 画面サイズ・縦横比・セーフエリアが変わっても、レイアウトは UI 側で決まり盤面が追従する。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class BoardFitter : MonoBehaviour
    {
        [Tooltip("盤面を収める UI の枠（Canvas は Screen Space - Camera）")]
        [SerializeField] RectTransform slot;
        [Tooltip("枠込みの盤面の大きさ（ワールド単位）。board_frame.png 312px ÷ PPU50 = 6.24")]
        [SerializeField] float boardWorldSize = 6.24f;

        readonly Vector3[] _corners = new Vector3[4];

        void Start()
        {
            Canvas.ForceUpdateCanvases();
            Fit();
        }

        void LateUpdate() => Fit();

        public void Fit()
        {
            if (slot == null || boardWorldSize <= 0f) return;
            slot.GetWorldCorners(_corners);
            float w = _corners[2].x - _corners[0].x;
            float h = _corners[2].y - _corners[0].y;
            if (w <= 0f || h <= 0f) return;

            float s = Mathf.Min(w, h) / boardWorldSize;
            Vector3 center = (_corners[0] + _corners[2]) * 0.5f;
            transform.position = new Vector3(center.x, center.y, 0f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
