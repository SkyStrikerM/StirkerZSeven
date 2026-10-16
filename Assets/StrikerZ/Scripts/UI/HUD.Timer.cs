using UnityEngine;
using UnityEngine.UI;

namespace StrikerZ
{
    /// <summary>
    /// 전투 중 선택지 제한 시간 표시 (GameManager.BattleChoiceSeconds, 지나면 자동 선택).
    /// 선택지 창(또는 공격 대상 바) 바로 아래에 붙어 버튼을 가리지 않는다. 3초 이하 = 빨간 급함 표시 + 매초 경고음 (VARCO Sound, timer_tick)
    /// </summary>
    public partial class HUD
    {
        RectTransform choiceTimerRoot;
        Text choiceTimerText;
        Image choiceTimerBg, choiceTimerEdge;
        int choiceTimerLastSec = -1;
        public const int UrgentSeconds = 3;

        /// <summary>남은 시간 표시 (left &lt; 0 = 숨김)</summary>
        public void ChoiceTimer(float left)
        {
            if (left < 0)
            {
                if (choiceTimerRoot != null) choiceTimerRoot.gameObject.SetActive(false);
                choiceTimerLastSec = -1;
                return;
            }
            if (choiceTimerRoot == null)
            {
                choiceTimerBg = UIKit.Img(root, "ChoiceTimer", new Color(0.035f, 0.028f, 0.07f, 0.94f), GameAssets.Rounded, true);
                choiceTimerRoot = choiceTimerBg.rectTransform;
                choiceTimerEdge = UIKit.Img(choiceTimerBg.transform, "Border", new Color(1f, 0.82f, 0.36f, 0.45f), GameAssets.RoundedOutline, true);
                choiceTimerEdge.rectTransform.Fill();
                choiceTimerText = UIKit.Txt(choiceTimerBg.transform, "T", "", 19, UIKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                choiceTimerText.rectTransform.Fill();
            }
            choiceTimerRoot.gameObject.SetActive(true);
            choiceTimerRoot.SetAsLastSibling();
            PlaceChoiceTimer();

            int s = Mathf.CeilToInt(left);
            bool urgent = s <= UrgentSeconds;
            choiceTimerText.text = $"남은 시간 <color={(urgent ? "#ff6a5a" : "#ffd77a")}>{s}초</color>  ·  지나면 자동 선택";
            if (urgent)
            {
                // 급함: 빨간 바탕이 박동 · 글자 살짝 커짐
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                choiceTimerBg.color = Color.Lerp(new Color(0.35f, 0.04f, 0.04f, 0.95f), new Color(0.6f, 0.08f, 0.06f, 0.97f), pulse);
                choiceTimerEdge.color = new Color(1f, 0.45f, 0.4f, 0.7f + 0.3f * pulse);
                choiceTimerRoot.localScale = Vector3.one * (1f + 0.05f * pulse);
                if (s != choiceTimerLastSec) Sfx.Play(s <= 1 ? "timer_tick_last" : "timer_tick", 0.9f);
            }
            else
            {
                choiceTimerBg.color = new Color(0.035f, 0.028f, 0.07f, 0.94f);
                choiceTimerEdge.color = new Color(1f, 0.82f, 0.36f, 0.45f);
                choiceTimerRoot.localScale = Vector3.one;
            }
            choiceTimerLastSec = s;
        }

        /// <summary>떠 있는 선택 UI(선택지 창 · 공격 대상 바) 바로 아래에 붙인다</summary>
        void PlaceChoiceTimer()
        {
            RectTransform anchorTo = null;
            if (choicePanel != null && choicePanel.gameObject.activeInHierarchy) anchorTo = choicePanel;
            else if (targetBar != null && targetBar.gameObject.activeInHierarchy) anchorTo = targetBar;
            choiceTimerRoot.anchorMin = choiceTimerRoot.anchorMax = new Vector2(0.5f, 1);
            choiceTimerRoot.pivot = new Vector2(0.5f, 1);
            choiceTimerRoot.sizeDelta = new Vector2(330, 38);
            if (anchorTo == null) { choiceTimerRoot.anchoredPosition = new Vector2(0, -292); return; }
            // 기준 UI 의 아래 변 (root 좌표) 에서 10px 아래
            var c = new Vector3[4];
            anchorTo.GetWorldCorners(c);
            var rootRt = (RectTransform)root;
            Vector3 bottom = rootRt.InverseTransformPoint(new Vector3((c[0].x + c[3].x) / 2, c[0].y, 0));
            float top = rootRt.rect.height * (1 - rootRt.pivot.y);
            choiceTimerRoot.anchoredPosition = new Vector2(bottom.x, bottom.y - top - 10);
        }
    }
}
