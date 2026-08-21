namespace SHIN
{
    /// <summary>
    /// Addressables 주소·라벨 상수. 에셋 Address는 Assets/Addressables/... 경로와 맞춘다.
    /// </summary>
    public static class PublicVariable
    {
        public static class Label
        {
            public const string UI = "UI";
            public const string InGame = "InGame";
            public const string Popup = "Popup";
            public const string Preload = "Preload";
            public const string Character = "Character";
            public const string Portrait = "Portrait";
            public const string Data = "Data";
            public const string Audio = "Audio";
        }

        public static class Address
        {
            public const string InGameHUD = "Assets/Addressables/Prefab/UI/InGameHUD.prefab";
            public const string HandRankPanel = "Assets/Addressables/Prefab/UI/HandRankPanel.prefab";
            public const string BetPopup = "Assets/Addressables/Prefab/UI/BetPopup.prefab";
            public const string CharacterAtlas = "Assets/Addressables/Atlas/Atlas_Char.spriteatlasv2";
            public const string PlayerIcon = "sprite_character_player_icon_001";
            public const string PlayerVs = "sprite_character_player_vs_001";
            public const string InGameAtlas = "Assets/Addressables/Atlas/Atlas_UI_InGame.spriteatlasv2";
            public const string OpponentDataSO = "Assets/Addressables/SO/OpponentDataSO.asset";
            public const string CardItem = "Assets/Addressables/Prefab/UI/CardObject.prefab";
            public const string InGameUI = "Assets/Addressables/Prefab/UI/InGameUI.prefab";
            public const string OpponentSelectUI = "Assets/Addressables/Prefab/UI/OpponentSelectUI/OpponentSelectUI.prefab";
            public const string OpponentSelectItem = "Assets/Addressables/Prefab/UI/OpponentSelectUI/OpponentSelectItem.prefab";
            public const string VersusUI = "Assets/Addressables/Prefab/UI/VersusUI.prefab";
            public const string FadeUI = "Assets/Addressables/Prefab/UI/FadeUI.prefab";
        }
    }
}
