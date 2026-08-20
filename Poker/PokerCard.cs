namespace SHIN
{
    public enum CardSuit
    {
        Spades = 0,
        Hearts = 1,
        Diamonds = 2,
        Clubs = 3
    }

    public enum CardRank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    public readonly struct PokerCard
    {
        public PokerCard(CardRank rank, CardSuit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public CardRank Rank { get; }
        public CardSuit Suit { get; }

        public string RankText => Rank switch
        {
            CardRank.Ace => "A",
            CardRank.King => "K",
            CardRank.Queen => "Q",
            CardRank.Jack => "J",
            CardRank.Ten => "10",
            _ => ((int)Rank).ToString()
        };

        public string SuitSymbol => Suit switch
        {
            CardSuit.Spades => "♠",
            CardSuit.Hearts => "♥",
            CardSuit.Diamonds => "♦",
            _ => "♣"
        };

        public string DisplayName => RankText + SuitSymbol;

        public string CornerText => RankText;
    }
}
