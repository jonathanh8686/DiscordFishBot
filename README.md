# FishBot
### By: Jonathan Hsieh

## About
A simple (quickly put together) bot to allow *intellectuals* to play Canadian Fish (Literature) on [Discord](https://discordapp.com/) Bot is pretty shit so there are going to be tons of bugs and potentially game breaking ones. Tell me if you find any.

## How to Use:
### Help Module
`.help [command]` This command gives the details about a single command, an overview of all commands can be found by leaving the `[command]` field empty.

### Pre-Game Setup
Use the team module to create teams. (As of now, teams do not need to be balanced - however this may lead to unforeseen bugs). Start all commands relating to team creation with the `.team` prefix.

#### Commands
`.team add [username] [team]` - **Allows you to add a username to a team.** This username is what everyone in the game will be referring to you as for the rest of the game. It is recommended that you pick something easy to type as it will be typed over and over again. You also cannot have a username containing a space or the name of a card. The only available teams are `Red` and `Blue`.

`.team remove [username]` - **Removes a player from whatever team he/she is on.**

`.claim [username]` - **Links user of the command's discord account to whatever username they enter.** Because FishBot will DM you your hand, it needs to link your discord account to the username you entered. This command links the two together.

`.unclaim` - **Removes links made by user**

`.team list [team name]` - **Lists users on a given team**

### In-Game Commands

#### Card Naming Conventions
All cards are referred to by two identifiers, the value and suit. Most cards (excluding the Tens) are represented by 2 digit codes. For example the `Ace of Spades` is referred to as `AS`. The rest of the high spades look like `KS, QS, JS, 10S, 9S`. The same reasoning applies for the rest of the suits, except replacing the  `S` with the first character of another suit. For example, `Two of Clubs` becomes `2C` and `Four of Diamonds` is `4D`. **Jokers are represented by a capital `J` followed by a `+` or `-` depending on Big/Little joker! Ex. `Big Joker` is `J+`**

#### HalfSuit Naming Conventions
Halfsuits are referred to as their suit and whether they are the high or low set. For example, the high spades is referred to as `S+` while the low spades are `S-`. Similarly, high and low diamonds are `D+` and `D-` respectively. **The only exception to this rule is the Jokers/Eight halfsuit. This halfsuit is referred to as `J8`**

#### Commands
`.start` - **Beings the game** After all players have joined a team and claimed a username, use this command to begin the game.

`.call [username] [card name]` - **Allows a user to call a card from another user.** This command takes in a username and a card name. Card naming conventions are stated above.

`.callhs [halfsuit] [callString]` - **Allows a user to call a halfsuit** **_This command is the most complex and easiest to mess up_** The call string should consist of usernames followed by the cards that they have. For example if you, "optimalplayer" decided to call the high spades because you have all of them, the callString looks like `"optimalplayer AS KS QS JS 10S 9S"` *The order of the cards does not matter.* All cards and usernames are case-sensitive. If instead of having all the cards, your teammate, "feedingretard" has the `QS and 10S` then the callString should look like `"ptimalplayer AS KS JS 9S feedingretard QS 10S"`

`.score` - **Displays the current score**

`.cardcount [username]` - **Gives the number of cards a user has**

`.designate [username]` - **Changes the turn to another player** In the case that you have run out of cards during your turn, you will be prompted to designate the next player. You may choose anyone in the game to continue.

`.reset` - **Resets the entire game** Do not use during game unless you want to lose all data of the game. Should really only be used if the bot breaks (and it will I promise)
