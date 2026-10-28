using System;
using System.Collections.Generic;

namespace SHIN
{
    /// <summary>
    /// 모드가 로비로 나갈 때 풀어 줄 리소스 묶음 이름.
    /// Addressables 라벨과 별개다. 부팅 때 이미 올라간 에셋은 묶음에 넣지 않는다.
    /// </summary>
    public static class ResourceScopeId
    {
        public const string Poker = "poker";
        public const string Survivors = "survivors";
    }

    public partial class ResourceManager
    {
        /// <summary>주소 핸들을 풀기 직전에 알린다. 캐시를 먼저 비우게 한다.</summary>
        public event Action<string> AssetReleased;

        private string _currentScope;
        private readonly Dictionary<string, ScopeSet> _scopes = new();

        /// <summary>
        /// 이후 새로 로드되는 주소·라벨을 이 묶음에 기록한다.
        /// 이미 보유 중인 핸들은 넣지 않는다.
        /// </summary>
        public void BeginScope(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (!_scopes.ContainsKey(name))
                _scopes[name] = new ScopeSet();

            _currentScope = name;
        }

        /// <summary>묶음에 기록된 주소와 라벨 핸들을 풀어 준다. 인스턴스를 먼저 없앤 뒤에 호출한다.</summary>
        public void ReleaseScope(string name)
        {
            if (string.IsNullOrEmpty(name) || !_scopes.TryGetValue(name, out var scope))
                return;

            if (_currentScope == name)
                _currentScope = null;

            _scopes.Remove(name);

            var addresses = new string[scope.Addresses.Count];
            scope.Addresses.CopyTo(addresses);
            var labels = new string[scope.Labels.Count];
            scope.Labels.CopyTo(labels);

            for (var i = 0; i < addresses.Length; i++)
                Release(addresses[i]);

            for (var i = 0; i < labels.Length; i++)
                ReleaseLabel(labels[i]);
        }

        private void TrackLoadedAsset(string address)
        {
            if (string.IsNullOrEmpty(_currentScope) || string.IsNullOrEmpty(address))
                return;

            if (_scopes.TryGetValue(_currentScope, out var scope))
                scope.Addresses.Add(address);
        }

        private void TrackLoadedLabel(string label)
        {
            if (string.IsNullOrEmpty(_currentScope) || string.IsNullOrEmpty(label))
                return;

            if (_scopes.TryGetValue(_currentScope, out var scope))
                scope.Labels.Add(label);
        }

        private void UntrackAsset(string address)
        {
            foreach (var scope in _scopes.Values)
                scope.Addresses.Remove(address);
        }

        private void UntrackLabel(string label)
        {
            foreach (var scope in _scopes.Values)
                scope.Labels.Remove(label);
        }

        private void NotifyAssetReleased(string address)
        {
            if (string.IsNullOrEmpty(address))
                return;

            AssetReleased?.Invoke(address);
        }

        private sealed class ScopeSet
        {
            public readonly HashSet<string> Addresses = new();
            public readonly HashSet<string> Labels = new();
        }
    }
}
