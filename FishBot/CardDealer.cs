using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using FishBot.Modules;

namespace FishBot
{
    public class Card
    {
        public string CardName;
        public Image CardImage;
    }

    public static class CardDealer
    {
        private static string[] wordMap = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"};


        public static string CardsAssetPath { get; } = "assets/cards";
        public static string CardNameAssetPath { get; } = "assets/CardNames.txt";
        public static string HalfSuitNameAssetPath { get; } = "assets/HalfSuitNames.txt";

        public static List<string> CardNames;
        public static List<string> HalfSuitNames;

        private static readonly Dictionary<string, Card> Cards;

        static CardDealer()
        {
            Cards = new Dictionary<string, Card>();
            HalfSuitNames = new List<string>();

            CardNames = File.ReadAllLines(CardNameAssetPath).ToList();
            HalfSuitNames = File.ReadAllLines(HalfSuitNameAssetPath).ToList();

            foreach (string name in CardNames)
            {
                var c = new Card {CardName = name};
                c.CardImage = new Image($"{CardsAssetPath}/{c.CardName}.png");
                Cards.Add(c.CardName, c);
            }
        }

        public static async Task DealCards()
        {
            var remainingCards = new List<string>(CardNames);
            foreach (string player in GameModule.Players)
                GameModule.PlayerCards.Add(player, new List<Card>());

            var playerDeal = 0;
            while (remainingCards.Count > 0)
            {
                int playerNameIndex = playerDeal % GameModule.Players.Count;
                string playerName = GameModule.Players[playerNameIndex];

                int givenCardIndex = new Random().Next(remainingCards.Count);
                GameModule.PlayerCards[playerName].Add(GetCardByName(remainingCards[givenCardIndex])); // lol what

                remainingCards.RemoveAt(givenCardIndex);
                playerDeal++;
            }

            await SendCards();
        }

        public static async Task SendCards()
        {
            EmbedBuilder builder;
            foreach (var user in GameModule.AuthorUsers)
            {
                builder = new EmbedBuilder();
                string halfSuitCards = "\n";
                for (int i = 0; i < CardNames.Count; i++)
                {
                    //int cardID = CardNames.IndexOf(GameModule.PlayerCards[user.Key][i].CardName);

                    if (i % 6 == 0 && i != 0)
                    {
                        if (halfSuitCards == "\n") continue;
                        builder.AddField(HalfSuitNames[(i / 6) - 1], halfSuitCards);
                        halfSuitCards = "\n";
                    }

                    if (GameModule.PlayerCards[user.Key].Contains(Cards[CardNames[i]]))
                    {
                        Console.WriteLine(i);
                        string cardNameOutput = "";
                        if (CardNames[i].Length == 3)
                            cardNameOutput = ":one::zero:";
                        else if (char.IsNumber(CardNames[i][0]))
                            cardNameOutput += $":{wordMap[int.Parse(CardNames[i][0].ToString())]}:";

                        switch (CardNames[i][0])
                        {
                            case 'J':
                                cardNameOutput += ":regional_indicator_j:";
                                break;
                            case 'Q':
                                cardNameOutput += ":regional_indicator_q:";
                                break;
                            case 'K':
                                cardNameOutput += ":regional_indicator_k:";
                                break;
                            case 'A':
                                cardNameOutput += ":regional_indicator_a:";
                                break;
                        }

                        if (CardNames[i].EndsWith("S"))
                            cardNameOutput += "\u2660";
                        else if(CardNames[i].EndsWith("C"))
                            cardNameOutput += "\u2663";
                        else if (CardNames[i].EndsWith("D"))
                            cardNameOutput += "\u2666";
                        else if (CardNames[i].EndsWith("H"))
                            cardNameOutput += "\u2665";

                        if (CardNames[i] == "J+")
                            cardNameOutput = ":black_joker::heavy_plus_sign:";
                        else if (CardNames[i] == "J-")
                            cardNameOutput = ":black_joker::heavy_minus_sign:";
                        halfSuitCards += cardNameOutput + "\n";
                    }

                }
                await user.Value.SendMessageAsync("", false, builder.Build());
            }
        }

        public static Card GetCardByName(string cardName)
        {
            return Cards[cardName.ToUpper()];
        }
    }
}