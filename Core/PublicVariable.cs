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
            public const string Poker = "Poker";
            public const string Popup = "Popup";
            public const string Preload = "Preload";
            public const string Character = "Character";
            public const string Portrait = "Portrait";
            public const string Data = "Data";
            public const string Audio = "Audio";
            public const string OpponentSelect = "OpponentSelect";
            public const string Survivors = "Survivors";
        }

        public static class Address
        {
            public const string InGameHUD = "Assets/Addressables/Prefab/UI/InGameHUD.prefab";
            public const string HandRankPanel = "Assets/Addressables/Prefab/UI/HandRankPanel.prefab";
            public const string BetPopup = "Assets/Addressables/Prefab/UI/BetPopup.prefab";
            public const string CharacterAtlas = "Assets/Addressables/Atlas/Atlas_Char.spriteatlasv2";
            public const string PlayerDataSO = "Assets/Addressables/SO/PlayerDataSO.asset";
            public const string InGameAtlas = "Assets/Addressables/Atlas/Atlas_UI_InGame.spriteatlasv2";
            public const string InGameWinSprite = "sprite_inGame_win_001";
            public const string InGameLoseSprite = "sprite_inGame_lose_001";
            public const string AnnouncerShowdown = "Assets/Addressables/Audio/Voice/Announcer/se_voice_announcer_showDown_001.mp3";
            public const string AnnouncerWin = "Assets/Addressables/Audio/Voice/Announcer/se_voice_announcer_win_001.mp3";
            public const string AnnouncerLose = "Assets/Addressables/Audio/Voice/Announcer/se_voice_announcer_lose_001.mp3";
            public const string InGameBgm = "Assets/Addressables/Audio/BGM/bgm_inGame_001.mp3";

            public const string SeCardDraw = "Assets/Addressables/Audio/SE/InGame/se_card_draw_001.ogg";
            public const string SeCardFlip = "Assets/Addressables/Audio/SE/InGame/se_card_flip_001.ogg";
            public const string SeCardFlipShowdown = "Assets/Addressables/Audio/SE/InGame/se_card_flip_002.ogg";
            public const string SeCardShuffle = "Assets/Addressables/Audio/SE/InGame/se_card_shuffle_001.ogg";
            public const string SeChipBet = "Assets/Addressables/Audio/SE/InGame/se_chip_bet_001.ogg";
            public const string SeChipUp = "Assets/Addressables/Audio/SE/InGame/se_chip_up_001.wav";
            public const string SePotUp = "Assets/Addressables/Audio/SE/InGame/se_pot_up_001.ogg";
            public const string SeUiClick = "Assets/Addressables/Audio/SE/se_ui_click_001.wav";
            public const string SeUiShowTurnPopup = "Assets/Addressables/Audio/SE/se_ui_show_turn_popup_001.wav";
            public const string SeWin = "Assets/Addressables/Audio/SE/InGame/se_win_001.wav";
            public const string SeLose = "Assets/Addressables/Audio/SE/InGame/se_lose_001.wav";
            public const string SeCharacterDialog = "Assets/Addressables/Audio/SE/InGame/se_chracter_dialog_001.mp3";
            public const string SeHandWin = "Assets/Addressables/Audio/SE/InGame/se_hand_win_001.wav";
            public const string SeVersusStart = "Assets/Addressables/Audio/SE/Versus/se_versus_001.mp3";
            public const string SeVersusClash = "Assets/Addressables/Audio/SE/Versus/se_versus_002.wav";
            public const string OpponentDataSO = "Assets/Addressables/SO/OpponentDataSO.asset";
            public const string CardItem = "Assets/Addressables/Prefab/InGame/Poker/PokerCardObject.prefab";
            public const string InGamePokerUI = "Assets/Addressables/Prefab/InGame/Poker/InGamePokerUI.prefab";
            public const string OpponentSelectUI = "Assets/Addressables/Prefab/UI/OpponentSelectUI/OpponentSelectUI.prefab";
            public const string OpponentSelectItem = "Assets/Addressables/Prefab/UI/OpponentSelectUI/OpponentSelectItem.prefab";
            public const string VersusUI = "Assets/Addressables/Prefab/InGame/Poker/VersusUI.prefab";
            public const string FadeUI = "Assets/Addressables/Prefab/UI/FadeUI.prefab";
            public const string StartUI = "Assets/Addressables/Prefab/UI/StartUI.prefab";
            public const string MainUI = "Assets/Addressables/Prefab/UI/MainUI.prefab";
            public const string SurvivorsRunSession = "Assets/Addressables/Prefab/InGame/SurvivorsRun/SurvivorsRunSession.prefab";
            /// <summary>타이틀 BGM. 에셋 추가 후 GameManager._titleBgmAddress에 이 경로를 넣는다.</summary>
            public const string TitleBgm = "Assets/Addressables/Audio/BGM/bgm_title_001.mp3";
        }
    }
}
