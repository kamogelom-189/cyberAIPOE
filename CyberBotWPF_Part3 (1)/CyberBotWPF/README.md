# CyberBot Companion — Part 3 (Task Assistant, Quiz, NLP, Activity Log)

This continues the existing **WPF** chatbot project (`CyberBotWPF`). Nothing from
Part 1/2 was removed — greeting, keyword recognition, dynamic responses,
sentiment detection, conversation memory, and the existing GUI all still work
exactly as before. Part 3 adds four new subsystems on top, integrated into the
same window via a new `TabControl`.

> Note: the original assignment brief mentions "Windows Forms" and `DateTimePicker`,
> but the project you supplied is **WPF**, not WinForms. Per the instructions
> ("do not create a new project, continue from my existing project"), everything
> below was built as WPF using the closest equivalents (`DatePicker`, `DataGrid`
> instead of `DataGridView`, etc.) so your existing code keeps working.

## What's new

| Area | Where |
|---|---|
| Cybersecurity Task Assistant (Add/Update/Delete/Complete/View, MySQL-backed) | **Task Manager** tab |
| Cybersecurity Quiz — 14 questions, 13 topics, MC + True/False, scoring, review | **Quiz** tab |
| Activity Log — every action logged, search/export/clear, last-10 / show-all | **Activity Log** tab |
| MySQL connection settings, test connection, create schema, auto fallback | **Database** tab |
| NLP-style command recognition (many phrasings → same intent) | `Managers/NLPProcessor.cs`, wired into the chat box on the **Chatbot** tab |
| Reminders ("remind me tomorrow" / "in 7 days"), due-reminder popup on launch | `Managers/ReminderManager.cs` |

The chatbot box on the **Chatbot** tab now understands things like:
`add a task to update my password`, `show my tasks`, `mark task #2 done`,
`delete task 3`, `remind me in 7 days`, `start quiz`, `activity log` — in
addition to every original topic (`password`, `phishing`, `vpn`, `2fa`, …).

## Project layout

```
CyberBotWPF/
├── CyberBotWPF.csproj          (added MySqlConnector NuGet package)
├── App.xaml / App.xaml.cs                  (unchanged)
├── AssemblyInfo.cs                         (unchanged)
├── ChatMemory.cs                           (unchanged)
├── ResponseEngine.cs                       (unchanged — original keyword/sentiment engine)
├── SplashWindow.xaml / .xaml.cs            (unchanged)
├── VoiceService.cs                         (unchanged)
├── MainWindow.xaml / .xaml.cs              (extended: TabControl, menu, status bar, all new tabs)
├── Models/
│   ├── TaskModel.cs        — mirrors the Tasks table
│   ├── ActivityModel.cs    — mirrors the ActivityLog table
│   └── Question.cs         — quiz question + QuestionType enum
├── Data/
│   ├── DbConfig.cs         — mutable connection settings (edited from the Database tab)
│   └── DatabaseHelper.cs   — reusable parameterized ADO.NET helper (no raw SQL elsewhere)
├── Managers/
│   ├── TaskManager.cs      — CRUD for Tasks (INSERT/SELECT/UPDATE/DELETE, all parameterized)
│   ├── ActivityLogger.cs   — writes/reads ActivityLog, search, export CSV, clear
│   ├── ReminderManager.cs  — "today"/"tomorrow"/"in N days" parsing + due-reminder check
│   ├── QuizManager.cs      — quiz state machine: order, scoring, review
│   ├── QuizQuestions.cs    — the 14-question bank
│   └── NLPProcessor.cs     — normalization + synonym/intent matching (pure C#, no AI)
└── Database/
    └── schema.sql          — run this once in MySQL (or use the in-app "Initialize Schema" button)
```

## Setup

1. **Install MySQL** (e.g. MySQL Community Server) if you don't already have it running locally.
2. **Create the database** — either:
   - run `Database/schema.sql` in MySQL Workbench / the `mysql` CLI, **or**
   - just launch the app, go to the **Database** tab, set your credentials, and click **Initialize Schema**.
3. **Open the project** in Visual Studio (or `dotnet build` from this folder). The
   `MySqlConnector` NuGet package is already referenced in the `.csproj` and will
   restore automatically.
4. Default connection assumed: `Server=localhost, Port=3306, Database=cyberbot_db, User=root, Password=(blank)`.
   Change these on the **Database** tab — they apply immediately for the session
   (click **Save Settings**, then **Test Connection**).
5. Run the app. If MySQL isn't reachable, **Tasks and the Activity Log
   automatically fall back to in-memory storage** so the app never crashes —
   you'll see "DB: ⚠ Offline (using memory fallback)" in the status bar.

## Notes on requirements coverage

- **SOLID / OOP**: each concern is its own class (`DatabaseHelper`, `TaskManager`,
  `ActivityLogger`, `ReminderManager`, `QuizManager`, `NLPProcessor`) — `MainWindow`
  just wires them together and handles UI events.
- **Error handling**: every DB-facing method is wrapped in try/catch with a
  memory fallback and a friendly status message; empty/duplicate/invalid input
  is validated before hitting the database.
- **No hardcoded SQL string concatenation** — all queries use `MySqlCommand`
  parameters via `DatabaseHelper`.
- `assets/greeting.wav` is **not included** (it wasn't part of the uploaded
  files) — `VoiceService` already handles a missing file gracefully with a
  debug message; drop your own `greeting.wav` into `assets/` to re-enable it.
