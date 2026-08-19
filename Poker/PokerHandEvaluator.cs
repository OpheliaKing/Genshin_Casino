using System;

namespace SHIN
{
    public enum HandCategory
    {
        HighCard = 0,
        OnePair = 1,
        TwoPair = 2,
        ThreeOfAKind = 3,
        Straight = 4,
        Flush = 5,
        FullHouse = 6,
        FourOfAKind = 7,
        StraightFlush = 8,
        RoyalFlush = 9
    }

    public readonly struct HandScore : IComparable<HandScore>
    {
        public HandScore(HandCategory category, long value)
        {
            Category = category;
            Value = value;
        }

        public HandCategory Category { get; }
        public long Value { get; }

        public int CompareTo(HandScore other) => Value.CompareTo(other.Value);

        public string DisplayName => Category switch
        {
            HandCategory.RoyalFlush => "로열 플러시",
            HandCategory.StraightFlush => "스트레이트 플러시",
            HandCategory.FourOfAKind => "포카드",
            HandCategory.FullHouse => "풀하우스",
            HandCategory.Flush => "플러시",
            HandCategory.Straight => "스트레이트",
            HandCategory.ThreeOfAKind => "트리플",
            HandCategory.TwoPair => "투페어",
            HandCategory.OnePair => "원페어",
            _ => "하이카드"
        };
    }

    public static class PokerHandEvaluator
    {
        public static HandScore Evaluate(PokerCard hole0, PokerCard hole1, PokerCard[] board)
        {
            var cards = new PokerCard[2 + board.Length];
            cards[0] = hole0;
            cards[1] = hole1;
            for (var i = 0; i < board.Length; i++)
                cards[2 + i] = board[i];

            return EvaluateBestFive(cards);
        }

        public static HandScore EvaluateBestFive(PokerCard[] cards)
        {
            if (cards == null || cards.Length < 5)
                return new HandScore(HandCategory.HighCard, 0);

            var best = new HandScore(HandCategory.HighCard, -1);
            var n = cards.Length;
            var combo = new PokerCard[5];

            for (var a = 0; a <= n - 5; a++)
            for (var b = a + 1; b <= n - 4; b++)
            for (var c = b + 1; c <= n - 3; c++)
            for (var d = c + 1; d <= n - 2; d++)
            for (var e = d + 1; e <= n - 1; e++)
            {
                combo[0] = cards[a];
                combo[1] = cards[b];
                combo[2] = cards[c];
                combo[3] = cards[d];
                combo[4] = cards[e];
                var score = EvaluateFive(combo);
                if (score.Value > best.Value)
                    best = score;
            }

            return best;
        }

        private static HandScore EvaluateFive(PokerCard[] cards)
        {
            var rankCount = new int[15];
            var suitCount = new int[4];
            var ranks = new int[5];

            for (var i = 0; i < 5; i++)
            {
                var rank = (int)cards[i].Rank;
                ranks[i] = rank;
                rankCount[rank]++;
                suitCount[(int)cards[i].Suit]++;
            }

            Array.Sort(ranks);
            Array.Reverse(ranks);

            var isFlush = false;
            for (var i = 0; i < 4; i++)
            {
                if (suitCount[i] == 5)
                    isFlush = true;
            }

            var isStraight = IsStraight(ranks, out var straightHigh);

            var fours = 0;
            var threes = 0;
            var pairHigh = 0;
            var pairLow = 0;
            for (var rank = 14; rank >= 2; rank--)
            {
                if (rankCount[rank] == 4)
                    fours = rank;
                else if (rankCount[rank] == 3)
                    threes = rank;
                else if (rankCount[rank] == 2)
                {
                    if (pairHigh == 0)
                        pairHigh = rank;
                    else if (pairLow == 0)
                        pairLow = rank;
                }
            }

            if (isFlush && isStraight)
            {
                var category = straightHigh == 14 ? HandCategory.RoyalFlush : HandCategory.StraightFlush;
                return Pack(category, straightHigh);
            }

            if (fours > 0)
            {
                var kicker = HighestExcept(rankCount, fours);
                return Pack(HandCategory.FourOfAKind, fours, kicker);
            }

            if (threes > 0 && pairHigh > 0)
                return Pack(HandCategory.FullHouse, threes, pairHigh);

            if (isFlush)
                return Pack(HandCategory.Flush, ranks[0], ranks[1], ranks[2], ranks[3], ranks[4]);

            if (isStraight)
                return Pack(HandCategory.Straight, straightHigh);

            if (threes > 0)
            {
                var k1 = HighestExcept(rankCount, threes);
                var k2 = HighestExcept(rankCount, threes, k1);
                return Pack(HandCategory.ThreeOfAKind, threes, k1, k2);
            }

            if (pairHigh > 0 && pairLow > 0)
            {
                var kicker = HighestExcept(rankCount, pairHigh, pairLow);
                return Pack(HandCategory.TwoPair, pairHigh, pairLow, kicker);
            }

            if (pairHigh > 0)
            {
                var k1 = HighestExcept(rankCount, pairHigh);
                var k2 = HighestExcept(rankCount, pairHigh, k1);
                var k3 = HighestExcept(rankCount, pairHigh, k1, k2);
                return Pack(HandCategory.OnePair, pairHigh, k1, k2, k3);
            }

            return Pack(HandCategory.HighCard, ranks[0], ranks[1], ranks[2], ranks[3], ranks[4]);
        }

        private static bool IsStraight(int[] ranksDesc, out int high)
        {
            high = 0;
            var unique = new int[5];
            var count = 0;
            for (var i = 0; i < ranksDesc.Length; i++)
            {
                if (count == 0 || unique[count - 1] != ranksDesc[i])
                    unique[count++] = ranksDesc[i];
            }

            if (count != 5)
                return false;

            if (unique[0] - unique[4] == 4)
            {
                high = unique[0];
                return true;
            }

            if (unique[0] == 14 && unique[1] == 5 && unique[2] == 4 && unique[3] == 3 && unique[4] == 2)
            {
                high = 5;
                return true;
            }

            return false;
        }

        private static int HighestExcept(int[] rankCount, params int[] except)
        {
            for (var rank = 14; rank >= 2; rank--)
            {
                if (rankCount[rank] == 0)
                    continue;

                var skip = false;
                for (var i = 0; i < except.Length; i++)
                {
                    if (except[i] == rank)
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                    return rank;
            }

            return 0;
        }

        private static HandScore Pack(HandCategory category, params int[] keys)
        {
            long value = (long)category << 24;
            for (var i = 0; i < keys.Length && i < 5; i++)
                value |= (long)(keys[i] & 0xF) << (20 - i * 4);

            return new HandScore(category, value);
        }
    }
}
