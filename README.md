# 🐟 Upstream (Akıntıya Karşı)

A one-finger mobile river game built with **Unity 6 LTS** (6000.3, URP 2D).
Swim upstream, eat shrimp, grow from Fry to Titan, and devour the sharks that once hunted you.

## 🎮 Gameplay
- **Drag to steer** left and right (keyboard A/D or arrows in the editor)
- **Eat shrimp** → fill the power bar → **Turbo** smashes rocks and logs
- **Dodge** sharks, rocks, bank logs and **hooks** (they look like bait)
- **Grow** through 5 stages: Fry → Young → Adult → Alpha → Titan; big enough and you eat sharks
- **Gold shrimp** heals a heart (or gives power at full health)
- Reaching **Alpha** once unlocks starting every run as **Young**

## ▶ Running it
1. Open the `Upstream` folder in Unity Hub (6000.3.25f1).
2. Open `Assets/_Project/Scenes/Game.unity` and press Play.
   The whole game is built at runtime by `UpstreamGameBootstrap`; balance values come from the
   assets in `Assets/_Project/ScriptableObjects/` (edit them in the Inspector, no code needed).

## 📱 Android build
The project path contains non-ASCII characters (`akıntiya karşi`), which Unity's Android tools
reject. Build through the ASCII junction `C:\Users\Msi\UpstreamBuild` (same folder) or rename the folder.

- **Test APK:** menu *Upstream → 3. Build Android APK*, or
  `Unity.exe -batchmode -quit -projectPath C:/Users/Msi/UpstreamBuild -buildTarget Android -executeMethod Upstream.EditorTools.UpstreamSceneBuilder.BuildAndroidApkCli`
  then `adb install -r Builds/Upstream.apk`
- **Play Store AAB:** *Upstream → 4. Build Android AAB* — needs your upload key, see [docs/KEYSTORE.md](docs/KEYSTORE.md)
- IL2CPP + ARM64, version and icon are applied automatically (`Assets/Editor/ReleaseSettings.cs`).

Close Android Studio / other Gradle builds while building: Unity's Gradle build needs several GB of RAM.

## 🏗️ Architecture
| Folder | Purpose |
|---|---|
| `Core` | GameManager (state), events, SaveSystem (versioned JSON), ObjectPool, Localization, bootstrap |
| `Data` | ScriptableObject configs: game, growth, spawn, difficulty/regions |
| `Mechanics` | Growth, Turbo, Health, Score, Difficulty — rules live here and are unit-tested |
| `Entities` | Food, Gold food, Shark, Rock, Log, Hook (all pooled) |
| `Spawning` | Weighted, fair spawn patterns + manual collision (no physics engine) |
| `Player` / `River` / `Camera` | Input & movement, procedural watercolor visuals, screen fitting |
| `UI` | HUD, menu, game over, pause, settings, toasts, safe area |
| `Audio` | Pooled SFX + procedural synth clips, vibration |
| `Ads` | `IAdsService`, `AdsManager` (consent → init → interstitial policy), AdMob, Mock |

## 🧪 Tests
*Window → General → Test Runner*: EditMode (rules, localization, ads policy, saves) and
PlayMode (boots the real game: menu → play → die → continue → replay, pause, settings, ads, a 25 s unattended run).

## 🌍 Languages
Turkish and English (`Core/LocalizationManager.cs`). The phone language decides on first launch;
players can switch in Settings. Adding a language = one more column in the table.

## 💰 Ads
Runs with `MockAdsService` until the Google Mobile Ads plugin is installed — see [docs/ADMOB.md](docs/ADMOB.md).
Only Google's official **test** ad ids are in code; real ids go in the gitignored `Assets/Resources/ad_config.json`.

## 📄 Release docs
- [docs/KEYSTORE.md](docs/KEYSTORE.md) — creating and using the upload key
- [docs/privacy-policy.html](docs/privacy-policy.html) — privacy policy draft (EN/TR)
- [docs/store-listing.md](docs/store-listing.md) — store texts (EN/TR), name ideas, screenshot plan, data safety answers
- [docs/ADMOB.md](docs/ADMOB.md) — switching from mock to real AdMob

## 📜 License
All rights reserved © 2026
