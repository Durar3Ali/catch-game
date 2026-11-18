# Heart Catch Game

### [**Play the Game Live in Your Browser**](https://Durar3Ali.github.io/catch-game/) 

A 2D catching game inspired by the animated fantasy-comedy series "My Little Pony" look and feel. Guide the unicorn to catch the falling hearts, stars and cupcakes, while avoiding the grumpy storm clouds. 

## Credits

Made as a course project for *Game Design and Development (CCS 3371)*, King Khalid University.


## Features

- Main menu with Play, How to Play and Quit, plus an animated loading screen (async scene loading).
- Falling items with random speed and spin, spawned by a weighted spawner that gets faster as you play.
- Animator-driven pony with Idle, Happy and Ouch states, plus sparkle and confetti particle effects.
- Score pill, progress bar, 3 heart lives, and Win / Lose panels with Restart and Menu buttons.
- Hand-drawn vector art, drifting clouds, floating decorations, and generated sound effects.

## Tech Stack

- **Engine:** Unity 6 (6000.6.0f1), 2D Universal Render Pipeline
- **Language:** C# (`Assets/Scripts`)
- **Systems:** Rigidbody2D + trigger colliders, Animator, ParticleSystem, uGUI, Input System, scene management
- **Hosting:** WebGL build served from `/docs` with GitHub Pages

### Scripts

| Script | What it does |
| --- | --- |
| `PonyController` | Keyboard/mouse/touch movement, flip, catching and hit reactions |
| `FallingItem` | Item type, points, gravity, spin and cleanup |
| `Spawner` | Weighted random spawning that speeds up over time |
| `GameManager` | Score, lives, UI, win/lose logic, restart and menu |
| `MenuController` | Menu buttons, how-to panel, loading screen |
| `Floater`, `Spinner`, `CloudDrift`, `ButtonPop` | Small polish effects (bobbing, spinning, drifting, button feedback) |

## Run It Locally
1. Install Unity **6000.6.0f1** (via Unity Hub).
2. Clone this repo and open the folder as a Unity project.
3. Open `Assets/Scenes/Menu.unity` and press Play.
   (If scenes are ever missing, use the menu **Pony Game > Build Everything**.)

To rebuild the web version: **Pony Game > Build WebGL (docs)**, then commit the `docs/` folder.

