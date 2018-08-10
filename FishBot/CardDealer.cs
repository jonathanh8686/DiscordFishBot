using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using static FishBot.Program;

namespace FishBot
{
    public class Card
    {
        public string CardName;
    }

    public static class CardDealer
    {
        private static readonly string[] WordMap =
            {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"};

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
                Cards.Add(c.CardName, c);
            }
        }

        public static string CardNameAssetPath { get; } = "assets/CardNames.txt";
        public static string HalfSuitNameAssetPath { get; } = "assets/HalfSuitNames.txt";

        public static async Task DealCards(IGuild cguild)
        {
            var remainingCards = new List<string>(CardNames);
            foreach (string player in variables[cguild].Players)
                variables[cguild].PlayerCards.Add(player, new List<Card>());

            var playerDeal = 0;
            while (remainingCards.Count > 0)
            {
                int playerNameIndex = playerDeal % variables[cguild].Players.Count;
                string playerName = variables[cguild].Players[playerNameIndex];

                int givenCardIndex = new Random().Next(remainingCards.Count);
                variables[cguild].PlayerCards[playerName]
                    .Add(GetCardByName(remainingCards[givenCardIndex])); // lol what

                remainingCards.RemoveAt(givenCardIndex);
                playerDeal++;
            }

            await SendCards(cguild);
        }

        public static async Task SendCards(IGuild cguild)
        {
            foreach (var user in variables[cguild].AuthorUsers)
            {
                var dmChannel = await user.Value.GetOrCreateDMChannelAsync();
                var dmMessages = dmChannel.GetMessagesAsync().FlattenAsync();
                foreach (var msg in dmMessages.Result)
                    try
                    {
                        if (msg.Content.Contains("*TEMPORARY*")) // maybe make this less hacky
                            await msg.DeleteAsync();
                    }
                    catch (HttpException)
                    {
                        await Log(new LogMessage(LogSeverity.Error, "CardDealer",
                            "HTTP Error when deleting hand information"));
                    }

                var builder = new EmbedBuilder {Title = $"{user.Key}'s hand"};
                var halfSuitCards = "\n";
                var rawNames = "";

                for (var i = 0; i < CardNames.Count; i++)
                {
                    if (i % 6 == 0 && i != 0)
                        if (halfSuitCards != "\n")
                        {
                            builder.AddField(HalfSuitNames[i / 6 - 1], halfSuitCards);
                            halfSuitCards = "\n";
                        }

                    if (!variables[cguild].PlayerCards[user.Key].Contains(Cards[CardNames[i]])) continue;
                    var cardNameOutput = "";

                    rawNames += CardNames[i] + "\n";
                    if (CardNames[i].Length == 3)
                        cardNameOutput = ":one::zero:";
                    else if (char.IsNumber(CardNames[i][0]))
                        cardNameOutput += $":{WordMap[int.Parse(CardNames[i][0].ToString())]}:";

                    if (CardNames[i][0] == 'J')
                        cardNameOutput += ":regional_indicator_j:";
                    else if (CardNames[i][0] == 'Q')
                        cardNameOutput += ":regional_indicator_q:";
                    else if (CardNames[i][0] == 'K')
                        cardNameOutput += ":regional_indicator_k:";
                    else if (CardNames[i][0] == 'A')
                        cardNameOutput += ":regional_indicator_a:";

                    if (CardNames[i].EndsWith("S"))
                        cardNameOutput += "\u2660";
                    else if (CardNames[i].EndsWith("C"))
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

                Console.WriteLine(user.Key);
                Console.WriteLine(rawNames);

                if (halfSuitCards != "\n")
                    builder.AddField(HalfSuitNames[8], halfSuitCards);

                await user.Value.SendMessageAsync("*TEMPORARY*", false, builder.Build());
            }
        }

        public static Card GetCardByName(string cardName)
        {
            return Cards[cardName.ToUpper()];
        }
    }
}