# Overcooked! 2 - Jetpack MOD

Fly in Overcooked! 2!

## Installation

1. Install [BepInEx 5 (x86)](https://github.com/BepInEx/BepInEx/releases) for the game
2. Copy `bin/Release/OC2Jetpack.dll` to the game's `BepInEx/plugins/` folder

> ### Compiling
>
> You may compile the MOD yourself. The following dependencies need to be copied into `lib/` directory: 
>
> - In the game's `Overcooked2_Data/Managed/` directory `Assembly-CSharp.dll`, `UnityEngine.dll`, `UnityEngine.AnimationModule.dll`, `UnityEngine.AudioModule.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.ParticleSystemModule.dll`.
>- In `BepInEx/core/` directory `0Harmony20.dll`, `BepInEx.dll`, `BepInEx.Harmony.dll`.



## How to use

1. Jet key can be set in the game key setup for keyboard / split keyboard. Gamepad players need to set a keyboard key here and map the gamepad button to this keyboard key in `Steam - Library - Overcooked! 2 - Controller Layout`.
- Long press the jet key to lift off, short press the jet key to cruise.
- Clients only work if the host has this MOD installed as well.
- Jet is disabled by default when starting the game, or can be set to disabled by pressing `Esc` during key setup. When disabled it does not change the original game logic.



## Tips

- ⚠️**Do not use in Arcade public games!**
- When enabled, gravity is changed to 1/5 of its original value, and all ceilings are removed, so there are many ways to get airborne without pressing the jet key.
