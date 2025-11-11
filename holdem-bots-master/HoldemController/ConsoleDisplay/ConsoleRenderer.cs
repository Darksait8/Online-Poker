using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    public class ConsoleRenderer
    {
        private const int PlayerWidth = 10;
        private const int PlayerHeight = 2; // todo: calculated?
        private const int PotWidth = 17;
        private const int PotHeight = 5;
        private const int MaxMoneyWidth = 10;
        private const int CardWidth = 3;
        private const int HoldCardWidth = CardWidth*2 + 1;
        private const int CardHeight = 3;
        private const int CommunityCardsWidth = CardWidth*5 + 4;
        private const int DealerChipWidth = 3;
        //private const int DealerChipHeight = 1;
        private const int TurnIndicatorWidth = 3;
        //private const int TurnIndicatorHeight = 1;

        private const int ActionWidth = 10;
        private const int ActionHeight = 2;

        private const int HorizontalPadding = 1;
        private const int TableBorderWidth = HorizontalPadding + PlayerWidth + HorizontalPadding;
        private const int VerticalPadding = 1;
        private const int TableBorderHeight = VerticalPadding + PlayerHeight + VerticalPadding;

        private readonly Dictionary<int, PlayerDrawArea> _players = new Dictionary<int, PlayerDrawArea>(8);
        private readonly DrawArea[] _communityCards;
        private readonly DrawArea _pots;

        public ConsoleRenderer(int width, int height, int playerCount)
        {
            Console.SetWindowSize(width, height);
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            if (playerCount > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Too many players. Maximum is 8.");
            }
            var tableWidth = width - (2*TableBorderWidth); // todo: validate min width
            var tableHeight = height - (2*TableBorderHeight); // todo: validate min height

            DrawTable(TableBorderWidth, TableBorderHeight, tableWidth, tableHeight);
            
            var yMid = height / 2 - 1;

            var potCommunityCardPadding = 3;
            var potX = (width - PotWidth - potCommunityCardPadding - CommunityCardsWidth)/2;
            _pots = new DrawArea(potX, yMid, PotWidth, PotHeight, ConsoleColor.DarkGreen);
            _communityCards = GetCardDrawAreas(potX + PotWidth + potCommunityCardPadding, yMid, 5);

            for (var i = 0; i < playerCount; i++)
            {
                int playerY;
                if (i == 0 || i == 4)
                {
                    playerY = yMid;
                }
                else if (i < 4)
                {
                    playerY = VerticalPadding;
                }
                else
                {
                    playerY = height - VerticalPadding - PlayerHeight;
                }
                int playerX;
                if (i == 0)
                {
                    playerX = HorizontalPadding;
                }
                else if (i == 4)
                {
                    playerX = width - HorizontalPadding - PlayerWidth;
                }
                else
                {
                    var playerWidth = tableWidth/4;
                    var leftMost = TableBorderWidth + playerWidth - PlayerWidth/2;
                    playerX = leftMost + (3 - Math.Abs(4 - i))* playerWidth;
                }

                var cardX = playerX;
                var cardY = playerY;
                if (i == 0)
                {
                    cardX = TableBorderWidth + HorizontalPadding;
                }
                else if (i == 4)
                {
                    cardX = width - (TableBorderWidth + HorizontalPadding + HoldCardWidth);
                }
                else if (i < 4)
                {
                    cardY = TableBorderHeight + VerticalPadding;
                }
                else
                {
                    cardY = height - (TableBorderHeight + VerticalPadding + CardHeight);
                }

                var dealerChipX = cardX;
                var dealerChipY = cardY;
                if (i == 0)
                {
                    dealerChipY -= 2;
                    dealerChipX += 4;
                }
                else if (i == 4)
                {
                    dealerChipY += CardHeight + 1;
                }
                else if (i < 4)
                {
                    dealerChipX += HoldCardWidth + 2;
                    dealerChipY += 2;
                }
                else
                {
                    dealerChipX -= DealerChipWidth + 2;
                }

                var actionX = cardX;
                var actionY = cardY;
                if (i == 0)
                {
                    actionX = TableBorderWidth + HoldCardWidth + HorizontalPadding + HorizontalPadding;
                }
                else if (i == 4)
                {
                    actionX = width - (TableBorderWidth + HorizontalPadding + HoldCardWidth + HorizontalPadding + ActionWidth);
                }
                else if (i < 4)
                {
                    actionY = cardY + CardHeight + VerticalPadding;
                }
                else
                {
                    actionY = cardY - (CardHeight + VerticalPadding);
                }

                var turnX = actionX;
                var turnY = actionY;
                if (i == 0)
                {
                    turnX += ActionWidth + HorizontalPadding;
                    turnY++;
                }
                else if (i == 4)
                {
                    turnX -= HorizontalPadding + TurnIndicatorWidth;
                    turnY++;
                }
                else if (i < 4)
                {
                    turnY += ActionHeight;
                }
                else
                {
                    turnY -= 1;
                }

                _players.Add(i, new PlayerDrawArea
                {
                    Name = new DrawArea(playerX, playerY, PlayerWidth, 1, ConsoleColor.Black, ConsoleColor.White),
                    Stack = new DrawArea(playerX, playerY + 1, PlayerWidth, 1, ConsoleColor.Black, ConsoleColor.White),
                    Cards = GetCardDrawAreas(cardX, cardY, 2),
                    BetAction = new DrawArea(actionX, actionY, ActionWidth, 1, ConsoleColor.DarkGreen, ConsoleColor.White),
                    BetAmount = new DrawArea(actionX, actionY + 1, ActionWidth, 1, ConsoleColor.DarkGreen, ConsoleColor.White),
                    ActionIndicator = new DrawArea(turnX, turnY, TurnIndicatorWidth, 1, ConsoleColor.DarkGreen, ConsoleColor.White),
                    DealerChip = new DrawArea(dealerChipX, dealerChipY, DealerChipWidth, 1, ConsoleColor.DarkGreen, ConsoleColor.DarkRed)
                });
            }
        }

        private static DrawArea[] GetCardDrawAreas(int x, int y, int count)
        {
            const int cardDistance = CardWidth + HorizontalPadding;
            return Enumerable.Range(0, count).Select(i => new DrawArea(x + (i*cardDistance), y, CardWidth, CardHeight, ConsoleColor.White)).ToArray();
        }

        private static void DrawTable(int x, int y, int width, int height)
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            var radius = (int)Math.Ceiling(height/2d);
            var lineY = 0;
            while (lineY < radius)
            {
                var lineX = CalculateRoundTableOffset(radius, lineY);
                var line = new string(' ', width - 2*lineX);
                // top
                Console.SetCursorPosition(lineX + x, lineY + y);
                Console.Write(line);
                // bottom
                Console.SetCursorPosition(lineX + x, height - 1 - lineY + y);
                Console.Write(line);
                lineY++;
            }
            Console.ResetColor();
            Console.SetCursorPosition(0, 0);
        }

        private static int CalculateRoundTableOffset(int radius, int y)
        {
            const double horizontalMultiplier = 1.83;
            y -= radius;
            var x = (int) -Math.Ceiling(Math.Sqrt(radius*radius - y*y));
            var actualX = (int) ((x + radius)*horizontalMultiplier);
            return actualX;
        }

        public void UpdatePlayerName(int playerId, string name)
        {
            _players[playerId].Name.Draw(name);
        }

        public void UpdatePlayerStackSize(int playerId, int stackSize)
        {
            _players[playerId].Stack.Draw(stackSize.ToString());
        }

        public void UpdateDealer(int playerId)
        {
            foreach (var player in _players)
            {
                var text = player.Key == playerId ? "(D)" : string.Empty;
                player.Value.DealerChip.Draw(text);
            }
        }

        public void UpdatePlayerCards(int playerId, Card card1, Card card2)
        {
            UpdateCards(_players[playerId].Cards, new[] {card1, card2});
        }

        public void UpdateCommunityCards(Card[] cards)
        {
            UpdateCards(_communityCards, cards);
        }

        private static void UpdateCards(IReadOnlyList<DrawArea> cardAreas, IReadOnlyList<Card> cards)
        {
            for (var i = 0; i < cardAreas.Count; i++)
            {
                Card card;
                if (cards == null || cards.Count <= i || (card = cards[i]) == null)
                {
                    cardAreas[i].DrawLines(CardFormatter.NoCard(), null, ConsoleColor.DarkGreen);
                    continue;
                }
                ConsoleColor cardColor;
                var cardText = CardFormatter.FormatCard(card, out cardColor);
                cardAreas[i].DrawLines(cardText, cardColor);
            }
        }

        public void UpdatePlayerTurn(int? playerId)
        {
            foreach (var player in _players)
            {
                var isTurn = player.Key == playerId;
                var text = isTurn ? "..." : string.Empty;
                var bgColor = isTurn ? ConsoleColor.Black : (ConsoleColor?)null;
                player.Value.ActionIndicator.Draw(text, null, bgColor);
            }
        }

        public void UpdatePlayerAction(int playerId, ActionType? action, int amount)
        {
            var color = ConsoleColor.Green;
            switch (action)
            {
                case ActionType.Check:
                case ActionType.Blind:
                case ActionType.Call:
                    color = ConsoleColor.Cyan;
                    break;
                case ActionType.Raise:
                    color = ConsoleColor.Yellow;
                    break;
                case ActionType.Fold:
                    color = ConsoleColor.Black;
                    break;
                case ActionType.Show:
                    break;
                case ActionType.Win:
                    color = ConsoleColor.Blue;
                    break;
            }
            var player = _players[playerId];

            player.BetAction.Draw(action?.ToString(), color);

            var bet = ((IList) new[] {ActionType.Blind, ActionType.Call, ActionType.Raise, ActionType.Win}).Contains(action) ? amount.ToString() : string.Empty;
            player.BetAmount.Draw(bet);
        }

        public void UpdatePots(IEnumerable<int> pots)
        {
            _pots.DrawLines(pots?.Select((p, i) => $"Pot {i + 1}: {p,MaxMoneyWidth}").ToArray() ?? new string[0]);
        }

        private class PlayerDrawArea
        {
            public DrawArea Name { get; set; }
            public DrawArea Stack { get; set; }
            public DrawArea[] Cards { get; set; }
            public DrawArea BetAction { get; set; }
            public DrawArea BetAmount { get; set; }
            public DrawArea DealerChip { get; set; }
            public DrawArea ActionIndicator { get; set; }
        }
    }
}