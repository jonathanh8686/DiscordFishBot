﻿using System.Collections.Generic;
using Discord;

namespace FishBot
{
    internal class DataStorage
    {
        public readonly List<string> CalledHalfSuits = new List<string>();
        public Dictionary<string, IUser> AuthorUsers = new Dictionary<string, IUser>();

        public Dictionary<string, string> TeamDict = new Dictionary<string, string>();
        public int BlueScore;
        public List<string> BlueTeam = new List<string>();
        public int RedScore; // it's like it's getting worse
        public List<string> RedTeam = new List<string>();

        public bool GameInProgress; // it's all gone to shit
        public bool GameStart; // lol this is so disgusting
        public bool GameClinch;
        public bool NeedsDesignatedPlayer;

        public Dictionary<string, List<Card>> PlayerCards = new Dictionary<string, List<Card>>();
        public List<string> Players = new List<string>();
        public string PlayerTurn;

        public string AlgebraicNotation;


    }
}