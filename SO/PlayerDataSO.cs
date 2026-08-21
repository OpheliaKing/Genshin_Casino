using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace SHIN
{
    [Serializable]
    public class PlayerData
    {
        public string tid;
        public string name;
        public string description;
        public string atlasAddress;
        public string iconPath;
        public string vsImagePath;
        public int haveGold;

        [Header("Voice (SE)")]
        [Tooltip("상황별 보이스 Addressables 주소. CharacterExpressionType 키")]
        [SerializedDictionary("State", "Voice")]
        public SerializedDictionary<CharacterExpressionType, DialogLines> voices = new();

        public string PickVoice(CharacterExpressionType type)
        {
            if (voices == null || !voices.TryGetValue(type, out var entry) || entry?.lines == null)
                return null;

            return PickRandomLine(entry.lines);
        }

        public void CollectVoiceAddresses(List<string> into)
        {
            if (into == null || voices == null)
                return;

            foreach (var pair in voices)
            {
                var lines = pair.Value?.lines;
                if (lines == null)
                    continue;

                for (var i = 0; i < lines.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                        into.Add(lines[i].Trim());
                }
            }
        }

        private static string PickRandomLine(List<string> lines)
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

    [CreateAssetMenu(fileName = "PlayerDataSO", menuName = "SHIN/Player Data SO")]
    public class PlayerDataSO : ScriptableObject
    {
        [SerializeField] private PlayerData _player = new();

        public PlayerData Player => _player;
    }
}
