using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberBotWPF;

public class ResponseEngine
{
    private static readonly Random _rng = new();
    private readonly ChatMemory _memory;

    // CyberGuard Security Shield & Branding Banner
    private static readonly string _cyberGuardLogo = @"
     .--------.
    / .------. \
   / /        \ \     ____       _               ____                      _ 
   | |  [🔒]  | |    / ___| _  _| |__   ___ _ __ / ___| _   _  __ _ _ __  __| |
   \ \        / /   | |   | | | | '_ \ / _ \ '__| |  _ | | | |/ _` | '__|/ _` |
    \ '------' /    | |___| |_| | |_) |  __/ |  | |_| || |_| | (_| | |  | (_| |
     '--------'      \____|\__, |_.__/ \___|_|   \____| \__,_|\__,_|_|   \__,_|
                           |___/                                             
";

    private static readonly (string[] Keywords, string? Response, string? Tip, string TopicKey)[] _staticResponses =
    {
        (
            new[] { "how are you", "how r u", "you ok", "you good", "are you well" },
            "I'm just code, but my defense algorithms are running at full strength and ready to keep you safe! 💪",
            null, "status"
        ),
        (
            new[] { "purpose", "what do you do", "what can you do", "who are you", "your job" },
            _cyberGuardLogo + "\nI am CyberGuard — your professional cybersecurity sandbox advisor. I am configured to help you eliminate digital threats, audit security habits, and protect your data.",
            null, "purpose"
        ),
        (
            new[] { "password", "passphrase", "login credentials" },
            "Use strong, unique passwords for every account. Aim for 16+ characters mixing letters, numbers, and symbols.",
            "💡 Tip: Use a reputable password manager (e.g. Bitwarden or 1Password) so you only need to remember one master password.",
            "password"
        ),
        (
            new[] { "malware", "virus", "ransomware", "trojan", "spyware" },
            "Malware is malicious software designed to damage, disrupt, or gain unauthorised access to your system. Keep your OS and antivirus up to date and avoid downloading software from unverified sources.",
            "💡 Tip: Windows Defender is solid, but adding Malwarebytes as a second-opinion scanner can catch things that slip through.",
            "malware"
        ),
        (
            new[] { "vpn", "virtual private network", "proxy" },
            "A VPN encrypts your internet traffic and masks your IP address — crucial on public Wi-Fi where attackers can sniff unencrypted data.",
            "💡 Tip: Choose a no-log VPN from a reputable provider (Mullvad, ProtonVPN). Free VPNs often monetise your data.",
            "vpn"
        ),
        (
            new[] { "2fa", "two factor", "two-factor", "mfa", "authenticator", "otp" },
            "Two-factor authentication (2FA) requires a second proof of identity — usually a time-based code — in addition to your password. Even if your password is stolen, an attacker can't log in without the second factor.",
            "💡 Tip: Use an authenticator app (Authy, Google Authenticator) instead of SMS codes — SIM-swap attacks can intercept SMS.",
            "2fa"
        ),
        (
            new[] { "backup", "back up", "data loss", "restore" },
            "Follow the 3-2-1 backup rule: keep 3 copies of your data, on 2 different media types, with 1 copy stored offsite (e.g. cloud).",
            "💡 Tip: Test your backups regularly! A backup you've never restored from is a backup you can't trust.",
            "backup"
        ),
        (
            new[] { "privacy", "personal data", "data protection", "gdpr", "personal information" },
            "Protecting your privacy online means controlling who has access to your personal data. Be mindful of what you share on social media and review app permissions regularly.",
            "💡 Tip: Use a privacy-focused browser like Firefox or Brave, and consider a search engine like DuckDuckGo.",
            "privacy"
        ),
        (
            new[] { "scam", "fraud", "con", "trick", "deceive" },
            "Scammers use urgency, fear, and impersonation to manipulate people. When in doubt, verify independently before acting — call the company directly using a number from their official website.",
            "💡 Tip: If an offer sounds too good to be true, it almost certainly is. Take a breath and verify before clicking anything.",
            "scam"
        ),
        (
            new[] { "social engineering", "manipulation", "pretexting", "baiting" },
            "Social engineering exploits human psychology rather than technical vulnerabilities. Attackers might impersonate IT support, a colleague, or a bank to extract sensitive information.",
            "💡 Tip: Legitimate organisations will never ask for your password or OTP. Hang up and call back via an official number.",
            "socialengineering"
        ),
        (
            new[] { "firewall", "network security", "intrusion" },
            "A firewall monitors and controls incoming and outgoing network traffic based on security rules. It acts as a barrier between trusted and untrusted networks.",
            "💡 Tip: Enable the built-in Windows Firewall or use a hardware firewall/router for home networks.",
            "firewall"
        ),
        (
            new[] { "tip", "tips", "advice", "checklist", "best practice", "recommendations" },
            null,
            null, "tips"
        ),
    };

    private static readonly Dictionary<string, string[]> _randomResponses = new()
    {
        ["phishing"] = new[]
        {
            "Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations.",
            "Always verify the sender's actual email address — hover over it before clicking. Official companies rarely use Gmail or Hotmail addresses.",
            "If an email creates urgency ('Act now or your account will be closed!'), that's a red flag. Legitimate companies give you time.",
            "Never click links in emails to log in. Open your browser and navigate to the site directly instead.",
            "Phishing pages often look identical to real sites. Check the URL carefully — a single character difference is a giveaway.",
        },
        ["password"] = new[]
        {
            "Use a passphrase — four random words strung together (e.g. 'correct-horse-battery-staple') are stronger and easier to remember than 'P@ssw0rd'.",
            "Never reuse passwords. If one site is breached, attackers will try your credentials on every other site (credential stuffing).",
            "Change your passwords immediately if you receive a breach notification from HaveIBeenPwned or a similar service.",
            "Avoid personal details in passwords — birthdays, pet names, and sports teams are the first things attackers try.",
        },
        ["privacy"] = new[]
        {
            "Review your social media privacy settings at least once a year — platforms often reset them after updates.",
            "Be wary of third-party apps that request access to your social accounts. Revoke permissions for apps you no longer use.",
            "Use a separate email address for newsletters and sign-ups to keep your main inbox clean and reduce spam exposure.",
            "Enable 'Login Notifications' on your accounts so you're alerted to new sign-ins from unfamiliar devices.",
        },
        ["scam"] = new[]
        {
            "Be cautious of unsolicited calls claiming to be from Microsoft, your bank, or SARS. They will never call you out of the blue to ask for remote access.",
            "Gift card payment requests are an immediate scam red flag — no legitimate company or government body will ask you to pay in gift cards.",
            "Romance scams are on the rise. If someone you've never met asks for money online, be extremely cautious regardless of how convincing they seem.",
            "Investment scams promising guaranteed high returns prey on people looking to grow their savings. Always verify with the FSCA in South Africa.",
        },
    };

    private static readonly Dictionary<string, (string Label, string Acknowledgement)> _sentiments = new()
    {
        ["worried"] = ("Worried 😟", "I understand — it's completely natural to feel worried about online threats. Let me share some information to help you feel more confident."),
        ["scared"] = ("Scared 😨", "Don't worry! Cybersecurity can feel overwhelming, but small steps make a big difference. You've already taken the first step by asking."),
        ["frustrated"] = ("Frustrated 😤", "I hear your frustration — cyber threats are genuinely annoying to deal with. Let's tackle this together."),
        ["confused"] = ("Confused 😕", "No worries — cybersecurity has a lot of jargon. Let me try to break this down more clearly for you."),
        ["curious"] = ("Curious 🤔", "Great — curiosity is the best starting point for staying safe! Here's what you should know:"),
        ["excited"] = ("Excited 😄", "Love the enthusiasm! Let's channel that energy into some great security habits:"),
        ["overwhelmed"] = ("Overwhelmed 😓", "Take a deep breath — you don't need to fix everything at once. Let's focus on the most important things first."),
        ["unsure"] = ("Unsure 🤷", "That's perfectly okay — nobody knows everything about cybersecurity. Let me guide you through it."),
        ["angry"] = ("Frustrated 😤", "It sounds like you've had a bad experience. I'm sorry to hear that. Let me help you understand what happened and how to prevent it."),
    };

    private static readonly string[] _followUpPhrases =
        { "more", "tell me more", "explain more", "another tip", "give me another",
          "continue", "elaborate", "more details", "go on", "what else" };

    private static readonly string[] _interestPhrases =
        { "interested in", "i like", "i love", "i want to learn about",
          "curious about", "tell me about", "i'm focusing on" };

    public ResponseEngine(ChatMemory memory) => _memory = memory;

    public EngineResult GetResponse(string input)
    {
        string norm = input.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(norm))
            return new EngineResult("I didn't catch that. Try asking about passwords, phishing, or type 'help'.");

        _memory.AddHistory($"User: {input}");

        if (IsMatch(norm, "exit", "quit", "bye", "goodbye", "ciao", "later"))
            return new EngineResult("__EXIT__") { IsExit = true };

        if (IsMatch(norm, "help", "?", "what can you do", "commands"))
        {
            string helpText = _cyberGuardLogo + "\n" +
                              "🛡️ CyberGuard Command Center\n" +
                              "You can ask me questions about the following topics:\n" +
                              "• Passwords & Passphrases\n" +
                              "• Phishing & Scams\n" +
                              "• Malware & Firewalls\n" +
                              "• VPNs & Privacy\n" +
                              "• 2FA / MFA Setup\n" +
                              "• Data Backups";
            return new EngineResult(helpText) { IsHelp = true };
        }

        string? sentimentAck = DetectSentiment(norm);

        // FIX: If a sentiment was detected but NO other tech topics were typed, 
        // treat the acknowledgment as the entire primary answer.
        if (sentimentAck is not null && IsOnlySentiment(norm))
        {
            return new EngineResult(sentimentAck);
        }

        if (_followUpPhrases.Any(p => norm.Contains(p)) && _memory.LastTopicKey is not null)
        {
            return BuildFollowUp(sentimentAck);
        }

        CheckInterestDeclaration(norm);

        if (IsMatch(norm, "tip", "tips", "advice", "checklist", "best practice", "recommendations"))
        {
            _memory.LastTopicKey = "tips";
            var tipsList = DailyTips();
            var tipsText = "Here's your daily security checklist:\n" +
                           string.Join("\n", tipsList.Select((t, i) => $"  {i + 1,2}. {t}"));
            return new EngineResult(tipsText) { SentimentAck = sentimentAck };
        }

        foreach (var (key, responses) in _randomResponses)
        {
            if (norm.Contains(key))
            {
                string response = responses[_rng.Next(responses.Length)];
                _memory.LastTopicKey = key;

                string? personalise = PersonaliseIfRelevant(key);
                if (personalise is not null)
                    response = personalise + " " + response;

                return new EngineResult(response) { SentimentAck = sentimentAck };
            }
        }

        foreach (var (keywords, response, tip, topicKey) in _staticResponses)
        {
            if (keywords.Any(k => norm.Contains(k)))
            {
                _memory.LastTopicKey = topicKey;

                string? personalise = PersonaliseIfRelevant(topicKey);
                string finalResponse = personalise is not null
                    ? personalise + " " + response
                    : response!;

                return new EngineResult(finalResponse)
                {
                    Tip = tip,
                    SentimentAck = sentimentAck
                };
            }
        }

        string fallback = $"I am CyberGuard, and I didn't quite parse that request. " +
                         $"Try asking about passwords, phishing, malware, VPNs, 2FA, backups, privacy, or scams — or type 'help' to see my interface console.";
        return new EngineResult(fallback) { SentimentAck = sentimentAck };
    }

    public static string[] DailyTips() => new[]
    {
        "Update your software and OS regularly.",
        "Use a password manager.",
        "Enable 2FA on all important accounts.",
        "Think before you click any link or attachment.",
        "Use a VPN on public Wi-Fi.",
        "Back up your data weekly.",
        "Review app permissions on your phone.",
        "Lock your screen when stepping away.",
        "Check HaveIBeenPwned.com to see if your email was breached.",
        "Be sceptical of unsolicited messages, even from known contacts.",
    };

    private string? DetectSentiment(string norm)
    {
        foreach (var (keyword, (label, ack)) in _sentiments)
        {
            if (norm.Contains(keyword))
            {
                _memory.SetSentiment(label);
                return ack;
            }
        }
        return null;
    }

    // HELPER METHOD: Confirms if the message is purely emotional or has technical text
    private bool IsOnlySentiment(string norm)
    {
        string[] coreTopics = { "password", "phishing", "malware", "vpn", "2fa", "backup", "privacy", "scam", "firewall", "help", "tip" };
        return !coreTopics.Any(topic => norm.Contains(topic));
    }

    private void CheckInterestDeclaration(string norm)
    {
        foreach (string phrase in _interestPhrases)
        {
            int idx = norm.IndexOf(phrase, StringComparison.Ordinal);
            if (idx >= 0)
            {
                string after = norm.Substring(idx + phrase.Length).Trim().TrimEnd('.', '!', '?');
                if (after.Length > 0 && after.Length < 40)
                {
                    string topic = System.Globalization.CultureInfo.CurrentCulture
                                                 .TextInfo.ToTitleCase(after);
                    _memory.SetFavouriteTopic(topic);
                }
                break;
            }
        }
    }

    private string? PersonaliseIfRelevant(string topicKey)
    {
        if (_memory.FavouriteTopic is null) return null;
        string fav = _memory.FavouriteTopic.ToLower();

        if (fav.Contains(topicKey) || topicKey.Contains(fav.Split(' ')[0]))
            return $"As someone interested in {_memory.FavouriteTopic}, this is especially relevant —";

        return null;
    }

    private EngineResult BuildFollowUp(string? sentimentAck)
    {
        string key = _memory.LastTopicKey!;

        if (_randomResponses.TryGetValue(key, out var pool))
        {
            string r = pool[_rng.Next(pool.Length)];
            return new EngineResult(r) { SentimentAck = sentimentAck };
        }

        foreach (var (keywords, response, tip, topicKey) in _staticResponses)
        {
            if (topicKey == key)
                return new EngineResult(response!, sentimentAck) { Tip = tip };
        }

        return new EngineResult(
            $"Let me expand on that. Type a specific topic like 'password', 'phishing', or '2FA' and I'll go deeper.",
            sentimentAck);
    }

    private static bool IsMatch(string norm, params string[] keywords) =>
        keywords.Any(k => norm.Contains(k));
}

public class EngineResult
{
    public string Response { get; set; }
    public string? SentimentAck { get; set; }
    public string? Tip { get; set; }
    public bool IsExit { get; set; }
    public bool IsHelp { get; set; }

    public EngineResult(string response)
    {
        Response = response;
    }

    public EngineResult(string response, string? sentimentAck)
    {
        Response = response;
        SentimentAck = sentimentAck;
    }
}