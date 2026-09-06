namespace SHIN
{
    /// <summary>인게임 공통 SE 재생.</summary>
    public static class PokerSfx
    {
        public static void PlayCardDraw() => Play(PublicVariable.Address.SeCardDraw);
        public static void PlayCardFlip() => Play(PublicVariable.Address.SeCardFlip);
        public static void PlayCardFlipShowdown() => Play(PublicVariable.Address.SeCardFlipShowdown);
        public static void PlayCardShuffle() => Play(PublicVariable.Address.SeCardShuffle);
        public static void PlayChipBet() => Play(PublicVariable.Address.SeChipBet);
        public static void PlayChipUp() => Play(PublicVariable.Address.SeChipUp);
        public static void PlayPotUp() => Play(PublicVariable.Address.SePotUp);
        public static void PlayUiClick() => Play(PublicVariable.Address.SeUiClick);
        public static void PlayUiShowTurnPopup() => Play(PublicVariable.Address.SeUiShowTurnPopup);
        public static void PlayWin() => Play(PublicVariable.Address.SeWin);
        public static void PlayLose() => Play(PublicVariable.Address.SeLose);
        public static void PlayCharacterDialog() => Play(PublicVariable.Address.SeCharacterDialog);
        public static void PlayHandWin() => Play(PublicVariable.Address.SeHandWin);

        private static void Play(string address)
        {
            if (string.IsNullOrEmpty(address))
                return;

            GameManager.Instance?.SoundManager?.PlaySe(address);
        }
    }
}
