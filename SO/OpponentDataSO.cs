using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 표정·대사 공통 상황 키.
    /// </summary>
    public enum CharacterExpressionType
    {
        NORMAL = 0,
        HAPPY = 1,
        WIN = 2,
        LOSE = 3,
        CUSTOM_1 = 4,
        CUSTOM_2 = 5,
        CUSTOM_3 = 6,
        GAME_START = 7,
        HAND_WIN = 8,
        HAND_LOSE = 9,
        TURN_START = 10,
    }

    /// <summary>
    /// 표정 루프 한 프레임. sprite 유지 시간이 끝나면 다음 프레임으로 진행.
    /// </summary>
    [Serializable]
    public class ExpressionSpriteFrame
    {
        public Sprite sprite;
        [Min(0f)] public float duration = 0.03f;
    }

    /// <summary>
    /// UIBlinkLoopPlayer 역할을 데이터로 옮긴 것.
    /// 휴식(마지막 프레임 스프라이트) → min~max 대기 → frames 순회 → 반복.
    /// </summary>
    [Serializable]
    public class ExpressionLoopData
    {
        [Min(0f)] public float min = 2f;
        [Min(0f)] public float max = 4f;
        public List<ExpressionSpriteFrame> frames = new();

        public float PickIdleDelay()
        {
            var lo = Mathf.Min(min, max);
            var hi = Mathf.Max(min, max);
            return UnityEngine.Random.Range(lo, hi);
        }
    }

    [Serializable]
    public class DialogLines
    {
        public List<string> lines = new();
    }

    [Serializable]
    public class OpponentData
    {
        public string tid;
        public string name;
        public string description;
        public string atlasAddress;
        public string spriteName;
        public string modelPath;
        public string iconPath;
        public string vsImagePath;
        public int haveGold;

        [Header("Reaction (Dialog + Face)")]
        [Tooltip("상황별 대사. CharacterExpressionType 키와 표정 딕셔너리와 동일")]
        [SerializedDictionary("State", "Dialog")]
        public SerializedDictionary<CharacterExpressionType, DialogLines> dialogs = new();

        [Tooltip("표정별 눈 스프라이트 루프")]
        [SerializedDictionary("State", "Eye Loop")]
        public SerializedDictionary<CharacterExpressionType, ExpressionLoopData> eyeExpressions = new();

        [Tooltip("표정별 입 스프라이트 루프")]
        [SerializedDictionary("State", "Mouth Loop")]
        public SerializedDictionary<CharacterExpressionType, ExpressionLoopData> mouthExpressions = new();

        public string PickDialog(CharacterExpressionType type)
        {
            if (dialogs == null || !dialogs.TryGetValue(type, out var entry) || entry?.lines == null)
                return null;

            return PickRandomDialog(entry.lines);
        }

        public bool TryGetEyeExpression(CharacterExpressionType type, out ExpressionLoopData data)
        {
            data = null;
            return eyeExpressions != null && eyeExpressions.TryGetValue(type, out data) && data != null;
        }

        public bool TryGetMouthExpression(CharacterExpressionType type, out ExpressionLoopData data)
        {
            data = null;
            return mouthExpressions != null && mouthExpressions.TryGetValue(type, out data) && data != null;
        }

        private static string PickRandomDialog(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return null;

            var valid = 0;
            for (var i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    valid++;
            }

            if (valid <= 0)
                return null;

            var pick = UnityEngine.Random.Range(0, valid);
            for (var i = 0; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                if (pick == 0)
                    return lines[i].Trim();
                pick--;
            }

            return null;
        }
    }

    [CreateAssetMenu(fileName = "OpponentDataSO", menuName = "SHIN/Opponent Data SO")]
    public class OpponentDataSO : ScriptableObject
    {
        [SerializeField] private List<OpponentData> _opponentList = new();

        public IReadOnlyList<OpponentData> OpponentList => _opponentList;

        public OpponentData GetByTid(string tid)
        {
            if (string.IsNullOrEmpty(tid) || _opponentList == null)
                return null;

            for (int i = 0; i < _opponentList.Count; i++)
            {
                var data = _opponentList[i];
                if (data != null && data.tid == tid)
                    return data;
            }

            Debug.LogWarning($"[OpponentDataSO] tid '{tid}'에 해당하는 OpponentData가 없습니다.");
            return null;
        }
    }
}
