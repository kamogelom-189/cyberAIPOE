using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CyberBotWPF.Data;
using CyberBotWPF.Managers;
using CyberBotWPF.Models;
using Microsoft.Win32;

namespace CyberBotWPF;

public partial class MainWindow : Window
{
    private readonly string _username;
    private readonly ResponseEngine _engine;
    private readonly ChatMemory _memory;

    // ── New subsystems (Part 3) ────────────────────────────────────────────
    private readonly DatabaseHelper _db = new();
    private readonly ActivityLogger _activityLogger;
    private readonly TaskManager _taskManager;
    private readonly ReminderManager _reminderManager;
    private readonly QuizManager _quizManager;
    private readonly NLPProcessor _nlp = new();

    private TaskModel? _selectedTask;
    private int? _pendingTaskCommandId; // tracks "add a task" -> awaiting reminder follow-up in chat

    private static readonly SolidColorBrush BrushBotBubble = new(Color.FromRgb(0x16, 0x1B, 0x22));
    private static readonly SolidColorBrush BrushUserBubble = new(Color.FromRgb(0x1C, 0x43, 0x6B));
    private static readonly SolidColorBrush BrushCyan = new(Color.FromRgb(0x00, 0xE5, 0xFF));
    private static readonly SolidColorBrush BrushYellow = new(Color.FromRgb(0xE3, 0xB3, 0x41));
    private static readonly SolidColorBrush BrushMuted = new(Color.FromRgb(0x8B, 0x94, 0x9E));
    private static readonly SolidColorBrush BrushBorder = new(Color.FromRgb(0x30, 0x36, 0x3D));
    private static readonly SolidColorBrush BrushGreen = new(Color.FromRgb(0x3F, 0xB9, 0x50));
    private static readonly SolidColorBrush BrushRed = new(Color.FromRgb(0xF8, 0x51, 0x49));

    private DispatcherTimer? _typeTimer;
    private string _pendingText = "";
    private int _typeIndex;
    private TextBlock? _typeTarget;

    // Required parameterless constructor fallback preventing WPF runtime launch crashes
    public MainWindow() : this("Guest")
    {
    }

    public MainWindow(string username)
    {
        InitializeComponent();

        _username = username;
        _memory = new ChatMemory(username);
        _engine = new ResponseEngine(_memory);

        _activityLogger = new ActivityLogger(_db);
        _taskManager = new TaskManager(_db, _activityLogger);
        _reminderManager = new ReminderManager(_taskManager);
        _quizManager = new QuizManager(_activityLogger);

        TitleLabel.Text = $"CyberBot  ·  Hi, {username}!";

        TopicButtons.ItemsSource = new[]
        {
            "🔐 Passwords", "🎣 Phishing", "🦠 Malware",
            "🌐 VPN", "🔑 2FA", "💾 Backups",
            "📋 Tips", "❓ Help"
        };

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AddBotMessage($"Welcome back, {_username}! 🛡 I'm CyberBot, your cybersecurity companion.");
        AddBotMessage("Ask me about passwords, phishing, malware, VPNs, 2FA, backups — add/show tasks, start the quiz, or check the activity log. Type 'help' any time.");
        UpdateMemoryPanel();
        InputBox.Focus();

        // Try to verify the schema quietly on launch (non-blocking UX; failures fall back silently).
        _db.InitializeSchema(out _);
        UpdateDbStatusIndicator();
        RefreshTasksGrid();
        RefreshLogGrid();

        // ── Reminder check on startup ───────────────────────────────────────
        var due = _reminderManager.GetDueReminders();
        if (due.Count > 0)
        {
            string list = string.Join("\n", due.Select(t => $"• {t.Title} (reminder: {t.ReminderDate:dd MMM yyyy})"));
            MessageBox.Show(this, $"You have {due.Count} cybersecurity task reminder(s) due or overdue:\n\n{list}",
                "🔔 CyberBot Reminders", MessageBoxButton.OK, MessageBoxImage.Information);
            AddBotMessage($"🔔 Heads up — you have {due.Count} task reminder(s) due today or overdue. Check the Task Manager tab!");
        }

        StatusBarText.Text = "Ready.";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CHATBOT TAB
    // ═══════════════════════════════════════════════════════════════════════

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ProcessInput();
    }

    private void Send_Click(object sender, RoutedEventArgs e) => ProcessInput();

    private void TopicButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            string raw = btn.Content?.ToString() ?? "";
            int spaceIdx = raw.IndexOf(' ');
            InputBox.Text = spaceIdx >= 0 ? raw.Substring(spaceIdx + 1) : raw;
            ProcessInput();
        }
    }

    private void QuickShowTasks_Click(object sender, RoutedEventArgs e)
    {
        InputBox.Text = "show my tasks";
        ProcessInput();
    }

    private void QuickStartQuiz_Click(object sender, RoutedEventArgs e)
    {
        InputBox.Text = "start quiz";
        ProcessInput();
    }

    private void QuickShowLog_Click(object sender, RoutedEventArgs e)
    {
        InputBox.Text = "activity log";
        ProcessInput();
    }

    /// <summary>
    /// Main chat pipeline. Tries NLP intent recognition first (tasks, quiz,
    /// activity log, reminders, broad topic phrasing) before falling back to
    /// the original ResponseEngine for general conversation / sentiment / memory.
    /// </summary>
    private void ProcessInput()
    {
        string text = InputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        InputBox.Clear();
        AddUserMessage(text);

        _activityLogger.Log("Chat Command", text);

        string normalized = NLPProcessor.Normalize(text);
        var intent = _nlp.DetectIntent(normalized);

        if (intent != Intent.None && TryHandleIntent(intent, text))
        {
            _activityLogger.Log("NLP Match", $"\"{text}\" → {intent}");
            UpdateMemoryPanel();
            ScrollToBottom();
            return;
        }

        // Fall back to the original keyword/sentiment chatbot engine.
        var result = _engine.GetResponse(text);

        if (result.IsExit)
        {
            AddBotMessage($"Stay safe out there, {_username}! 🛡 CyberBot signing off.");
            SendButton.IsEnabled = false;
            InputBox.IsEnabled = false;
            return;
        }

        if (result.IsHelp)
        {
            ShowHelp();
            return;
        }

        if (result.SentimentAck is not null)
            AddBotMessage(result.SentimentAck);

        AddBotMessageAnimated(result.Response);

        if (result.Tip is not null)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                AddBotMessage(result.Tip);
            };
            timer.Start();
        }

        UpdateMemoryPanel();
        ScrollToBottom();
    }

    /// <summary>Handles task/quiz/log/reminder intents directly inside the chatbot. Returns true if handled.</summary>
    private bool TryHandleIntent(Intent intent, string rawText)
    {
        try
        {
            switch (intent)
            {
                case Intent.AddTask:
                {
                    string title = NLPProcessor.ExtractTaskTitle(rawText);
                    var task = _taskManager.AddTask(title, "Added via chatbot.", null);
                    _pendingTaskCommandId = task.TaskID;
                    AddBotMessage($"✅ Task added successfully: \"{task.Title}\" (#{task.TaskID}). Would you like to set a reminder? (e.g. 'remind me tomorrow' or 'remind me in 7 days')");
                    RefreshTasksGrid();
                    return true;
                }
                case Intent.ShowTasks:
                {
                    var tasks = _taskManager.GetAllTasks();
                    if (tasks.Count == 0)
                    {
                        AddBotMessage("You have no cybersecurity tasks yet. Try: 'add a task to update my password'.");
                    }
                    else
                    {
                        string list = string.Join("\n", tasks.Take(10).Select(t => t.ToChatSummary()));
                        AddBotMessage($"📋 Your tasks ({tasks.Count} total):\n{list}");
                    }
                    MainTabs.SelectedIndex = 1;
                    return true;
                }
                case Intent.DeleteTask:
                {
                    var match = FindTaskFromText(rawText);
                    if (match is null) { AddBotMessage("I couldn't tell which task to delete — try 'delete task #3' or open the Task Manager tab."); return true; }
                    _taskManager.DeleteTask(match.TaskID);
                    AddBotMessage($"🗑 Deleted task \"{match.Title}\".");
                    RefreshTasksGrid();
                    return true;
                }
                case Intent.CompleteTask:
                {
                    var match = FindTaskFromText(rawText);
                    if (match is null) { AddBotMessage("I couldn't tell which task to mark complete — try 'mark task #3 done' or open the Task Manager tab."); return true; }
                    _taskManager.MarkComplete(match.TaskID);
                    AddBotMessage($"✅ Marked \"{match.Title}\" as completed. Great job staying on top of your security!");
                    RefreshTasksGrid();
                    return true;
                }
                case Intent.SetReminder:
                {
                    var date = ReminderManager.ParseReminderPhrase(rawText);
                    if (date is null) { AddBotMessage("I couldn't parse that reminder date. Try 'remind me tomorrow' or 'remind me in 7 days'."); return true; }

                    TaskModel? target = _pendingTaskCommandId.HasValue
                        ? _taskManager.GetAllTasks().FirstOrDefault(t => t.TaskID == _pendingTaskCommandId.Value)
                        : _taskManager.GetAllTasks().Where(t => !t.IsCompleted).OrderByDescending(t => t.DateCreated).FirstOrDefault();

                    if (target is null) { AddBotMessage("You don't have a task to attach that reminder to yet — add one first."); return true; }

                    target.ReminderDate = date.Value;
                    _taskManager.UpdateTask(target);
                    _activityLogger.Log("Reminder Modified", $"Task #{target.TaskID} reminder set to {date:dd MMM yyyy}");
                    AddBotMessage($"🔔 Reminder set for \"{target.Title}\" on {date:dd MMM yyyy}.");
                    _pendingTaskCommandId = null;
                    RefreshTasksGrid();
                    return true;
                }
                case Intent.StartQuiz:
                {
                    MainTabs.SelectedIndex = 2;
                    StartQuizInternal();
                    AddBotMessage("🧠 Quiz started — head to the Quiz tab to answer!");
                    return true;
                }
                case Intent.ShowActivityLog:
                {
                    var recent = _activityLogger.GetRecent(10);
                    string list = string.Join("\n", recent.Select(a => $"[{a.Timestamp:HH:mm:ss}] {a.Action} — {a.Details}"));
                    AddBotMessage(string.IsNullOrEmpty(list) ? "No activity logged yet." : $"📜 Recent activity:\n{list}");
                    MainTabs.SelectedIndex = 3;
                    return true;
                }
                // Broad topic intents: let the response engine's existing keyword logic answer
                // (it already covers these), but log the NLP match for traceability.
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            AddBotMessage($"⚠ Something went wrong handling that: {ex.Message}");
            _activityLogger.Log("Errors", ex.Message);
            return true;
        }
    }

    private TaskModel? FindTaskFromText(string rawText)
    {
        var match = System.Text.RegularExpressions.Regex.Match(rawText, @"#?(\d+)");
        var tasks = _taskManager.GetAllTasks();
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            return tasks.FirstOrDefault(t => t.TaskID == id);

        // Fall back to the most recently created pending task.
        return tasks.Where(t => !t.IsCompleted).OrderByDescending(t => t.DateCreated).FirstOrDefault();
    }

    private void AddUserMessage(string text)
    {
        var bubble = MakeBubble(text, BrushUserBubble, BrushCyan, $"{_username}", isUser: true);
        ChatPanel.Children.Add(bubble);
        ScrollToBottom();
    }

    private void AddBotMessage(string text)
    {
        var bubble = MakeBubble(text, BrushBotBubble, BrushCyan, "🛡 CyberBot", isUser: false);
        ChatPanel.Children.Add(bubble);
        ScrollToBottom();
    }

    private void AddBotMessageAnimated(string text)
    {
        var (container, tb) = MakeBubbleAnimated(BrushBotBubble, "🛡 CyberBot");
        ChatPanel.Children.Add(container);
        ScrollToBottom();
        StartTyping(tb, text);
    }

    private static Border MakeBubble(string text, SolidColorBrush bg, SolidColorBrush labelColor, string label, bool isUser)
    {
        var tb = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        };

        var labelTb = new TextBlock
        {
            Text = label,
            Foreground = labelColor,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI")
        };

        var stack = new StackPanel();
        stack.Children.Add(labelTb);
        stack.Children.Add(tb);

        return new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = isUser ? new Thickness(80, 4, 0, 4) : new Thickness(0, 4, 80, 4),
            BorderBrush = BrushBorder,
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private static (Border container, TextBlock textBlock) MakeBubbleAnimated(SolidColorBrush bg, string label)
    {
        var tb = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        };

        var labelTb = new TextBlock
        {
            Text = label,
            Foreground = BrushCyan,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI")
        };

        var stack = new StackPanel();
        stack.Children.Add(labelTb);
        stack.Children.Add(tb);

        var border = new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 4, 80, 4),
            BorderBrush = BrushBorder,
            BorderThickness = new Thickness(1),
            Child = stack
        };

        return (border, tb);
    }

    private void StartTyping(TextBlock target, string text)
    {
        _typeTimer?.Stop();
        _pendingText = text;
        _typeIndex = 0;
        _typeTarget = target;

        _typeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(14) };
        _typeTimer.Tick += TypeTick;
        _typeTimer.Start();
    }

    private void TypeTick(object? sender, EventArgs e)
    {
        if (_typeTarget is null || _typeIndex >= _pendingText.Length)
        {
            _typeTimer?.Stop();
            ScrollToBottom();
            return;
        }

        int chunk = Math.Min(3, _pendingText.Length - _typeIndex);
        _typeTarget.Text += _pendingText.Substring(_typeIndex, chunk);
        _typeIndex += chunk;
        ScrollToBottom();
    }

    private void ShowHelp()
    {
        var topics = new (string kw, string desc)[]
        {
            ("password",      "Password best practices"),
            ("phishing",      "Spot & avoid phishing attacks"),
            ("malware",       "Malware explained"),
            ("vpn",           "Why a VPN matters"),
            ("2fa",           "Two-factor authentication"),
            ("backup",        "The 3-2-1 backup strategy"),
            ("add task",      "Add a cybersecurity task"),
            ("show my tasks", "View your tasks"),
            ("start quiz",    "Launch the security quiz"),
            ("activity log",  "View recent activity"),
            ("tips",          "Quick daily security checklist"),
            ("exit",          "Say goodbye")
        };

        var sp = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        sp.Children.Add(new TextBlock
        {
            Text = "📚  Available Topics & Commands",
            Foreground = BrushYellow,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var (kw, desc) in topics)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            row.Children.Add(new TextBlock
            {
                Text = kw.PadRight(15),
                Foreground = BrushCyan,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Width = 120
            });
            row.Children.Add(new TextBlock
            {
                Text = $"→  {desc}",
                Foreground = BrushMuted,
                FontSize = 12
            });
            sp.Children.Add(row);
        }

        var border = new Border
        {
            Background = BrushBotBubble,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 4, 80, 4),
            BorderBrush = BrushBorder,
            BorderThickness = new Thickness(1),
            Child = sp
        };

        ChatPanel.Children.Add(border);
        ScrollToBottom();
    }

    private void UpdateMemoryPanel()
    {
        MemoryUserLabel.Text = $"👤 Name: {_memory.Username}";
        MemoryTopicLabel.Text = $"⭐ Interest: {(_memory.FavouriteTopic ?? "–")}";
        MemorySentimentLabel.Text = $"💬 Mood: {(_memory.LastSentiment ?? "–")}";
    }

    private void ScrollToBottom()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () => ChatScroll.ScrollToEnd());
    }

    private void ClearChat_Click(object sender, RoutedEventArgs e)
    {
        ChatPanel.Children.Clear();
        AddBotMessage("Chat cleared! How can I help you stay safe online?");
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MENU STRIP
    // ═══════════════════════════════════════════════════════════════════════

    private void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshTasksGrid();
        RefreshLogGrid();
        StatusBarText.Text = "Refreshed.";
    }

    private void MenuGoToTasks_Click(object sender, RoutedEventArgs e) => MainTabs.SelectedIndex = 1;
    private void MenuGoToLog_Click(object sender, RoutedEventArgs e) => MainTabs.SelectedIndex = 3;
    private void MenuGoToAbout_Click(object sender, RoutedEventArgs e) => MainTabs.SelectedIndex = 5;

    // ═══════════════════════════════════════════════════════════════════════
    // TASK MANAGER TAB
    // ═══════════════════════════════════════════════════════════════════════

    private void RefreshTasksGrid(System.Collections.Generic.List<TaskModel>? overrideList = null)
    {
        var tasks = overrideList ?? _taskManager.GetAllTasks();
        TasksGrid.ItemsSource = tasks;
        UpdateDbStatusIndicator();
    }

    private string GetDescriptionText() =>
        new System.Windows.Documents.TextRange(TaskDescriptionBox.Document.ContentStart, TaskDescriptionBox.Document.ContentEnd).Text.Trim();

    private void SetDescriptionText(string text)
    {
        TaskDescriptionBox.Document.Blocks.Clear();
        TaskDescriptionBox.Document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text)));
    }

    private void AddTask_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string title = TaskTitleBox.Text.Trim();
            string desc = GetDescriptionText();
            DateTime? reminder = (TaskNoReminderCheck.IsChecked == true) ? null : TaskReminderPicker.SelectedDate;

            if (string.IsNullOrWhiteSpace(title))
            {
                TaskStatusText.Foreground = BrushRed;
                TaskStatusText.Text = "⚠ Task title cannot be empty.";
                return;
            }

            var task = _taskManager.AddTask(title, desc, reminder);
            TaskStatusText.Foreground = BrushGreen;
            TaskStatusText.Text = $"✅ Task #{task.TaskID} \"{task.Title}\" added successfully.";
            ClearTaskForm_Click(sender, e);
            RefreshTasksGrid();
        }
        catch (Exception ex)
        {
            TaskStatusText.Foreground = BrushRed;
            TaskStatusText.Text = $"❌ {ex.Message}";
            _activityLogger.Log("Errors", ex.Message);
        }
    }

    private void UpdateTask_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTask is null)
        {
            TaskStatusText.Foreground = BrushRed;
            TaskStatusText.Text = "⚠ Select a task in the grid first.";
            return;
        }

        try
        {
            _selectedTask.Title = TaskTitleBox.Text.Trim();
            _selectedTask.Description = GetDescriptionText();
            _selectedTask.ReminderDate = (TaskNoReminderCheck.IsChecked == true) ? null : TaskReminderPicker.SelectedDate;

            if (string.IsNullOrWhiteSpace(_selectedTask.Title))
                throw new ArgumentException("Task title cannot be empty.");

            _taskManager.UpdateTask(_selectedTask);
            TaskStatusText.Foreground = BrushGreen;
            TaskStatusText.Text = $"✅ Task #{_selectedTask.TaskID} updated.";
            RefreshTasksGrid();
        }
        catch (Exception ex)
        {
            TaskStatusText.Foreground = BrushRed;
            TaskStatusText.Text = $"❌ {ex.Message}";
            _activityLogger.Log("Errors", ex.Message);
        }
    }

    private void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTask is null)
        {
            TaskStatusText.Foreground = BrushRed;
            TaskStatusText.Text = "⚠ Select a task in the grid first.";
            return;
        }

        var confirm = MessageBox.Show(this, $"Delete task \"{_selectedTask.Title}\"?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _taskManager.DeleteTask(_selectedTask.TaskID);
            TaskStatusText.Foreground = BrushGreen;
            TaskStatusText.Text = "🗑 Task deleted.";
            ClearTaskForm_Click(sender, e);
            RefreshTasksGrid();
        }
        catch (Exception ex)
        {
            TaskStatusText.Foreground = BrushRed;
            TaskStatusText.Text = $"❌ {ex.Message}";
            _activityLogger.Log("Errors", ex.Message);
        }
    }

    private void MarkComplete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTask is null)
        {
            TaskStatusText.Foreground = BrushRed;
            TaskStatusText.Text = "⚠ Select a task in the grid first.";
            return;
        }

        try
        {
            _taskManager.MarkComplete(_selectedTask.TaskID);
            TaskStatusText.Foreground = BrushGreen;
            TaskStatusText.Text = $"✅ \"{_selectedTask.Title}\" marked complete.";
            RefreshTasksGrid();
        }
        catch (Exception ex)
        {
            TaskStatusText.Foreground = BrushRed;
            TaskStatusText.Text = $"❌ {ex.Message}";
            _activityLogger.Log("Errors", ex.Message);
        }
    }

    private void ViewTasks_Click(object sender, RoutedEventArgs e) => RefreshTasksGrid();

    private void ClearTaskForm_Click(object sender, RoutedEventArgs e)
    {
        TaskTitleBox.Clear();
        SetDescriptionText("");
        TaskReminderPicker.SelectedDate = null;
        TaskNoReminderCheck.IsChecked = true;
        _selectedTask = null;
        TasksGrid.SelectedItem = null;
        TaskStatusText.Text = "";
    }

    private void TaskNoReminderCheck_Changed(object sender, RoutedEventArgs e)
    {
        TaskReminderPicker.IsEnabled = TaskNoReminderCheck.IsChecked != true;
    }

    private void SearchTasks_Click(object sender, RoutedEventArgs e)
    {
        string keyword = TaskSearchBox.Text.Trim();
        RefreshTasksGrid(string.IsNullOrWhiteSpace(keyword) ? null : _taskManager.SearchTasks(keyword));
    }

    private void TasksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TasksGrid.SelectedItem is TaskModel task)
        {
            _selectedTask = task;
            TaskTitleBox.Text = task.Title;
            SetDescriptionText(task.Description);
            TaskNoReminderCheck.IsChecked = !task.ReminderDate.HasValue;
            TaskReminderPicker.IsEnabled = task.ReminderDate.HasValue;
            TaskReminderPicker.SelectedDate = task.ReminderDate;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUIZ TAB
    // ═══════════════════════════════════════════════════════════════════════

    private void StartQuizInternal()
    {
        _quizManager.StartQuiz(randomOrder: true);
        QuizScoreText.Text = $"Score: 0 / {_quizManager.TotalQuestions}";
        QuizReviewList.ItemsSource = null;
        QuizFeedbackText.Text = "";
        QuizExplanationText.Text = "";
        ShowCurrentQuestion();
    }

    private void StartQuiz_Click(object sender, RoutedEventArgs e) => StartQuizInternal();

    private void ShowCurrentQuestion()
    {
        var q = _quizManager.CurrentQuestion;
        QuizFeedbackText.Text = "";
        QuizExplanationText.Text = "";

        if (q is null)
        {
            ShowQuizResults();
            return;
        }

        QuizProgressText.Text = $"Question {_quizManager.CurrentIndex + 1} of {_quizManager.TotalQuestions}";
        QuizProgressBar.Maximum = _quizManager.TotalQuestions;
        QuizProgressBar.Value = _quizManager.CurrentIndex;
        QuizTopicText.Text = $"TOPIC: {q.Topic.ToUpper()}  ·  {(q.Type == QuestionType.TrueFalse ? "TRUE / FALSE" : "MULTIPLE CHOICE")}";
        QuizQuestionText.Text = q.Text;
        QuizOptionsList.ItemsSource = q.Options;
        QuizOptionsList.SelectedIndex = -1;
        QuizScoreText.Text = $"Score: {_quizManager.Score} / {_quizManager.TotalQuestions}";
    }

    private void QuizOptionsList_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* selection captured at submit time */ }

    private void SubmitAnswer_Click(object sender, RoutedEventArgs e)
    {
        if (_quizManager.CurrentQuestion is null) { AddBotMessage("Start the quiz first!"); return; }

        if (QuizOptionsList.SelectedIndex < 0)
        {
            QuizFeedbackText.Foreground = BrushYellow;
            QuizFeedbackText.Text = "⚠ Please select an answer first.";
            return;
        }

        var q = _quizManager.CurrentQuestion;
        bool correct = _quizManager.SubmitAnswer(QuizOptionsList.SelectedIndex);

        QuizFeedbackText.Foreground = correct ? BrushGreen : BrushRed;
        QuizFeedbackText.Text = correct ? "✅ Correct!" : $"❌ Incorrect. The correct answer was: {q!.CorrectAnswerText}";
        QuizExplanationText.Text = q!.Explanation;
        QuizScoreText.Text = $"Score: {_quizManager.Score} / {_quizManager.TotalQuestions}";

        if (_quizManager.IsFinished)
            ShowQuizResults();
    }

    private void NextQuestion_Click(object sender, RoutedEventArgs e) => ShowCurrentQuestion();

    private void ShowQuizResults()
    {
        QuizTopicText.Text = "QUIZ COMPLETE";
        QuizQuestionText.Text = $"🏁 Final Score: {_quizManager.Score} / {_quizManager.TotalQuestions} ({_quizManager.PercentageScore:0}%)\n{_quizManager.PerformanceMessage}";
        QuizOptionsList.ItemsSource = null;
        QuizFeedbackText.Text = "";
        QuizExplanationText.Text = "";
        QuizProgressText.Text = "Quiz finished.";
        QuizProgressBar.Value = QuizProgressBar.Maximum;

        var review = _quizManager.GetReview()
            .Select(r => $"{(r.Correct ? "✅" : "❌")} [{r.Question.Topic}] {r.Question.Text}\n    Your answer: {r.ChosenText} | Correct answer: {r.Question.CorrectAnswerText}")
            .ToList();
        QuizReviewList.ItemsSource = review;
    }

    private void RestartQuiz_Click(object sender, RoutedEventArgs e) => StartQuizInternal();

    private void ExitQuiz_Click(object sender, RoutedEventArgs e)
    {
        QuizTopicText.Text = "";
        QuizQuestionText.Text = "Welcome to the CyberBot Security Quiz! Click 'Start Quiz' below to test your knowledge across 13 cybersecurity topics.";
        QuizOptionsList.ItemsSource = null;
        QuizFeedbackText.Text = "";
        QuizExplanationText.Text = "";
        QuizReviewList.ItemsSource = null;
        QuizProgressText.Text = "Press Start Quiz to begin.";
        QuizProgressBar.Value = 0;
        QuizScoreText.Text = "Score: 0 / 0";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ACTIVITY LOG TAB
    // ═══════════════════════════════════════════════════════════════════════

    private void RefreshLogGrid()
    {
        bool showAll = LogShowAllCheck.IsChecked == true;
        LogGrid.ItemsSource = showAll ? _activityLogger.GetAll() : _activityLogger.GetRecent(10);
        UpdateDbStatusIndicator();
    }

    private void LogRefresh_Click(object sender, RoutedEventArgs e) => RefreshLogGrid();

    private void LogSearch_Click(object sender, RoutedEventArgs e)
    {
        string keyword = LogSearchBox.Text.Trim();
        LogGrid.ItemsSource = string.IsNullOrWhiteSpace(keyword) ? _activityLogger.GetRecent(10) : _activityLogger.Search(keyword);
    }

    private void LogExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = $"ActivityLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv" };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _activityLogger.ExportToCsv(dialog.FileName);
                MessageBox.Show(this, "Activity log exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LogClear_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(this, "Clear the entire activity log? This cannot be undone.", "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        _activityLogger.ClearLog();
        RefreshLogGrid();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATABASE TAB
    // ═══════════════════════════════════════════════════════════════════════

    private void DbSave_Click(object sender, RoutedEventArgs e)
    {
        DbConfig.Server = DbServerBox.Text.Trim();
        DbConfig.Port = DbPortBox.Text.Trim();
        DbConfig.Database = DbNameBox.Text.Trim();
        DbConfig.User = DbUserBox.Text.Trim();
        DbConfig.Password = DbPasswordBox.Password;

        DbStatusText.Foreground = BrushGreen;
        DbStatusText.Text = "💾 Settings saved for this session.";
        _activityLogger.Log("Database Event", "Connection settings updated.");
    }

    private void DbTest_Click(object sender, RoutedEventArgs e)
    {
        DbSave_Click(sender, e);
        bool ok = _db.TestConnection(out string message);
        DbStatusText.Foreground = ok ? BrushGreen : BrushRed;
        DbStatusText.Text = message;
        UpdateDbStatusIndicator();
    }

    private void DbInitSchema_Click(object sender, RoutedEventArgs e)
    {
        DbSave_Click(sender, e);
        bool ok = _db.InitializeSchema(out string message);
        DbStatusText.Foreground = ok ? BrushGreen : BrushRed;
        DbStatusText.Text = message;
        UpdateDbStatusIndicator();
        RefreshTasksGrid();
        RefreshLogGrid();
    }

    private void UpdateDbStatusIndicator()
    {
        bool fallback = _taskManager.UsingFallback || _activityLogger.UsingFallback;
        DbStatusIndicator.Text = fallback ? "DB: ⚠ Offline (using memory fallback)" : "DB: ✅ Connected";
        DbStatusIndicator.Foreground = fallback ? BrushYellow : BrushGreen;
    }
}
