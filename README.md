# Breed With Followers

A *Cult of the Lamb* mod that lets the Lamb (or the Goat, in co-op) breed with a follower through the Mating Tent, using the game's own mating flow.

Your character shows up in the tent's normal follower picker. Pick them and a follower, and the rest plays out like a regular follower pairing:

- The follower considers the offer, accepts, and they kiss.
- Both walk into the tent.
- You pick the egg's traits on the vanilla trait screen: the follower's traits plus two random ones.
- The follower comes out carrying the egg, naked as in vanilla, and plants it. The Lamb comes out as they went in.
- Only the follower ends up exhausted.

The egg goes to the Hatchery like any other, and hatches into a normal child that takes after the follower.

## Details

- The odds always show 100% and the mating always succeeds (your followers love you).
- The follower is always the parent. The child inherits the follower's look.
- Your character appears at the top of the picker (shown as level 10, age 25) wearing your current fleece, or as the Goat.
- Costs 1 Sin, same as normal mating.
- With a player as the partner, the tent opens with just one available follower.
- Follower + follower mating is untouched and still uses the game's own code.

## Requirements

- *Cult of the Lamb* with the Sins of the Flesh update (v1.3.0, Mating Tent) or later.
- [BepInExPack_CultOfTheLamb](https://thunderstore.io/c/cult-of-the-lamb/p/BepInEx/BepInExPack_CultOfTheLamb/) 5.4.2101.
- COTL_API is not needed.

## Install

r2modman / Thunderstore Mod Manager: install *BreedWithFollowers* from its [Thunderstore page](https://thunderstore.io/c/cult-of-the-lamb/p/Carmenlings/BreedWithFollowers/).

Manual: install BepInExPack_CultOfTheLamb, then put `BreedWithFollowers.dll` in `BepInEx/plugins/`.

To uninstall, remove the DLL. The mod itself writes nothing to your save. The eggs and children it creates are ordinary game eggs and followers.

## Troubleshooting

The BepInEx log is `BepInEx/LogOutput.log`. Look for `Breed With Followers 2.1.0 loaded`. If a patch fails to apply (for example after a game update), it logs `Patch class ... failed` and the rest of the mod keeps working.

## Compatibility

- Windows. The game is the same on every store.
- Co-op works. The tent adds whichever player opened it, the Lamb or the Goat.
- Mods that change the Mating Tent's menu or the follower picker may conflict.

## Building from source

Requires the .NET SDK. Point the project at your game and your BepInEx profile:

```
dotnet build -c Release -p:GameDir="C:\Path\To\Cult of the Lamb" -p:ProfileDir="C:\Path\To\BepInEx\profile"
```

The project references the game's `Assembly-CSharp.dll`, the Unity assemblies and BepInEx/Harmony from those folders. None of them are included in this repository. A prebuilt `BreedWithFollowers.dll` is in `dist/`.

## Credits

- [BepInEx](https://github.com/BepInEx/BepInEx) and [HarmonyX](https://github.com/BepInEx/HarmonyX).
- The scene is built from the game's own Mating Tent, Hatchery and follower-interaction code, so it reuses vanilla animations and logic.

*Cult of the Lamb* is by Massive Monster. This is an unofficial fan mod, not affiliated with or endorsed by them, and it contains no game files.

## License

[MIT](LICENSE)
