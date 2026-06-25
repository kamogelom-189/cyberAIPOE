using System.Collections.Generic;
using CyberBotWPF.Models;

namespace CyberBotWPF.Managers;

/// <summary>
/// Static bank of cybersecurity quiz questions (13 topics, mix of
/// Multiple Choice and True/False, as required by the assignment).
/// </summary>
public static class QuizQuestions
{
    public static List<Question> GetAll() => new()
    {
        new Question
        {
            Topic = "Passwords",
            Type = QuestionType.MultipleChoice,
            Text = "Which of these passwords is the strongest?",
            Options = new[] { "password123", "MyDog2020", "correct-horse-battery-staple", "qwerty" },
            CorrectIndex = 2,
            Explanation = "Long passphrases of random unrelated words are harder to brute-force than short, predictable passwords."
        },
        new Question
        {
            Topic = "Passwords",
            Type = QuestionType.TrueFalse,
            Text = "True or False: It's safe to reuse the same password across multiple important accounts.",
            Options = new[] { "True", "False" },
            CorrectIndex = 1,
            Explanation = "Never share or reuse your password — if one site is breached, attackers try the same credentials everywhere else (credential stuffing)."
        },
        new Question
        {
            Topic = "Phishing",
            Type = QuestionType.MultipleChoice,
            Text = "What is the safest action when you receive an unexpected email asking you to 'verify your account' via a link?",
            Options = new[] { "Click the link immediately", "Reply with your password", "Go to the official website directly instead of clicking the link", "Forward it to friends" },
            CorrectIndex = 2,
            Explanation = "Never click login links in emails. Navigate to the official site yourself to check your account."
        },
        new Question
        {
            Topic = "Phishing",
            Type = QuestionType.TrueFalse,
            Text = "True or False: Phishing emails often create a false sense of urgency to pressure you into acting quickly.",
            Options = new[] { "True", "False" },
            CorrectIndex = 0,
            Explanation = "Urgency ('act now or your account will be closed') is a classic phishing red flag."
        },
        new Question
        {
            Topic = "Malware",
            Type = QuestionType.MultipleChoice,
            Text = "What is ransomware?",
            Options = new[] { "A free antivirus tool", "Malware that encrypts your files and demands payment", "A type of firewall", "A password manager" },
            CorrectIndex = 1,
            Explanation = "Ransomware locks or encrypts your files and demands a ransom for the decryption key."
        },
        new Question
        {
            Topic = "Social Engineering",
            Type = QuestionType.TrueFalse,
            Text = "True or False: A caller claiming to be 'IT Support' asking for your password should always be trusted.",
            Options = new[] { "True", "False" },
            CorrectIndex = 1,
            Explanation = "Legitimate IT support will never ask for your password. This is a classic social engineering / pretexting tactic."
        },
        new Question
        {
            Topic = "Safe Browsing",
            Type = QuestionType.MultipleChoice,
            Text = "Which browser indicator suggests a website connection is encrypted?",
            Options = new[] { "A padlock icon and https://", "A pop-up advertisement", "A long URL", "A flashing banner" },
            CorrectIndex = 0,
            Explanation = "The padlock and 'https://' indicate the connection is encrypted with TLS/SSL — always check before entering sensitive data."
        },
        new Question
        {
            Topic = "Public WiFi",
            Type = QuestionType.TrueFalse,
            Text = "True or False: It's safe to do online banking on open public Wi-Fi without a VPN.",
            Options = new[] { "True", "False" },
            CorrectIndex = 1,
            Explanation = "Open Wi-Fi networks can be intercepted by attackers. Use a VPN to encrypt your traffic on public networks."
        },
        new Question
        {
            Topic = "Encryption",
            Type = QuestionType.MultipleChoice,
            Text = "What does encryption primarily protect against?",
            Options = new[] { "Slow internet speeds", "Unauthorised reading of your data", "Running out of storage", "Software bugs" },
            CorrectIndex = 1,
            Explanation = "Encryption scrambles data so that only someone with the correct key can read it, protecting confidentiality."
        },
        new Question
        {
            Topic = "Firewalls",
            Type = QuestionType.TrueFalse,
            Text = "True or False: A firewall monitors and controls incoming and outgoing network traffic.",
            Options = new[] { "True", "False" },
            CorrectIndex = 0,
            Explanation = "Firewalls act as a barrier, filtering traffic between trusted and untrusted networks based on security rules."
        },
        new Question
        {
            Topic = "Updates",
            Type = QuestionType.MultipleChoice,
            Text = "Why should you install software/OS updates promptly?",
            Options = new[] { "They make your PC look modern", "They often patch known security vulnerabilities", "They are required by law", "They increase battery life" },
            CorrectIndex = 1,
            Explanation = "Updates frequently patch security flaws that attackers actively exploit — delaying them leaves you exposed."
        },
        new Question
        {
            Topic = "Ransomware",
            Type = QuestionType.TrueFalse,
            Text = "True or False: Regular offline backups can help you recover from a ransomware attack without paying.",
            Options = new[] { "True", "False" },
            CorrectIndex = 0,
            Explanation = "If your files are backed up offline (3-2-1 rule), you can restore them instead of paying the ransom."
        },
        new Question
        {
            Topic = "Identity Theft",
            Type = QuestionType.MultipleChoice,
            Text = "Which of these is a warning sign of identity theft?",
            Options = new[] { "Unexpected accounts or charges in your name", "Receiving a software update", "Your antivirus running a scheduled scan", "A strong Wi-Fi signal" },
            CorrectIndex = 0,
            Explanation = "Unfamiliar accounts, charges, or credit inquiries can indicate someone is using your stolen identity."
        },
        new Question
        {
            Topic = "Online Shopping",
            Type = QuestionType.TrueFalse,
            Text = "True or False: You should only enter your card details on websites with 'https' and a trusted, verified domain.",
            Options = new[] { "True", "False" },
            CorrectIndex = 0,
            Explanation = "Always verify the site is encrypted (https) and legitimate before entering payment information."
        },
        new Question
        {
            Topic = "Email Safety",
            Type = QuestionType.MultipleChoice,
            Text = "What should you do before opening an unexpected email attachment?",
            Options = new[] { "Open it immediately to see what it is", "Verify the sender and scan it first", "Forward it to all contacts", "Reply asking for more attachments" },
            CorrectIndex = 1,
            Explanation = "Unexpected attachments are a common malware delivery method — verify the sender and scan before opening."
        },
    };
}
