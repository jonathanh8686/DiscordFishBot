using System.Collections.Generic;
using Discord;

namespace FishBot
{
    internal class DataStorage
    {
        public readonly List<string> CalledHalfSuits = new List<string>();
        public Dictionary<string, IUser> AuthorUsers = new Dictionary<string, IUser>();

        public Dictionary<string, string> TeamDict = new Dictionary<string, string>();

        public int BlueScore;
        public int BlueSurrenderVotes;
        public List<string> BlueSurrenders = new List<string>();

        public List<string> BlueTeam = new List<string>();
        public int RedScore;
        public int RedSurrenderVotes; // maybe add all this to another class
        public List<string> RedSurrenders = new List<string>();

        public List<string> RedTeam = new List<string>();

        public bool GameInProgress;
        public bool GameStart;
        public bool GameClinch;
        public bool NeedsDesignatedPlayer;
        public string Designator = "";

        public Dictionary<string, List<Card>> PlayerCards = new Dictionary<string, List<Card>>();
        public List<string> Players = new List<string>();
        public string PlayerTurn;

        public string AlgebraicNotation = "";


    }
}