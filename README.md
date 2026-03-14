# VocabTrainer

> Desktop flashcard app for learning vocabulary — spaced repetition,
> four training modes, bilingual UI (EN/UA), dark and light themes.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)
![Language](https://img.shields.io/badge/language-C%23%20%2F%20WPF-orange)

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Getting Started](#getting-started)
- [Training Modes](#training-modes)
- [SM-2 Spaced Repetition](#sm-2-spaced-repetition)
- [Word Format & CSV Import](#word-format--csv-import)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Licence](#licence)

---

## Overview

VocabTrainer is a local-first WPF application for learning vocabulary. It implements the **SuperMemo SM-2** spaced repetition algorithm — words you struggle with appear more often, words you know well are shown less frequently. Everything is stored in a local SQLite database; no account or internet connection required.

---

## Features

### Core
- **SM-2 spaced repetition** — automatic scheduling based on your performance
- **4 training modes** — Flashcard, Multiple Choice, Text Input, Mixed
- **Multi-variant translations** — separate answers with `/` (e.g. `go / walk`, `дім / будинок`)
- **Streak tracking** — counts consecutive days you have practiced

### Word Management
- Add, edit, and delete words via an inline side panel
- Bulk operations: checkbox selection, select all / clear / invert, delete selected
- Search, filter by tag, sort by German / Next Review / Review Count / Success Rate
- Export to **CSV** or **Excel (.xlsx)**

### Statistics
- KPI cards: Total Words, Learned, Accuracy %, Streak
- Compact metrics: Due Today, Total Reviews, Not Started
- Vocabulary progress bar — learned / in progress / not started
- Daily activity per month

### Import
- Import from **CSV** or **Excel (.xlsx)** with configurable column mapping
- Preview before committing, duplicate detection

### UI & Customisation
- Full **EN / UA** bilingual interface — switch instantly in Settings
- **Dark** and **Light** themes
- Configurable: question/answer language pair, words per session (5–50),
  typo tolerance (0–50%), countdown timer, Text-to-Speech

---

## Getting Started

### Requirements
- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or JetBrains Rider

### Run

```bash
git clone https://github.com/kranel-argonavt/VocabTrainer.git
cd VocabTrainer
dotnet run --project VocabTrainer
```

On first launch the app creates `vocab.db` automatically and seeds it with sample German words. No manual database setup needed.

### Publish a self-contained executable

```bash
dotnet publish VocabTrainer -c Release -r win-x64 --self-contained true
```

---

## Training Modes

| Mode | How it works |
|---|---|
| **Flashcard** | The word is shown. You think of the answer, reveal it, then mark yourself as knew it / didn't know. |
| **Multiple Choice** | Four options are shown — pick the correct translation. |
| **Text Input** | Type the translation. Levenshtein distance tolerance lets minor typos pass. |
| **Mixed** | Each card is randomly assigned one of the three modes above. |

Before every session you choose the mode and the number of words (5–50).

---

## SM-2 Spaced Repetition

Each word card stores:

| Field | Description |
|---|---|
| `EaseFactor` | Controls how fast the interval grows. Starts at 2.5, adjusted after every review (min 1.3) |
| `IntervalDays` | Days until next review |
| `ReviewCount` | Total number of reviews |
| `CorrectAnswers` / `WrongAnswers` | Raw counts used to compute `SuccessRate` |
| `NextReview` | Scheduled review date |

**Interval progression (correct answers):**
- After review 1 → **1 day**
- After review 2 → **6 days**
- After review N → `previousInterval × EaseFactor`

**On a wrong answer:** interval resets to **1 day**, EaseFactor drops by 0.2.

A word is considered **Learned** when `SuccessRate ≥ 80%` and `ReviewCount ≥ 5`.

---

## Word Format & CSV Import

### Fields

| Field | Required | Notes |
|---|---|---|
| German | | The word or phrase to learn |
| English | | One or more translations |
| Ukrainian | | One or more translations |
| Example | | Example sentence in German |
| Tags | | Comma-separated, e.g. `nouns,A1` |

### Multiple accepted answers

Separate variants with `/`. During training, any variant is accepted as correct:

```
English:    go / walk / stroll
Ukrainian:  йти / ходити
```

### Example CSV

```csv
German,English,Ukrainian,Example,Tags
Haus,house,будинок,"Das Haus ist groß.",nouns
gehen,"go / walk","йти / ходити","Ich gehe nach Hause.",verbs
schön,"beautiful / lovely","гарний / чудовий","Das ist sehr schön.",adjectives
```

The column separator is configurable in the Import screen.

---

## Architecture

The project uses **MVVM** with strict layer separation and dependency inversion:

```
VocabTrainer/
├── Core/                        # Domain — no dependencies on UI or infrastructure
│   ├── Entities/                # WordCard, AppSettings, SessionStats, GlobalStats
│   ├── Interfaces/              # IWordCardRepository, ISettingsRepository,
│   │                            # ITrainingService, IStatisticsService, IImportService
│   └── Algorithms/              # Sm2Algorithm (ISpacedRepetitionService)
│
├── Infrastructure/              # Data access
│   ├── Data/                    # VocabDbContext (EF Core), DatabaseSeeder
│   ├── Repositories/            # WordCardRepository, JsonSettingsRepository
│   └── Services/                # TrainingService, StatisticsService, ImportService
│
├── Application/                 # ViewModels — CommunityToolkit.Mvvm
│   └── ViewModels/              # MainViewModel, TrainingViewModel,
│                                # WordManagementViewModel, StatisticsViewModel,
│                                # SettingsViewModel, ImportViewModel
│
├── Presentation/                # Pure XAML — no business logic
│   ├── Views/                   # One .xaml per screen
│   ├── Themes/                  # DarkTheme.xaml, LightTheme.xaml, Styles.xaml
│   └── Converters/              # BoolToVisibility, NullToVisibility,
│                                # DifficultyToColor, MultipleChoiceColor
│
└── Common/                      # Cross-cutting
    ├── Strings.cs               # Centralised string key constants
    ├── LocalizationService.cs   # EN and UA translation dictionaries
    └── LocExtension.cs          # {loc:Loc Key} XAML markup extension
```

---

## Tech Stack

| Library | Version | Purpose |
|---|---|---|
| .NET 8 / WPF | 8.0 | UI framework |
| Entity Framework Core + SQLite | 8.0.0 | Local database and ORM |
| CommunityToolkit.Mvvm | 8.2.2 | `[ObservableProperty]` and `[RelayCommand]` source generators |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | Dependency injection container |
| ClosedXML | 0.102.1 | Excel (.xlsx) import and export |
| System.Text.Json | 8.0.0 | Settings persistence (JSON) |

---

## Licence

MIT — free to use, modify, and distribute. See [LICENSE](LICENSE) for the full text.
