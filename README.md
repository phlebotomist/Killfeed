![](logo.png)

# PvPBreakdown- Leaderboard, Killfeed, and post fight breakdowns

## Features

- Announce PvP kills and assists in chat
- PvP Leaderboard with Kills/Deaths/Assists
- Track and annunce kill streaks
- Announce max level used in the fight (works with weapon swapping)
- Announce PVP kill steals by mobs
- Discord webhook integration for making kill announcements
- Full post fight breakdown of who hit who with what abilities (webhook only)
- Post fight summary giving summarized breakdown of damage taken in fight (webhook only)

## RoadMap

- More detailed death messages when units kill steal
- Specific message when sun kill steals
- Option to give assist, kill, or neither when units kill steal
- Reward system that can be set by admins for hitting pvp benchmarks
- Custom Chat messaging when gear scrore gap is high
- Gank (at VBloods) detection and custom messaging in chat when players die to ganks
- Get clan based stats
- Record healing as well as damage values
- collect stats on sieges
- Keep stats on overall damage done

## Known Issues

- Player's dying to sun, silver, or unstuck do not go towards kills or kill steals at the moment.

## Setttings

This is a preview of the settings file, the plugin will generate a file with the default values when run.

```ini
[General]

## Announce kills in chat
# Setting type: Boolean
# Default value: true
AnnounceKills = true

## Minimum killstreak count that must be lost to be announced.
# Setting type: Int32
# Default value: 3
AnnounceKillstreakLostMinimum = 3

## Announce killstreaks in chat
# Setting type: Boolean
# Default value: true
AnnounceKillstreak = true

## Include player gear levels in announcements.
# Setting type: Boolean
# Default value: true
IncludeLevel = true

## Wether or not to send messages to the discord webhook (web hook needs to be set up)
# Setting type: Boolean
# Default value: true
UseDiscordWebhook = true

## The level of detail to go into for death recaps
# Setting type: Int32
# Default value: 2
CombatBreakdownDetail = 2
```

# credits and thanks:

This mod is based on Deca's [Killfeed](https://thunderstore.io/c/v-rising/p/deca/Killfeed/)
Want to give thanks to Deca for all his work making kf, bloodstone, and vfc. Additionally everyone in the VrisingModing discord who helped answer questions.

# Support:

- I go by `Morphine` on the Vrising modding [Modding VRising Mod ](]https://vrisingmods.com/discord)
- Additionally feel free to open issues on the github

# Pull Requests:

- It is highly encouraged you open an issue before putting in the work to make a pull request.
  That being said I'm open to looking and reviewing at the time of writing this so feel free to open suggestions or bug reports.
- I will try to be quick to respond
