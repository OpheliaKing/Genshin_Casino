using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 처치 보상 칩 플로팅 텍스트. DOTween으로 상승·페이드 후 풀 반납.
    /// SurvivorsRun 커스텀 TimeScale을 트윈 timeScale에 동기화한다.
    /// </summary>
    public class SurvivorsRunGoldPopup : MonoBehaviour
    {
        private const float RiseDistance = 0.9f;
        private const float Duration = 0.75f;
        private const float SpawnYOffset = 0.4f;
        private const float JitterX = 0.22f;

        private static readonly Color GoldColor = new(1f, 0.92f, 0.35f, 1f);

        [SerializeField] private TextMeshPro _label;

        private SurvivorsRunManager _manager;
        private string _poolKey;
        private Sequence _sequence;
        private bool _playing;

        public void EnsureBuilt()
        {
            if (_label != null)
                return;

            _label = GetComponent<TextMeshPro>();
            if (_label == null)
                _label = gameObject.AddComponent<TextMeshPro>();

            ConfigureLabel(_label);
        }

        public static void ConfigureLabel(TextMeshPro tmp)
        {
            if (tmp == null)
                return;

            var font = TMP_Settings.defaultFontAsset;
            if (font != null)
            {
                tmp.font = font;
                if (font.material != null)
                    tmp.fontSharedMaterial = font.material;
            }

            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 3.5f;
            tmp.color = GoldColor;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1.2f);
            tmp.sortingOrder = 220;
        }

        public void Play(
            SurvivorsRunManager manager,
            Vector3 worldPosition,
            int amount,
            string poolKey)
        {
            EnsureBuilt();
            KillTween();

            _manager = manager;
            _poolKey = poolKey;
            _playing = true;

            var jitterX = Random.Range(-JitterX, JitterX);
            transform.position = worldPosition + new Vector3(jitterX, SpawnYOffset, 0f);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            _label.text = $"+{Mathf.Max(0, amount)}";
            var color = GoldColor;
            color.a = 1f;
            _label.color = color;
            _label.alpha = 1f;

            var start = transform.position;
            var end = start + Vector3.up * RiseDistance;

            _sequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                .Append(transform.DOMove(end, Duration).SetEase(Ease.OutCubic))
                .Join(DOTween.To(
                        () => _label.alpha,
                        a => _label.alpha = a,
                        0f,
                        Duration)
                    .SetEase(Ease.InQuad))
                .OnComplete(Despawn);

            if (_manager != null)
                _sequence.timeScale = _manager.TimeScale;
        }

        private void Update()
        {
            if (!_playing || _sequence == null || !_sequence.IsActive())
                return;

            _sequence.timeScale = _manager != null ? _manager.TimeScale : 1f;
        }

        private void OnDisable()
        {
            // 풀 반납(비활성) 시 잔여 트윈 정리
            if (_playing)
                KillTween();
            _playing = false;
        }

        private void KillTween()
        {
            if (_sequence != null && _sequence.IsActive())
                _sequence.Kill();
            _sequence = null;
        }

        private void Despawn()
        {
            _playing = false;
            // OnComplete에서 호출되므로 Kill하지 않고 참조만 끊는다.
            _sequence = null;

            if (_manager != null && !string.IsNullOrEmpty(_poolKey))
            {
                _manager.ReturnPooled(_poolKey, gameObject);
                _poolKey = null;
                return;
            }

            Destroy(gameObject);
        }
    }
}
