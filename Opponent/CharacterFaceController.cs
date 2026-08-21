using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// 프리팹의 눈/입 Image 슬롯에 OpponentData 표정 루프를 재생합니다.
    /// (구 UIBlinkLoopPlayer: idle 대기 → frames 순회 → 반복)
    /// </summary>
    public class CharacterFaceController : MonoBehaviour
    {
        [SerializeField] private Image _eyeImage;
        [SerializeField] private Image _mouthImage;

        private OpponentData _data;
        private CharacterExpressionType _current = CharacterExpressionType.NORMAL;
        private Coroutine _eyeRoutine;
        private Coroutine _mouthRoutine;
        private bool _bound;
        private bool _hasExpression;

        private void OnEnable()
        {
            if (_bound && _hasExpression)
                RestartLoops();
        }

        private void OnDisable()
        {
            StopLoops();
        }

        public void Bind(OpponentData data)
        {
            _data = data;
            _bound = data != null;
            _hasExpression = false;
            StopLoops();
        }

        public void SetExpression(CharacterExpressionType type, bool forceRestart = false)
        {
            if (!forceRestart && _hasExpression && _current == type)
                return;

            _current = type;
            _hasExpression = true;
            RestartLoops();
        }

        private void RestartLoops()
        {
            StopLoops();

            if (!_bound || !isActiveAndEnabled)
                return;

            if (_data != null && _data.TryGetEyeExpression(_current, out var eyeData))
                _eyeRoutine = StartOrApplyStatic(_eyeImage, eyeData);

            if (_data != null && _data.TryGetMouthExpression(_current, out var mouthData))
                _mouthRoutine = StartOrApplyStatic(_mouthImage, mouthData);
        }

        private Coroutine StartOrApplyStatic(Image target, ExpressionLoopData data)
        {
            if (target == null || data?.frames == null)
                return null;

            var frameCount = CountValidFrames(data);
            if (frameCount <= 0)
                return null;

            // 0~1개: 루프 없이 해당(또는 유일한) 이미지만 적용
            if (frameCount <= 1)
            {
                ApplyRestSprite(target, data);
                return null;
            }

            return StartCoroutine(PlayLoop(target, data));
        }

        private void StopLoops()
        {
            if (_eyeRoutine != null)
            {
                StopCoroutine(_eyeRoutine);
                _eyeRoutine = null;
            }

            if (_mouthRoutine != null)
            {
                StopCoroutine(_mouthRoutine);
                _mouthRoutine = null;
            }
        }

        private static IEnumerator PlayLoop(Image target, ExpressionLoopData data)
        {
            if (target == null || data == null || data.frames == null || CountValidFrames(data) <= 1)
                yield break;

            // 휴식 표정 = 마지막 유효 스프라이트 (보통 뜬 눈)
            ApplyRestSprite(target, data);

            while (true)
            {
                yield return new WaitForSecondsRealtime(data.PickIdleDelay());

                for (var i = 0; i < data.frames.Count; i++)
                {
                    var frame = data.frames[i];
                    if (frame?.sprite == null)
                        continue;

                    target.sprite = frame.sprite;

                    var hold = Mathf.Max(0f, frame.duration);
                    if (hold > 0f)
                        yield return new WaitForSecondsRealtime(hold);
                }

                ApplyRestSprite(target, data);
            }
        }

        private static int CountValidFrames(ExpressionLoopData data)
        {
            if (data?.frames == null)
                return 0;

            var count = 0;
            for (var i = 0; i < data.frames.Count; i++)
            {
                if (data.frames[i]?.sprite != null)
                    count++;
            }

            return count;
        }

        private static void ApplyRestSprite(Image target, ExpressionLoopData data)
        {
            if (target == null || data?.frames == null)
                return;

            for (var i = data.frames.Count - 1; i >= 0; i--)
            {
                var frame = data.frames[i];
                if (frame?.sprite == null)
                    continue;

                target.sprite = frame.sprite;
                return;
            }
        }
    }
}
