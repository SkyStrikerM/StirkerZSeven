using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StrikerZ
{
    /// <summary>카드 칸 입력: 호버 / 우클릭 / 벗어남 / 더블클릭 (스크롤·드래그는 막지 않는다)</summary>
    public class CellEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action onEnter, onExit, onRightClick, onLeftClick, onDoubleClick;
        /// <summary>한 번만 클릭하고 더블클릭 대기 시간(0.35초)이 지나면 호출 (더블클릭이면 호출 안 함)</summary>
        public Action onSingleClick;
        float lastClick = -1f;
        Coroutine pendingSingle;
        public void OnPointerEnter(PointerEventData e) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData e) => onExit?.Invoke();
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right) { onRightClick?.Invoke(); return; }
            if (e.button != PointerEventData.InputButton.Left || e.dragging) return;
            // 더블클릭: 0.35초 안에 두 번 (첫 클릭은 선택만)
            bool dbl = onDoubleClick != null && Time.unscaledTime - lastClick < 0.35f;
            lastClick = dbl ? -1f : Time.unscaledTime;
            if (pendingSingle != null) { StopCoroutine(pendingSingle); pendingSingle = null; }
            if (dbl) onDoubleClick();
            else
            {
                onLeftClick?.Invoke();
                if (onSingleClick != null) pendingSingle = StartCoroutine(SingleAfterWindow());
            }
        }

        System.Collections.IEnumerator SingleAfterWindow()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            pendingSingle = null;
            onSingleClick?.Invoke();
        }

        void OnDisable() => pendingSingle = null;
    }
}
