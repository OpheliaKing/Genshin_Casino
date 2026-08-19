using System;
using System.Collections.Generic;

namespace SHIN
{
    public sealed class PokerDeck
    {
        private readonly List<PokerCard> _cards = new(52);
        private readonly System.Random _random;
        private int _index;

        public PokerDeck(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            Reset();
        }

        public void Reset()
        {
            _cards.Clear();
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                for (var rank = CardRank.Two; rank <= CardRank.Ace; rank++)
                    _cards.Add(new PokerCard(rank, suit));
            }

            Shuffle();
            _index = 0;
        }

        public PokerCard Draw()
        {
            if (_index >= _cards.Count)
                throw new InvalidOperationException("[PokerDeck] 덱이 비었습니다.");

            return _cards[_index++];
        }

        private void Shuffle()
        {
            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }
    }
}
