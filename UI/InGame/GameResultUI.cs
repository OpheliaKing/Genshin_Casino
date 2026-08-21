using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 핸드 승/패 결과 이미지. DOTween으로 등장·퇴장.
    /// </summary>
    public class GameResultUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Sprite _winSpriteRef;
        [SerializeField] private Sprite _loseSpriteRef;
        [SerializeField] private float _appearDuration = 0.4f;
        [SerializeField] private float _hideDuration = 0.28f;
        [SerializeField] private float _fromScale = 0.55f;
        [SerializeField] private float _punchScale = 1.12f;

        private Sequence _sequence;
        private Vector3 _restScale = Vector3.one;
        private bool _restCached;
        private bool _completing;

        private static Sprite _winSprite;
        private static Sprite _loseSprite;
        private static bool _spritesReady;
        private static Task _spriteLoadTask;

        private void Awake()
        {
            EnsureRefs();
            CacheRestScale();
            ApplyHiddenVisuals();
        }

        public async Task PlayAsync(bool playerWins, float holdSeconds = 1.35f)
        {
            EnsureRefs();
            CacheRestScale();
            KillSequence(completeAwaiter: false);

            await EnsureSpritesAsync();
            if (this == null)
                return;

            // 인스턴스에 직접 연결된 스프라이트를 우선 사용 (Addressables 아틀라스 누락 대비)
            if (_winSprite == null && _winSpriteRef != null)
                _winSprite = _winSpriteRef;
            if (_loseSprite == null && _loseSpriteRef != null)
                _loseSprite = _loseSpriteRef;

            var sprite = playerWins ? _winSprite : _loseSprite;
            if (_image != null)
            {
                if (sprite == null)
                {
                    Debug.LogWarning($"[GameResultUI] {(playerWins ? "승리" : "패배")} 스프라이트가 없습니다.");
                    return;
                }

                _image.sprite = sprite;
                _image.preserveAspect = true;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (_rect != null)
                _rect.localScale = _restScale * _fromScale;

            ApplyHiddenVisuals();

            GameManager.Instance?.SoundManager?.PlaySe(
                playerWins ? PublicVariable.Address.AnnouncerWin : PublicVariable.Address.AnnouncerLose);

            var tcs = new TaskCompletionSource<bool>();
            _completing = false;
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (_canvasGroup != null)
                _sequence.Append(_canvasGroup.DOFade(1f, _appearDuration).SetEase(Ease.OutQuad));

            if (_rect != null)
            {
                var appear = _rect.DOScale(_restScale * _punchScale, _appearDuration).SetEase(Ease.OutBack);
                if (_canvasGroup != null)
                    _sequence.Join(appear);
                else
                    _sequence.Append(appear);

                _sequence.Append(_rect.DOScale(_restScale, 0.14f).SetEase(Ease.OutQuad));
            }

            _sequence.AppendInterval(Mathf.Max(0.05f, holdSeconds));

            if (_canvasGroup != null)
                _sequence.Append(_canvasGroup.DOFade(0f, _hideDuration).SetEase(Ease.InQuad));

            if (_rect != null)
            {
                var hide = _rect.DOScale(_restScale * 0.82f, _hideDuration).SetEase(Ease.InBack);
                if (_canvasGroup != null)
                    _sequence.Join(hide);
                else
                    _sequence.Append(hide);
            }

            _sequence.OnComplete(() =>
            {
                _completing = true;
                _sequence = null;
                ApplyHiddenVisuals();
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                tcs.TrySetResult(true);
                _completing = false;
            });
            _sequence.OnKill(() =>
            {
                if (_completing)
                    return;
                tcs.TrySetResult(false);
            });

            await tcs.Task;
        }

        public void HideImmediate()
        {
            KillSequence(completeAwaiter: false);
            EnsureRefs();
            ApplyHiddenVisuals();
            if (_rect != null && _restCached)
                _rect.localScale = _restScale;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public static Task PreloadSpritesAsync()
        {
            return EnsureSpritesAsync();
        }

        private void ApplyHiddenVisuals()
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void CacheRestScale()
        {
            if (_restCached || _rect == null)
                return;

            _restScale = _rect.localScale;
            if (_restScale == Vector3.zero)
                _restScale = Vector3.one;
            _restCached = true;
        }

        private void EnsureRefs()
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            if (_image == null)
                _image = GetComponent<Image>();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void KillSequence(bool completeAwaiter)
        {
            if (_sequence == null || !_sequence.IsActive())
            {
                _sequence = null;
                return;
            }

            if (!completeAwaiter)
                _sequence.OnKill(null);

            _sequence.Kill();
            _sequence = null;
        }

        private static Task EnsureSpritesAsync()
        {
            if (_spritesReady && _winSprite != null && _loseSprite != null)
                return Task.CompletedTask;

            if (_spriteLoadTask != null)
                return _spriteLoadTask;

            _spriteLoadTask = LoadSpritesAsync();
            return _spriteLoadTask;
        }

        private static async Task LoadSpritesAsync()
        {
            try
            {
                var resourceManager = GameManager.Instance?.ResourceManager;
                if (resourceManager == null)
                    return;

                var atlas = await resourceManager.LoadAsync<SpriteAtlas>(PublicVariable.Address.InGameAtlas);
                if (atlas == null)
                {
                    Debug.LogError("[GameResultUI] InGameAtlas 로드 실패");
                    return;
                }

                // GetSprite는 (Clone) 이름을 돌려주므로 이름 정규화해서 찾는다.
                _winSprite = ResolveSprite(atlas, PublicVariable.Address.InGameWinSprite);
                _loseSprite = ResolveSprite(atlas, PublicVariable.Address.InGameLoseSprite);

                if (_winSprite == null)
                    Debug.LogWarning($"[GameResultUI] 아틀라스에 승리 스프라이트 없음: {PublicVariable.Address.InGameWinSprite} (프리팹 직접 참조로 폴백 가능)");
                if (_loseSprite == null)
                    Debug.LogWarning($"[GameResultUI] 아틀라스에 패배 스프라이트 없음: {PublicVariable.Address.InGameLoseSprite} (프리팹 직접 참조로 폴백 가능)");

                _spritesReady = true;
            }
            finally
            {
                if (_winSprite == null && _loseSprite == null)
                    _spriteLoadTask = null;
            }
        }

        private static Sprite ResolveSprite(SpriteAtlas atlas, string exactName)
        {
            var exact = atlas.GetSprite(exactName);
            if (exact != null)
                return exact;

            var sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null)
                    continue;

                var name = sprite.name.Replace("(Clone)", string.Empty).Trim();
                if (name == exactName || name.EndsWith(exactName) || name.StartsWith(exactName))
                    return sprite;
            }

            return null;
        }

        private void OnDestroy()
        {
            KillSequence(completeAwaiter: false);
        }
    }
}
