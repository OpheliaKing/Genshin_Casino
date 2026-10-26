using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 아이템 획득 문구. 플레이어 위에서 DOTween으로 상승·페이드.
    /// </summary>
    public class SurvivorsRunItemAcquirePopup : MonoBehaviour
    {
        private const float RiseDistance = 1.15f;
        private const float Duration = 1.15f;
        private const float SpawnYOffset = 0.85f;

        private static readonly Color PopupColor = new(0.55f, 1f, 0.85f, 1f);

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

            SurvivorsRunUiBuild.ApplyKoreanFont(tmp);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4.2f;
            tmp.color = PopupColor;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 1.4f);
            tmp.sortingOrder = 240;
        }

        public void Play(
            SurvivorsRunManager manager,
            Vector3 worldPosition,
            string message,
            string poolKey)
        {
            EnsureBuilt();
            KillTween();

            _manager = manager;
            _poolKey = poolKey;
            _playing = true;

            transform.position = worldPosition + Vector3.up * SpawnYOffset;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.85f;

            _label.text = string.IsNullOrWhiteSpace(message) ? "아이템 획득" : message.Trim();
            var color = PopupColor;
            color.a = 1f;
            _label.color = color;
            _label.alpha = 1f;

            var start = transform.position;
            var end = start + Vector3.up * RiseDistance;

            _sequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
                .Append(transform.DOScale(1.05f, 0.18f).SetEase(Ease.OutBack))
                .Join(transform.DOMove(end, Duration).SetEase(Ease.OutCubic))
                .Insert(Duration * 0.35f, DOTween.To(
                        () => _label.alpha,
                        a => _label.alpha = a,
                        0f,
                        Duration * 0.65f)
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
