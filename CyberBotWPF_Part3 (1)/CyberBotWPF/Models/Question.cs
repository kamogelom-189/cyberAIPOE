using System;

namespace CyberBotWPF.Models;

public enum QuestionType { MultipleChoice, TrueFalse }

/// <summary>
/// A single quiz question. For TrueFalse questions, Options is always
/// ["True", "False"] and CorrectIndex is 0 (True) or 1 (False).
/// </summary>
public class Question
{
    public string Topic { get; set; } = "";
    public string Text { get; set; } = "";
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public string[] Options { get; set; } = Array.Empty<string>();
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = "";

    public string CorrectAnswerText => Options.Length > CorrectIndex ? Options[CorrectIndex] : "";
}
