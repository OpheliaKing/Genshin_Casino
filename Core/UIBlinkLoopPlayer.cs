using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// UI Image 스프라이트를 순차 교체해 눈 깜빡임 루프를 재생합니다.
    /// Animation/Legacy 설정과 무관하게 동작합니다.
    /// </summary>
    public class UIBlinkLoopPlayer : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _openSprite;      // 001
        [SerializeField] private Sprite _halfCloseSprite; // 002
        [SerializeField] private Sprite _closeSprite;     // 003

        [Header("Blink Timing (seconds)")]
        [SerializeField] private float _idleMin = 2.0f;
        [SerializeField] private float _idleMax = 4.0f;
        [SerializeField] private float _openToHalf = 0.03f;
        [SerializeField] private float _halfToClose = 0.03f;
        [SerializeField] private float _closedHold = 0.04f;
        [SerializeField] private float _closeToHalf = 0.03f;
        [SerializeField] private float _halfToOpen = 0.03f;

        private Coroutine _blinkRoutine;

        private void Reset()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (_image == null)
                _image = GetComponent<Image>();

            if (_image == null)
                return;

            if (_blinkRoutine == null)
                _blinkRoutine = StartCoroutine(BlinkLoop());
        }

        private void OnDisable()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
        }

        private IEnumerator BlinkLoop()
        {
            // 시작은 열린 눈으로 고정
            SetSprite(_openSprite);

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(_idleMin, _idleMax));

                SetSprite(_halfCloseSprite);
                yield return new WaitForSeconds(_openToHalf);

                SetSprite(_closeSprite);
                yield return new WaitForSeconds(_halfToClose + _closedHold);

                SetSprite(_halfCloseSprite);
                yield return new WaitForSeconds(_closeToHalf);

                SetSprite(_openSprite);
                yield return new WaitForSeconds(_halfToOpen);
            }
        }

        private void SetSprite(Sprite next)
        {
            if (_image == null || next == null)
                return;

            _image.sprite = next;
        }
    }
}

