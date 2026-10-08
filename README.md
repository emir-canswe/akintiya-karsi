# 🐟 Upstream (Akıntıya Karşı)

A mobile river survival game built with **Unity 6 LTS** (URP 2D).

Swim upstream, eat food, grow bigger, and devour the sharks that once hunted you.

## 🎮 Core Gameplay
- **Drag to steer** — guide your fish left and right
- **Eat shrimp** to fill the power bar → **Turbo mode**
- **Dodge** sharks, rocks, logs, and hooks
- **Grow** through 5 stages: Fry → Young → Adult → Alpha → Titan
- At higher stages, **eat the sharks** that used to chase you

## 🏗️ Architecture
| Module | Purpose |
|--------|---------|
| `Core` | GameManager, ObjectPool, SaveSystem, Events |
| `Data` | ScriptableObject configs (balance, growth, spawn, difficulty) |
| `Player` | Input (touch + keyboard), movement, boundary clamping |
| `Mechanics` | Growth (5-stage), Turbo, Health, Score, Difficulty |
| `Entities` | Food, Shark, Rock, Log, Hook — all pooled |
| `Spawning` | Pattern-based spawning, manual collision (no physics engine) |
| `UI` | HUD, GameOver, MainMenu, Toast notifications |
| `Audio` | Pooled AudioSource manager |
| `Ads` | IAdsService interface, MockAds for testing, AdMob stub |

## 🔧 Setup
1. Open **Unity Hub** → New Project → **2D (URP)** template → Unity 6 LTS
2. Set project path to this repo's root
3. Install packages: **Input System**, **Localization**, **TextMeshPro**
4. Player Settings: **IL2CPP**, **ARM64**
5. Create ScriptableObject assets: `Assets > Create > Upstream > ...`

## 📱 Target
- Android (AAB, IL2CPP, ARM64) — 60 FPS on mid-range devices
- iOS ready architecture
- Object pooling throughout — zero GC during gameplay

## 🔒 Ad Safety
- Only Google **test** ad IDs in code
- Real IDs loaded from `ad_config.json` (gitignored)
- See `ad_config.example.json` for format

## 📜 License
All rights reserved © 2026
