using System.Globalization;
using System.Text.RegularExpressions;
using CarePlus.API.Data;
using CarePlus.API.DTO.ChatDTO;
using CarePlus.API.Services.ChatServices.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarePlus.API.Services.ChatServices;

public sealed class ChatServices : IChatServices
{
    private readonly IVectorKnowledgeStore _knowledgeStore;
    private readonly IGroqChatClient _groqClient;
    private readonly ChatOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly AppDbContext _db;

    public ChatServices(
        IVectorKnowledgeStore knowledgeStore,
        IGroqChatClient groqClient,
        IOptions<ChatOptions> options,
        IWebHostEnvironment environment,
        AppDbContext db)
    {
        _knowledgeStore = knowledgeStore;
        _groqClient = groqClient;
        _options = options.Value;
        _environment = environment;
        _db = db;
    }

    public async Task<ChatResponse> AnswerAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var question = request.Message.Trim();
        var wordCount = question.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (question.Length > 500 || wordCount > 80)
        {
            return new ChatResponse(
                "Please keep your message under 80 words.",
                _options.Disclaimer,
                Array.Empty<ChatSourceResponse>());
        }
        if (TryAnswerCommonWebsiteQuestion(question, out var commonResponse))
        {
            return commonResponse;
        }
        if (TryAnswerSmallTalk(question, out var smallTalkResponse))
        {
            return smallTalkResponse;
        }

        var clinicMedicine = await TryAnswerClinicMedicineAsync(question, cancellationToken);
        if (clinicMedicine != null)
        {
            return clinicMedicine;
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
        if (ContainsAny(normalized, "what can you do", "what can i do", "what can this", "what are you able to do", "how can you help", "你能做什么", "我可以做什么", "你能帮我什么", "这个网站能做什么"))
        {
            response = new ChatResponse(
                isChinese
                    ? "登录后，您可以在 CarePlus 做这些事：\n\n- 在 [预约](/book) 挂号，并在 [即将到来的就诊](/visits) 查看、改期或取消。\n- 在 [医生](/doctors) 查看医生资料。\n- 阅读 [诊所记录](/clinic-records)，并在 [个人健康记录](/records) 添加自己的记录。\n- 在 [药品](/medicine) 订购处方药，选择到诊所取药或配送。\n- 在 [未付账单](/invoices) 付款，并在 [付款记录](/payments) 下载收据。\n- 管理 [家庭档案](/family)，并查看 [通知](/notifications)。\n- 在 [账户](/account) 更新个人资料、付款卡和密码。\n\n未登录时可以查看医生、公告、[联系我们](/contact)、[注册](/register) 和 [登录](/login)。我不能诊断症状，也不能打开您的私人记录。"
                    : "After you sign in, you can:\n\n- Book, reschedule, or cancel a visit in [Booking](/book) and [Upcoming visits](/visits).\n- Look up doctors in [Doctors](/doctors).\n- Read [Clinic records](/clinic-records) and add your own [Personal health records](/records).\n- Order prescribed medicine for pickup or delivery in [Medicine](/medicine).\n- Pay a bill in [Outstanding bill](/invoices) and open [Payment history](/payments).\n- Manage [Family profiles](/family) and read [Notifications](/notifications).\n- Update your profile, cards, and password in [Account](/account).\n\nBefore login, you can browse doctors, announcements, [Contact us](/contact), [Register](/register), and [Login](/login). I can't diagnose symptoms or open your private records.",
                _options.Disclaimer,
                [WebsiteGuideSource()]);
            return true;
        }

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

    private async Task<ChatResponse?> TryAnswerClinicMedicineAsync(string question, CancellationToken cancellationToken)
    {
        var rows = await _db.Medicines.AsNoTracking()
            .OrderBy(row => row.MedicineName)
            .Select(row => new
            {
                row.MedicineName,
                row.NameZh,
                row.Pack,
                row.PackZh,
                row.Description,
                row.DescriptionZh,
                row.Method,
                row.MethodZh,
                row.Price,
                row.RequiresPrescription,
                row.Status,
            })
            .ToListAsync(cancellationToken);
        var catalog = rows.Select(row => new ClinicMedicine(
            row.MedicineName,
            row.NameZh,
            row.Pack,
            row.PackZh,
            row.Description,
            row.DescriptionZh,
            row.Method,
            row.MethodZh,
            row.Price,
            row.RequiresPrescription,
            row.Status == "Available")).ToList();
        if (!TryAnswerClinicMedicine(question, catalog, out var answer))
        {
            return null;
        }

        return new ChatResponse(answer, _options.Disclaimer, [new ChatSourceResponse(
            "CarePlus clinic medicine list",
            "CarePlus medicines uploaded for the clinic",
            1)]);
    }

    private readonly record struct ClinicMedicine(
        string Name,
        string? NameZh,
        string Pack,
        string? PackZh,
        string Description,
        string? DescriptionZh,
        string Method,
        string? MethodZh,
        decimal Price,
        bool RequiresPrescription,
        bool Available);

    private static readonly string[] MedicineNameSkip = ["tablet", "tablets", "capsule", "capsules", "syrup", "pack", "oral", "suspension", "drops", "mg"];

    private static bool TryAnswerClinicMedicine(string question, IReadOnlyList<ClinicMedicine> catalog, out string answer)
    {
        answer = "";
        var normalized = question.Trim().Trim('.', '!', '?', ',', '，', '。', '！', '？').ToLowerInvariant();
        var isChinese = ContainsChinese(question);
        var hits = catalog.Where(item => NameMatches(normalized, item.Name) || NameMatches(normalized, item.NameZh)).Take(3).ToList();
        var asksList = ContainsAny(normalized, "what medicine", "which medicine", "medicines do you", "medicines can i", "what can i buy", "list of medicine", "有什么药", "哪些药", "有哪些药", "可以买什么");
        var asksBuy = ContainsAny(normalized, "buy", "purchase", "买", "购买");
        var asksLimit = ContainsAny(normalized, "how many", "limit", "maximum", "最多", "几盒", "多少");
        var asksDose = ContainsAny(normalized, "dose", "dosage", "what is it for", "used for", "should i take", "how do i take", "吃", "服用", "剂量", "用来", "作用");
        var medicineWord = ContainsAny(normalized, "medicine", "medication", "drug", "药");
        var askingCatalog = asksList || asksBuy || ((asksLimit || asksDose) && (medicineWord || hits.Count > 0)) || hits.Count > 0;
        if (!askingCatalog)
        {
            return false;
        }

        if (hits.Count == 0)
        {
            answer = asksList
                ? ListMedicines(catalog, isChinese)
                : isChinese
                    ? "诊所目前上传的药品里没有这个。请打开 [药品](/medicine) 查看现有药品。同一种药在同一个月内最多买 5 个单位。"
                    : "That medicine is not in the list uploaded for the clinic. Open [Medicine](/medicine) to see what is available. Each medicine is capped at 5 units in one calendar month.";
            return true;
        }

        var overLimit = OverMonthlyLimit(normalized, hits);

        var lines = hits.Select(item => DescribeMedicine(item, isChinese, overLimit, asksDose)).ToArray();
        var lead = overLimit
            ? (isChinese ? "不可以。同一个月内，同一种药最多买 5 个单位。" : "No. The same patient can buy at most 5 units of the same medicine in one calendar month.")
            : "";
        answer = string.Join("\n\n", new[] { lead }.Concat(lines).Where(line => line.Length > 0));
        return true;
    }

    private static string ListMedicines(IReadOnlyList<ClinicMedicine> catalog, bool isChinese)
    {
        var available = catalog.Where(item => item.Available).ToList();
        if (available.Count == 0)
        {
            return isChinese
                ? "诊所还没有上传可购买的药品。请稍后再打开 [药品](/medicine)，或联系诊所。"
                : "The clinic has not uploaded any medicine that can be ordered yet. Open [Medicine](/medicine) later, or contact the clinic.";
        }

        var shown = available.Take(20).Select(item =>
        {
            var name = isChinese && !string.IsNullOrWhiteSpace(item.NameZh) ? item.NameZh! : item.Name;
            var pack = isChinese && !string.IsNullOrWhiteSpace(item.PackZh) ? item.PackZh! : item.Pack;
            return $"- {name} — {pack} — RM {item.Price.ToString("0.00", CultureInfo.InvariantCulture)}";
        });
        var more = available.Count > 20
            ? (isChinese ? $"\n\n还有 {available.Count - 20} 种，请打开 [药品](/medicine) 查看。" : $"\n\n{available.Count - 20} more are on [Medicine](/medicine).")
            : (isChinese ? "\n\n打开 [药品](/medicine) 可以查看详情并加入 [购物车](/cart)。" : "\n\nOpen [Medicine](/medicine) for details, then add a medicine to the [Cart](/cart).");
        var intro = isChinese
            ? "诊所目前上传了这些药品。同一种药在同一个月内最多买 5 个单位："
            : "These are the medicines uploaded for the clinic. Each one is capped at 5 units in one calendar month:";
        return intro + "\n\n" + string.Join("\n", shown) + more;
    }

    private static string DescribeMedicine(ClinicMedicine item, bool isChinese, bool overLimit, bool asksDose)
    {
        var name = isChinese && !string.IsNullOrWhiteSpace(item.NameZh) ? item.NameZh! : item.Name;
        var pack = isChinese && !string.IsNullOrWhiteSpace(item.PackZh) ? item.PackZh! : item.Pack;
        var price = item.Price.ToString("0.00", CultureInfo.InvariantCulture);
        if (!item.Available)
        {
            return isChinese
                ? $"{name} 已在诊所名单里，但现在不能下单。请打开 [药品](/medicine) 查看可购买的药品。"
                : $"{name} is on the clinic list, but it cannot be ordered right now. Open [Medicine](/medicine) for medicines that are available.";
        }

        var prescription = item.RequiresPrescription
            ? (isChinese ? " 这是处方药，需要医生先开药。" : " A doctor has to prescribe it before the clinic can supply it.")
            : "";
        if (asksDose)
        {
            var note = isChinese && !string.IsNullOrWhiteSpace(item.DescriptionZh) ? item.DescriptionZh! : item.Description;
            var method = isChinese && !string.IsNullOrWhiteSpace(item.MethodZh) ? item.MethodZh! : item.Method;
            var how = string.IsNullOrWhiteSpace(method) ? "" : (isChinese ? $" 诊所标注的用法：{method}。" : $" The clinic lists the directions as: {method}.");
            return isChinese
                ? $"{name}：{note}{how} 一个单位是 {pack}，RM {price}。这是诊所上传的说明，不是针对个人的剂量。同一种药一个月最多买 5 个单位。请打开 [药品](/medicine)。{prescription}"
                : $"{name}: {note}{how} One unit is {pack}, RM {price}. This is the note uploaded by the clinic, not a personal dose. The monthly cap is 5 units. Open [Medicine](/medicine).{prescription}";
        }

        return isChinese
            ? $"{name} 在 [药品](/medicine) 里。一个单位是 {pack}，RM {price}。{(overLimit ? "这个数量超过了每月 5 个单位的限制。" : "若还有库存，同一个月最多买 5 个单位。")}请登录后加入 [购物车](/cart)，再选择到诊所取药或配送。我看不到您的购物车和今天的库存。{prescription}"
            : $"{name} is listed in [Medicine](/medicine). One unit is {pack}, RM {price}. {(overLimit ? "That quantity is over the monthly limit of 5 units." : "If it is in stock, you can buy up to 5 units of it in one calendar month.")} Sign in, add it to the [Cart](/cart), then choose clinic pickup or delivery. I can't see your cart or today's stock.{prescription}";
    }

    private static bool NameMatches(string normalized, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var lower = name.Trim().ToLowerInvariant();
        if (normalized.Contains(lower, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var token in lower.Split([' ', '-', '/', ',', '(', ')'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length < 4 || MedicineNameSkip.Contains(token) || int.TryParse(token, out _))
            {
                continue;
            }
            if (normalized.Contains(token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OverMonthlyLimit(string normalized, IReadOnlyList<ClinicMedicine> hits)
    {
        var inName = hits
            .SelectMany(item => Regex.Matches(item.Name, @"\d+").Select(match => int.Parse(match.Value)))
            .ToHashSet();
        foreach (Match match in Regex.Matches(normalized, @"\d+"))
        {
            var count = int.Parse(match.Value);
            if (inName.Contains(count))
            {
                continue;
            }
            var after = normalized[(match.Index + match.Length)..].TrimStart();
            if (after.StartsWith("mg", StringComparison.Ordinal) || after.StartsWith("毫克", StringComparison.Ordinal))
            {
                continue;
            }
            if (count > 5)
            {
                return true;
            }
        }

        return false;
    }

    static ChatServices()
    {
        var sample = new ClinicMedicine[]
        {
            new("Paracetamol 500 mg tablets", "扑热息痛", "20 tablets", "20 片", "For fever or pain.", null, "1 to 2 tablets", null, 8.90m, false, true),
            new("Vitamin C drops", "维生素C", "10 bottles", null, "A supplement.", null, "Once a day", null, 12m, false, true),
        };
        if (!TryAnswerClinicMedicine("can i buy 6 paracetamol", sample, out var over) || !over.StartsWith("No.", StringComparison.Ordinal) || !over.Contains("Paracetamol", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Medicine answer missed an over-limit catalog question.");
        }
        if (!TryAnswerClinicMedicine("can i buy 2 vitamin c", sample, out var added) || !added.Contains("Vitamin C", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Medicine answer missed an uploaded medicine.");
        }
        if (!TryAnswerClinicMedicine("what medicines do you have", sample, out var list) || !list.Contains("Vitamin C", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Medicine answer missed the uploaded list.");
        }
        if (TryAnswerClinicMedicine("where is the clinic", sample, out _))
        {
            throw new InvalidOperationException("Medicine answer matched an unrelated question.");
        }
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
        if (ContainsAny(normalized, "medicine", "medication", "cart", "checkout", "order", "delivery", "pickup", "buy", "purchase", "paracetamol", "panadol", "amoxicillin", "loratadine", "ibuprofen", "cetirizine", "cloxacillin", "药", "购物车", "订单", "配送", "取药", "买", "扑热息痛", "阿莫西林", "布洛芬"))
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

