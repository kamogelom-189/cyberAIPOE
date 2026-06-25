using System;
using System.Collections.Generic;
using System.Linq;
using CyberBotWPF.Models;

namespace CyberBotWPF.Managers;

/// <summary>
/// Drives the cybersecurity quiz: question order, current position,
/// scoring, and final performance messaging. Pure in-memory state —
/// no persistence required for the quiz itself.
/// </summary>
public class QuizManager
{
    private readonly ActivityLogger _logger;
    private List<Question> _questions = new();
    private readonly List<(Question q, int chosenIndex, bool correct)> _answerLog = new();

    public int CurrentIndex { get; private set; }
    public int Score { get; private set; }
    public int TotalQuestions => _questions.Count;
    public bool IsFinished => CurrentIndex >= _questions.Count;

    public QuizManager(ActivityLogger logger) => _logger = logger;

    /// <summary>Starts a new quiz. randomOrder shuffles the question bank (bonus feature).</summary>
    public void StartQuiz(bool randomOrder = true)
    {
        _questions = QuizQuestions.GetAll();
        if (randomOrder)
        {
            var rng = new Random();
            _questions = _questions.OrderBy(_ => rng.Next()).ToList();
        }
        CurrentIndex = 0;
        Score = 0;
        _answerLog.Clear();
        _logger.Log("Quiz Started", $"{_questions.Count} questions loaded.");
    }

    public Question? CurrentQuestion => IsFinished ? null : _questions[CurrentIndex];

    /// <summary>Submits an answer for the current question, advances the index, and returns whether it was correct.</summary>
    public bool SubmitAnswer(int chosenIndex)
    {
        if (CurrentQuestion is null) throw new InvalidOperationException("Quiz already finished.");

        var q = CurrentQuestion;
        bool correct = chosenIndex == q.CorrectIndex;
        if (correct) Score++;

        _answerLog.Add((q, chosenIndex, correct));
        _logger.Log("Question Answered",
            $"[{q.Topic}] Q{CurrentIndex + 1}: {(correct ? "Correct" : "Incorrect")} (chose \"{q.Options[chosenIndex]}\")");

        CurrentIndex++;

        if (IsFinished)
            _logger.Log("Quiz Finished", $"Final score {Score}/{_questions.Count} ({PercentageScore:0}%).");

        return correct;
    }

    public double PercentageScore => TotalQuestions == 0 ? 0 : (Score * 100.0) / TotalQuestions;

    public string PerformanceMessage
    {
        get
        {
            double pct = PercentageScore;
            return pct switch
            {
                >= 90 => "🏆 Excellent! You are a cybersecurity expert.",
                >= 70 => "🎉 Great Job! You know your stuff.",
                >= 50 => "👍 Good effort! A bit more practice will make you even safer online.",
                _ => "📚 Keep learning to stay safe online — review the explanations below and try again!"
            };
        }
    }

    /// <summary>Full review list of question, the user's answer, correctness, and the correct answer text.</summary>
    public IReadOnlyList<(Question Question, string ChosenText, bool Correct)> GetReview() =>
        _answerLog.Select(a => (a.q, a.q.Options[a.chosenIndex], a.correct)).ToList();

    public void Restart() => StartQuiz();
}
