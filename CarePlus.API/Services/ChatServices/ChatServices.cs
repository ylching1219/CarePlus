using CarePlus.API.DTO.ChatDTO;
using CarePlus.API.Services.ChatServices.Interface;
using Microsoft.Extensions.Options;

namespace CarePlus.API.Services.ChatServices;

public sealed class ChatServices : IChatServices
{
    private readonly IVectorKnowledgeStore _knowledgeStore;
    private readonly IGroqChatClient _groqClient;
    private readonly ChatOptions _options;
    private readonly IWebHostEnvironment _environment;

    public ChatServices(
        IVectorKnowledgeStore knowledgeStore,
        IGroqChatClient groqClient,
        IOptions<ChatOptions> options,
        IWebHostEnvironment environment)
    {
        _knowledgeStore = knowledgeStore;
        _groqClient = groqClient;
        _options = options.Value;
        _environment = environment;
    }

    public async Task<ChatResponse> AnswerAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var question = request.Message.Trim();
        if (TryAnswerCommonWebsiteQuestion(question, out var commonResponse))
        {
            return commonResponse;
        }
        if (TryAnswerSmallTalk(question, out var smallTalkResponse))
        {
            return smallTalkResponse;
        }

        var matches = await _knowledgeStore.SearchAsync(ExpandRetrievalQuery(question), cancellationToken);
        var selected = LimitContext(matches, _options.MaximumContextCharacters);

        if (selected.Count == 0)
        {
            return new ChatResponse(
                "I could not find verified CarePlus information for that question. Please contact the clinic for help.",
                _options.Disclaimer,
                []);
        }

        var history = request.History
            .Where(turn => turn.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(turn.Content))
            .TakeLast(Math.Clamp(_options.MaximumHistoryMessages, 0, 12))
            .Select(turn => new ChatTurnRequest { Role = turn.Role, Content = turn.Content.Trim() })
            .ToArray();
        var reply = AddCarePlusPageLinks(await _groqClient.CompleteAsync(question, history, selected, cancellationToken));
        var sources = selected
            .GroupBy(match => new { match.Title, match.Source })
            .Select(group => new ChatSourceResponse(group.Key.Title, group.Key.Source, Math.Round(group.Max(item => item.Score), 3)))
            .ToArray();
        return new ChatResponse(reply, _options.Disclaimer, sources);
    }

    private static string AddCarePlusPageLinks(string reply)
    {
        var pageLinks = new (string Label, string Route)[]
        {
            ("Clinic announcements", "/announcements"),
            ("Personal health records", "/records"),
            ("Upcoming visits", "/visits"),
            ("Book a visit", "/book"),
            ("Appointments", "/book"),
            ("Appointment", "/book"),
            ("Clinic records", "/clinic-records"),
            ("Outstanding bill", "/invoices"),
            ("Payment history", "/payments"),
            ("Family profiles", "/family"),
            ("Order history", "/orders"),
            ("Contact us", "/contact"),
            ("Notifications", "/notifications"),
            ("Medicine", "/medicine"),
            ("Doctors", "/doctors"),
            ("Booking", "/book"),
            ("Account", "/account"),
            ("Register", "/register"),
            ("Login", "/login"),
            ("Cart", "/cart")
        };

        foreach (var (label, route) in pageLinks)
        {
            reply = reply.Replace($"**[{label}]({route})**", $"[{label}]({route})", StringComparison.OrdinalIgnoreCase);
            reply = reply.Replace($"**{label}**", $"[{label}]({route})", StringComparison.OrdinalIgnoreCase);
        }
        return reply;
    }

    private ChatSourceResponse WebsiteGuideSource() => new(
        "CarePlus website guide and common questions",
        "CarePlus knowledge base/careplus-website-guide.md",
        1);

    private ChatSourceResponse PublicClinicSource() => new(
        "CarePlus doctors, clinic information, and support",
        "CarePlus knowledge base/careplus-public-clinic.md",
        1);

    private bool TryAnswerCommonWebsiteQuestion(string question, out ChatResponse response)
    {
        var normalized = question.Trim().Trim('.', '!', '?', ',', '，', '。', '！', '？').ToLowerInvariant();
        var isChinese = ContainsChinese(question);
        var mentionsDoctor = ContainsAny(normalized, "doctor", "医生");
        var asksAvailability = ContainsAny(normalized, "available", "availability", "free", "open slot", "有空", "可预约", "空档");
        if (mentionsDoctor && asksAvailability)
        {
            response = new ChatResponse(
                isChinese
                    ? "医生的可预约时间会根据日期和时段变化。\n\nStep 1: 登录 CarePlus 后打开 [预约](/book)。\n\nStep 2: 选择日期以及上午或夜间时段。\n\nStep 3: 查看系统从诊所数据库载入的可用医生与时间。\n\nStep 4: 如果没有指定医生，可选择 **任何有空的医生**。"
                    : "Doctor availability changes according to the selected date and session.\n\nStep 1: Sign in and open [Booking](/book).\n\nStep 2: Choose the date and either the morning or night session.\n\nStep 3: View the doctors and time slots loaded from the clinic scheduling database.\n\nStep 4: Select **Any available doctor** if you do not need a specific doctor.",
                _options.Disclaimer,
                [WebsiteGuideSource(), PublicClinicSource()]);
            return true;
        }

        var mentionsClinic = ContainsAny(normalized, "clinic", "careplus", "诊所");
        if (mentionsClinic && ContainsAny(normalized, "where", "location", "address", "direction", "map", "在哪里", "地址", "路线"))
        {
            response = new ChatResponse(
                isChinese
                    ? "CarePlus 诊所位于 **Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur, Wilayah Persekutuan Kuala Lumpur**。您可以打开 [联系我们](/contact) 页面查看地图或使用 Google Maps 导航。"
                    : "CarePlus Clinic is located at **Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur, Wilayah Persekutuan Kuala Lumpur**. Open [Contact us](/contact) to view the map or open the location in Google Maps.",
                _options.Disclaimer,
                [WebsiteGuideSource(), PublicClinicSource()]);
            return true;
        }

        if (mentionsClinic && ContainsAny(normalized, "contact", "call", "phone", "telephone", "email", "联系", "电话", "电邮", "邮箱"))
        {
            response = new ChatResponse(
                isChinese
                    ? "您可以通过以下方式联系 CarePlus 诊所：\n\n- **电话：** +60 3-4141 8888\n- **电邮：** careplus.noreply@gmail.com\n- **地址：** Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur\n\n您也可以打开 [联系我们](/contact) 页面查看地图、营业时间和常见问题。"
                    : "You can contact CarePlus Clinic using:\n\n- **Phone:** +60 3-4141 8888\n- **Email:** careplus.noreply@gmail.com\n- **Address:** Jalan Genting Kelang, Setapak, 53300 Kuala Lumpur\n\nYou can also open [Contact us](/contact) for the map, clinic hours, and frequently asked questions.",
                _options.Disclaimer,
                [WebsiteGuideSource(), PublicClinicSource()]);
            return true;
        }

        var asksHowToBook = ContainsAny(
            normalized,
            "how do i make a booking",
            "how to make a booking",
            "how do i book",
            "how to book",
            "book an appointment",
            "make booking",
            "怎么预约",
            "如何预约",
            "如何挂号");
        if (asksHowToBook)
        {
            response = new ChatResponse(
                isChinese
                    ? "Step 1: 登录 CarePlus 并打开 [预约](/book)。\n\nStep 2: 选择就诊者、日期以及上午或夜间时段。\n\nStep 3: 选择指定医生或 **任何有空的医生**。\n\nStep 4: 选择就诊类型和可用时间。\n\nStep 5: 检查预约摘要并确认。\n\n完成后，预约会显示在 [即将到来的就诊](/visits)。"
                    : "Step 1: Sign in to CarePlus and open [Booking](/book).\n\nStep 2: Choose who the visit is for, the date, and the morning or night session.\n\nStep 3: Select a doctor or **Any available doctor**.\n\nStep 4: Choose the visit type and an available time slot.\n\nStep 5: Review the booking summary and confirm.\n\nThe appointment will then appear under [Upcoming visits](/visits).",
                _options.Disclaimer,
                [WebsiteGuideSource()]);
            return true;
        }

        response = default!;
        return false;
    }

    private static string ExpandRetrievalQuery(string question)
    {
        var normalized = question.ToLowerInvariant();
        if (ContainsAny(normalized, "book", "booking", "appointment", "visit", "slot", "预约", "挂号", "看诊"))
        {
            return $"{question}\nCarePlus appointment booking: make a booking, book a visit, choose doctor, visit type, date and available time slot.";
        }
        if (ContainsAny(normalized, "bill", "invoice", "payment", "pay", "receipt", "overdue", "账单", "付款", "收据"))
        {
            return $"{question}\nCarePlus bills, outstanding invoices, payment, late fees, payment history and receipts.";
        }
        if (ContainsAny(normalized, "medicine", "medication", "cart", "checkout", "order", "delivery", "pickup", "药", "购物车", "订单", "配送", "取药"))
        {
            return $"{question}\nCarePlus prescribed medicine, cart, checkout, medicine order, clinic pickup, delivery and order tracking.";
        }
        if (ContainsAny(normalized, "record", "health", "blood pressure", "pdf", "diagnosis", "prescription", "记录", "健康", "血压", "诊断", "处方"))
        {
            return $"{question}\nCarePlus clinic records, personal health records, blood pressure, uploaded PDF documents and prescriptions.";
        }
        if (ContainsAny(normalized, "family", "parent", "child", "invite", "invitation", "家庭", "家人", "父母", "孩子", "邀请"))
        {
            return $"{question}\nCarePlus family profiles, parent and child relationships, invitations and family membership.";
        }
        if (ContainsAny(normalized, "account", "profile", "password", "email", "two-factor", "2fa", "address", "card", "账户", "个人资料", "密码", "邮箱", "地址"))
        {
            return $"{question}\nCarePlus account profile, security, password, email verification, two-factor authentication, addresses and payment cards.";
        }
        if (ContainsAny(normalized, "notification", "reminder", "announcement", "通知", "提醒", "公告"))
        {
            return $"{question}\nCarePlus notifications, reminders, clinic announcements and notification preferences.";
        }
        if (ContainsAny(normalized, "doctor", "clinic", "contact", "hours", "location", "医生", "诊所", "联系", "营业时间", "地点"))
        {
            return $"{question}\nCarePlus doctors, clinic information, contact details, opening hours and location.";
        }
        return question;
    }

    private static bool TryAnswerSmallTalk(string question, out ChatResponse response)
    {
        var normalized = question.Trim().Trim('.', '!', '?', ',', '，', '。', '！', '？').ToLowerInvariant();
        if (GreetingPhrases.Contains(normalized))
        {
            var isChinese = ContainsChinese(question);
            response = new ChatResponse(
                isChinese
                    ? "你好！我是 CarePlus 诊所助手。我可以协助您了解预约、复诊安排、诊所记录、个人健康记录、处方药、订单、账单、付款、家庭档案、通知、医生和诊所资讯。请问有什么可以帮您？"
                    : "Hi! I’m the CarePlus Clinic assistant. I can help with appointments, upcoming visits, clinic records, personal health records, prescribed medicine, orders, bills, payments, family profiles, notifications, doctors, and clinic information. What would you like help with?",
                string.Empty,
                []);
            return true;
        }

        if (ThanksPhrases.Contains(normalized))
        {
            response = new ChatResponse(
                ContainsChinese(question) ? "不客气！如果您还想了解 CarePlus 的任何功能，请告诉我。" : "You’re welcome! Let me know if you need help with anything else in CarePlus.",
                string.Empty,
                []);
            return true;
        }

        if (GoodbyePhrases.Contains(normalized))
        {
            response = new ChatResponse(
                ContainsChinese(question) ? "再见！如有需要，随时回来询问 CarePlus 诊所助手。" : "Goodbye! You can return anytime if you need help with CarePlus.",
                string.Empty,
                []);
            return true;
        }

        response = default!;
        return false;
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsChinese(string value) => value.Any(character => character is >= '\u4e00' and <= '\u9fff');

    private static readonly HashSet<string> GreetingPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "hi", "hello", "hey", "hi there", "hello there", "good morning", "good afternoon", "good evening",
        "你好", "您好", "嗨", "早安", "早上好", "下午好", "晚上好"
    };

    private static readonly HashSet<string> ThanksPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "thanks", "thank you", "thank you very much", "thx", "谢谢", "谢谢你", "多谢"
    };

    private static readonly HashSet<string> GoodbyePhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "bye", "goodbye", "see you", "see you later", "再见", "拜拜"
    };

    public async Task<IndexKnowledgeResponse> IndexAsync(IndexKnowledgeRequest request, CancellationToken cancellationToken)
    {
        var textChunks = KnowledgeChunker.Split(request.Content);
        if (textChunks.Count == 0)
        {
            throw new ArgumentException("Knowledge content cannot be empty.");
        }

        var chunks = textChunks.Select((content, index) => new KnowledgeChunk(index, content)).ToArray();
        var document = new KnowledgeDocument(request.DocumentId.Trim(), request.Title.Trim(), request.Source.Trim());
        await _knowledgeStore.ReplaceDocumentAsync(document, chunks, cancellationToken);
        return new IndexKnowledgeResponse(document.Id, chunks.Length);
    }

    public async Task<IReadOnlyList<IndexKnowledgeResponse>> IndexBundledKnowledgeAsync(CancellationToken cancellationToken)
    {
        var folder = Path.Combine(_environment.ContentRootPath, "KnowledgeBase");
        if (!Directory.Exists(folder))
        {
            return [];
        }

        var results = new List<IndexKnowledgeResponse>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.md", SearchOption.TopDirectoryOnly))
        {
            var id = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            results.Add(await IndexAsync(new IndexKnowledgeRequest
            {
                DocumentId = id,
                Title = id.Replace('-', ' '),
                Source = $"CarePlus knowledge base/{Path.GetFileName(path)}",
                Content = content
            }, cancellationToken));
        }
        return results;
    }

    private static IReadOnlyList<KnowledgeMatch> LimitContext(IReadOnlyList<KnowledgeMatch> matches, int maximumCharacters)
    {
        var maximum = Math.Clamp(maximumCharacters, 1_000, 20_000);
        var selected = new List<KnowledgeMatch>();
        var used = 0;
        foreach (var match in matches)
        {
            if (used + match.Content.Length > maximum && selected.Count > 0)
            {
                break;
            }
            selected.Add(match);
            used += match.Content.Length;
        }
        return selected;
    }
}

