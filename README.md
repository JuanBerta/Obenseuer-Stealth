# Stealth

A new **Stealth** skill for Obenseuer. Get away with things unseen to level it up, and get harder to notice as you go.

## Features
- **A real skill on the Character tab.** It looks like the game's own skills, starts unlearned, and shows the "New skill learned" and level-up popups.
- **XP for getting away with it:**
  - taking things that aren't yours without anyone seeing (valuable things give more)
  - picking locks unseen (harder locks give more)
  - spending time unseen in places you're not allowed to be (capped per area per day)
- **Harder to notice.** People and cameras are less likely to spot you: 5% less per level, 50% at level 10. People's sight range also gets shorter (30% shorter at level 10).
- **Crouching** and **darkness** (outdoors at night, without a light) make you even harder to notice.
- **Your state matters.** Being drunk makes you clumsy and easier to spot, but getting away with it drunk gives extra XP. Your Drinking skill shrinks the penalty, and mushrooms change your effective level.
- **Level 10 perk, Shadow:** when you do get caught for theft, burglary or trespassing, the crime counts as 30% less serious.
- The Stealth card shows how much less likely you are to be seen right now.
- XP is saved with your save game and belongs to that character only.

Every number can be changed in the config file or with ConfigurationManager.

## Install
1. Install BepInEx 6 (Unity.Mono, x64) for Obenseuer.
2. Put `Obenseuer-Stealth.dll` in `BepInEx\plugins\Obenseuer-Stealth\`.

## Build
`dotnet build -c Release` (set `/p:ObenseuerDir=...` if the game is not in the default Steam folder).

Made by JuanBerta, with AI assistance (Claude).
