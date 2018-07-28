using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace FishBot
{
    class DataStorage
    { // it's all gone to shit
        // lol this is so disgusting
        public bool GameStart;
        public List<string> Players = new List<string>();
        public Dictionary<string, List<Card>> PlayerCards = new Dictionary<string, List<Card>>();
        public Dictionary<string, IUser> AuthorUsers = new Dictionary<string, IUser>();
        public string PlayerTurn;

        public bool GameInProgress;

        public int RedScore;
        public int BlueScore;

        public readonly List<string> CalledHalfSuits = new List<string>();

        public Dictionary<string, string> TeamDict = new Dictionary<string, string>();
        public List<string> RedTeam = new List<string>();
        public List<string> BlueTeam = new List<string>();

    }
}
