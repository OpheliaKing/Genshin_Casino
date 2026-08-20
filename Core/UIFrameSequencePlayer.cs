using UnityEngine;
using UnityEngine.UI;

namespace SHIN
{
    /// <summary>
    /// AnimationEvent에서 호출해 UI Image의 sprite를 프레임 인덱스로 바꿉니다.
    /// </summary>
    public class UIFrameSequencePlayer : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite[] _frames; // 0:001, 1:002, 2:003

        private void Reset()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }

        // AnimationEvent에서 호출 (anim clip 내부 functionName과 시그니처 일치 필요)
        public void SetBlinkFrame(int frameIndex)
        {
            if (_image == null || _frames == null || _frames.Length == 0)
                return;

            if (frameIndex < 0 || frameIndex >= _frames.Length)
                return;

            _image.sprite = _frames[frameIndex];
        }
    }
}

