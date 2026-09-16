using Domain.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Tools.Seeding;

/// <summary>Write-side persistence contract used by the tool-forms seeder.</summary>
public interface IToolFormsDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Seeds one <see cref="ToolForm"/> per tool. The schema JSON is the *form definition*
///     the pure-UI client renders: defaults, field metadata and static reference tables.
///     Idempotent — existing tool codes are skipped.
/// </summary>
public static class ToolFormsSeeder
{
    public static async Task SeedAsync(IToolFormsDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var existing = await db.Set<ToolForm>().Select(t => t.ToolCode).ToListAsync(ct);

        var forms = Definitions().Where(d => !existing.Contains(d.Code)).ToList();
        if (forms.Count == 0)
        {
            logger.LogInformation("Tool forms seed already present, skipping");
            return;
        }

        foreach (var d in forms)
            db.Set<ToolForm>().Add(ToolForm.Create(d.Code, d.Title, d.Description, d.Kind, d.Schema, d.Sort));

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} tool form definitions", forms.Count);
    }

    private static string Json(object o) => System.Text.Json.JsonSerializer.Serialize(o);

    private static (string Code, string Title, string? Description, ToolKind Kind, string Schema, int Sort)[] Definitions() =>
    [
        // ─────────────────────────── Question tools ───────────────────────────
        ("IDEA-ASSESS", "ایده‌سنج", "ارزیابی پتانسیل ایده کسب‌وکار در ۵ بخش", ToolKind.Assessment,
            Json(new
            {
                sections = new object[]
                {
                    new { key = "innovation", title = "نوآوری", maxScore = 25, questions = new[] { "innovation_q1", "innovation_q2", "innovation_q3", "innovation_q4", "innovation_q5" } },
                    new { key = "team", title = "تیم", maxScore = 30, questions = new[] { "team_q1", "team_q2", "team_q3", "team_q4", "team_q5", "team_q6" } },
                    new { key = "technology", title = "فناوری", maxScore = 30, questions = new[] { "technology_q1", "technology_q2", "technology_q3", "technology_q4", "technology_q5", "technology_q6" } },
                    new { key = "market", title = "بازار", maxScore = 25, questions = new[] { "market_q1", "market_q2", "market_q3", "market_q4", "market_q5" } },
                    new { key = "financial", title = "مالی", maxScore = 25, questions = new[] { "financial_q1", "financial_q2", "financial_q3", "financial_q4", "financial_q5" } },
                },
                questions = IdeaQuestions(),
                scale = new[] { 1, 2, 3, 4, 5 },
                maxTotal = 135,
            }), 1),

        ("INNOV-READ", "KTH Innovation Readiness", "سنجش آمادگی نوآوری در ۹ بعد", ToolKind.Assessment,
            Json(new
            {
                indicators = new object[]
                {
                    new { key = "crl", code = "CRL", color = "#2563eb", title = "آمادگی مشتری" },
                    new { key = "trl", code = "TRL", color = "#7c3aed", title = "آمادگی فناوری" },
                    new { key = "brl", code = "BRL", color = "#db2777", title = "آمادگی کسب‌وکار" },
                    new { key = "iprl", code = "IPRL", color = "#0891b2", title = "آمادگی مالکیت فکری" },
                    new { key = "tmrl", code = "TmRL", color = "#16a34a", title = "آمادگی تیم" },
                    new { key = "frl", code = "FRL", color = "#ea580c", title = "آمادگی تأمین مالی" },
                    new { key = "lrl", code = "LRL", color = "#9333ea", title = "آمادگی قانونی" },
                    new { key = "scrl", code = "SCRL", color = "#ca8a04", title = "آمادگی اجتماعی" },
                    new { key = "srl", code = "SRL", color = "#0f766e", title = "آمادگی پایداری" },
                },
                scale = 10,
            }), 2),

        ("IP-AUDIT", "بازرسی مالکیت فکری (IP Audit)", "ثبت دارایی‌های IP و محاسبه ریسک", ToolKind.Assessment,
            Json(new
            {
                jurisdictions = new[] { "ایران", "PCT", "آمریکا", "اروپا", "چین", "امارات", "عمان", "سایر" },
                riskScores = new[] { new { val = 0, label = "۰ - بهینه / شفاف" }, new { val = 1, label = "۱ - مشکل جزئی" }, new { val = 2, label = "۲ - شناسایی خلأ" }, new { val = 3, label = "۳ - ریسک مهم" }, new { val = 4, label = "۴ - نقص جدی" }, new { val = 5, label = "۵ - خطر بحرانی" } },
                riskWeights = new { r_ownership = 0.30, r_protection = 0.25, r_infringement = 0.20, r_compliance = 0.15, r_impact = 0.10 },
                categories = new[] { "trademarks", "copyrights", "patents", "domains", "contracts", "actions" },
            }), 3),

        ("JOB-EVAL", "ارزیابی ارزش‌های شغلی", "کشف ارزش‌های کاری و شغل مناسب", ToolKind.Assessment,
            Json(new
            {
                values = new object[]
                {
                    new { id = 1, title = "آزادی", tags = new[] { "creative", "autonomy" }, desc = "استقلال در تصمیم‌گیری" },
                    new { id = 2, title = "امنیت", tags = new[] { "stability", "structure" }, desc = "ثبات و عدم ریسک" },
                    new { id = 3, title = "موفقیت", tags = new[] { "achievement", "leadership" }, desc = "دستیابی به اهداف" },
                    new { id = 4, title = "لذت", tags = new[] { "hedonism", "creative" }, desc = "شادی و لذت بردن" },
                    new { id = 5, title = "خلاقیت", tags = new[] { "creative", "autonomy" }, desc = "ایده‌پردازی و نوآوری" },
                    new { id = 6, title = "همدلی", tags = new[] { "social", "people" }, desc = "درک احساسات دیگران" },
                    new { id = 7, title = "احترام", tags = new[] { "social", "ethics" }, desc = "احترام متقابل" },
                    new { id = 8, title = "قدرت", tags = new[] { "leadership", "achievement" }, desc = "تاثیرگذاری و کنترل" },
                    new { id = 9, title = "تعلق", tags = new[] { "social", "stability" }, desc = "عضویت در گروه" },
                    new { id = 10, title = "معنویت", tags = new[] { "intellectual", "ethics" }, desc = "ارتباط با معنا" },
                    new { id = 11, title = "شجاعت", tags = new[] { "autonomy", "leadership" }, desc = "ریسک‌پذیری" },
                    new { id = 12, title = "دانش", tags = new[] { "intellectual", "structure" }, desc = "یادگیری مداوم" },
                    new { id = 13, title = "زیبایی", tags = new[] { "creative", "hedonism" }, desc = "قدردانی از ظرافت‌ها" },
                    new { id = 14, title = "عدالت", tags = new[] { "ethics", "social" }, desc = "انصاف و برابری" },
                    new { id = 15, title = "سلامتی", tags = new[] { "stability", "hedonism" }, desc = "تندرستی" },
                    new { id = 16, title = "صداقت", tags = new[] { "ethics", "structure" }, desc = "راستگویی" },
                },
            }), 4),

        ("WIPO-DIAG", "WIPO Diagnostics", "ارزیابی وضعیت مالکیت فکری (WIPO)", ToolKind.Assessment,
            Json(new { sections = new[] { "trademark", "confidential", "designs", "inventive", "employment", "website" } }), 5),

        ("IPSCORE", "IPscore — ارزیابی اختراعات", "ارزیابی پورتفوی پتنت (۵ دسته سوال + NPV)", ToolKind.Assessment,
            Json(new
            {
                scoreOptions = new[] { new { val = 1, text = "۱ - بسیار ضعیف / نامناسب" }, new { val = 2, text = "۲ - ضعیف" }, new { val = 3, text = "۳ - متوسط" }, new { val = 4, text = "۴ - خوب" }, new { val = 5, text = "۵ - بسیار عالی / قوی" } },
                groups = new[] { new { key = "A", count = 8 }, new { key = "B", count = 9 }, new { key = "C", count = 9 }, new { key = "D", count = 6 }, new { key = "E", count = 8 } },
            }), 6),

        // ─────────────────────────── Calculators ───────────────────────────
        ("STARTUP-VAL", "ارزش‌گذاری استارتاپ", "۹ روش ارزش‌گذاری با وزن مرحله‌ای", ToolKind.Calculator,
            Json(new
            {
                currency = "میلیارد تومان",
                stages = new[] { "Pre-Seed", "Seed", "Series A", "Series B+" },
                defaults = new
                {
                    rev0 = 20, growth = 0.6, ebitdaMargin = 0.15, fcfConv = 0.8, wacc = 0.35,
                    ltGrowth = 0.10, capitalNeeded = 30, irr = 0.5, yearsToExit = 5, exitMultiple = 4,
                    compAvgValue = 80, revMultipleIndustry = 4, ebitdaMultipleIndustry = 12,
                    riskFreeRate = 0.22, beta = 1.2, equityRiskPremium = 0.08, sizePremium = 0.03, specificRiskPremium = 0.02,
                },
            }), 10),

        ("BRAND-VAL", "ارزش‌گذاری برند", "RFR + Premium Profit + ICF + سناریو + مونت‌کارلو", ToolKind.Calculator,
            Json(new
            {
                currency = "میلیارد تومان",
                industries = new object[]
                {
                    new { name = "مواد غذایی و نوشیدنی", low = 0.02, baseRoyalty = 0.05, high = 0.08 },
                    new { name = "پوشاک و مد", low = 0.03, baseRoyalty = 0.07, high = 0.12 },
                    new { name = "لوازم آرایشی و بهداشتی", low = 0.03, baseRoyalty = 0.06, high = 0.10 },
                    new { name = "دارویی و سلامت مصرفی", low = 0.02, baseRoyalty = 0.05, high = 0.09 },
                    new { name = "نرم‌افزار و SaaS", low = 0.02, baseRoyalty = 0.05, high = 0.10 },
                    new { name = "خدمات مالی", low = 0.01, baseRoyalty = 0.04, high = 0.08 },
                    new { name = "هتل و گردشگری", low = 0.03, baseRoyalty = 0.06, high = 0.10 },
                    new { name = "خرده‌فروشی", low = 0.01, baseRoyalty = 0.04, high = 0.07 },
                    new { name = "لوازم خانگی", low = 0.02, baseRoyalty = 0.05, high = 0.08 },
                    new { name = "خودرو و قطعات", low = 0.01, baseRoyalty = 0.04, high = 0.08 },
                    new { name = "مصالح ساختمانی", low = 0.01, baseRoyalty = 0.03, high = 0.06 },
                    new { name = "فناوری و الکترونیک", low = 0.02, baseRoyalty = 0.05, high = 0.10 },
                    new { name = "کالای لوکس", low = 0.05, baseRoyalty = 0.10, high = 0.15 },
                    new { name = "خدمات حرفه‌ای", low = 0.01, baseRoyalty = 0.03, high = 0.07 },
                    new { name = "سایر / قابل تنظیم", low = 0.01, baseRoyalty = 0.05, high = 0.12 },
                },
                defaults = new { taxRate = 0.20, discountRate = 0.30, terminalGrowth = 0.05, legalWeight = 0.15, brandWeight = 0.85, noBrandFactor = 0.70, shock = 0.10 },
            }), 11),

        ("TRADEMARK-VAL", "ارزش‌گذاری علامت تجاری", "۷ روش + مونت‌کارلو + تلفیق وزنی", ToolKind.Calculator,
            Json(new
            {
                currency = "ریال",
                defaults = new { termYears = 10, growthRate = 0.2, taxRate = 0.2, discountRate = 0.25, royaltyRate = 0.02, attributionRate = 0.7, legalRisk = 1 },
            }), 12),

        ("PATENT-VAL", "ارزش‌گذاری پتنت (عمومی)", "DCF + RFR + Black-Scholes + دوجمله‌ای + مونت‌کارلو", ToolKind.Calculator,
            Json(new
            {
                currency = "میلیون تومان",
                sectors = new[] { "نرم‌افزار/فناوری اطلاعات", "دارو/بیوتکنولوژی", "الکترونیک/نیمه‌هادی", "خودرو", "کالاهای صنعتی/تجهیزات", "عمومی صنعتی/فناوری (میانه همه صنایع)" },
                defaults = new { trl = 7, remainingLife = 15, taxRate = 0.25, growthRate = 0.05, volatility = 0.4, riskFreeRate = 0.25, optionHorizon = 5, binomialSteps = 5 },
            }), 13),

        ("PHARMA-IP", "ارزش‌گذاری پتنت دارویی", "همان موتور پتنت عمومی با پیش‌فرض‌های دارویی", ToolKind.Calculator,
            Json(new
            {
                currency = "میلیون تومان",
                sectors = new[] { "دارو/بیوتکنولوژی" },
                defaults = new { trl = 6, remainingLife = 12, taxRate = 0.25, growthRate = 0.06, volatility = 0.55, riskFreeRate = 0.25, optionHorizon = 8, binomialSteps = 6 },
            }), 14),

        ("INTANGIBLE", "دارایی‌های نامشهود", "R&D + سرمایه انسانی + دارایی داده + مونت‌کارلو", ToolKind.Calculator,
            Json(new
            {
                currency = "میلیون تومان",
                defaults = new { discountRate = 0.30, ltGrowth = 0.05, taxRate = 0.20 },
            }), 15),

        ("KNOWHOW", "ارزش‌گذاری دانش فنی (Know-How)", "بازتولید/جایگزینی + RFR + بازار + کارت امتیازی", ToolKind.Calculator,
            Json(new
            {
                currency = "میلیون ریال",
                defaults = new { overheadPct = 35, developerProfitPct = 15, entrepreneurIncentivePct = 10, functionalObsolescencePct = 15, economicObsolescencePct = 5, discountRate = 22, royaltyRate = 4 },
            }), 16),
    ];

    private static object[] IdeaQuestions() =>
    [
        new { id = "innovation_q1", section = "innovation", title = "ایده شما چقدر در مقایسه با رقبا متمایز است؟" },
        new { id = "innovation_q2", section = "innovation", title = "راه حل ایده شما چقدر مشکل را بهبود می‌بخشد؟" },
        new { id = "innovation_q3", section = "innovation", title = "ایده شما چقدر از فناوری‌های جدید استفاده می‌کند؟" },
        new { id = "innovation_q4", section = "innovation", title = "ایده شما چقدر پتانسیل ایجاد بازار جدید دارد؟" },
        new { id = "innovation_q5", section = "innovation", title = "ایده شما چقدر قابل کپی‌برداری توسط رقبا است؟" },
        new { id = "team_q1", section = "team", title = "تجربه تیم شما در صنعت مرتبط چقدر است؟" },
        new { id = "team_q2", section = "team", title = "توانمندی‌های فنی تیم شما چقدر است؟" },
        new { id = "team_q3", section = "team", title = "چسبندگی تیم (Team Cohesion) چقدر است؟" },
        new { id = "team_q4", section = "team", title = "دسترسی تیم به مشاوران و متخصصان چقدر است؟" },
        new { id = "team_q5", section = "team", title = "تعهد تیم به موفقیت پروژه چقدر است؟" },
        new { id = "team_q6", section = "team", title = "تنوع تخصص‌ها در تیم چقدر است؟" },
        new { id = "technology_q1", section = "technology", title = "پیچیدگی فنی ایده شما چقدر است؟" },
        new { id = "technology_q2", section = "technology", title = "موانع فنی برای اجرای ایده چقدر است؟" },
        new { id = "technology_q3", section = "technology", title = "پایداری فناوری در بلندمدت چقدر است؟" },
        new { id = "technology_q4", section = "technology", title = "قابلیت توسعه‌پذیری فناوری چقدر است؟" },
        new { id = "technology_q5", section = "technology", title = "هزینه توسعه فناوری چقدر است؟" },
        new { id = "technology_q6", section = "technology", title = "امنیت و قابلیت اطمینان فناوری چقدر است؟" },
        new { id = "market_q1", section = "market", title = "اندازه بازار هدف چقدر است؟" },
        new { id = "market_q2", section = "market", title = "رشد بازار هدف چقدر است؟" },
        new { id = "market_q3", section = "market", title = "شدت نیاز مشتری به راه حل شما چقدر است؟" },
        new { id = "market_q4", section = "market", title = "توانایی پرداخت مشتریان چقدر است؟" },
        new { id = "market_q5", section = "market", title = "تکرارپذیری خرید مشتریان چقدر است؟" },
        new { id = "financial_q1", section = "financial", title = "پتانسیل سودآوری ایده شما چقدر است؟" },
        new { id = "financial_q2", section = "financial", title = "نرخ بازگشت سرمایه (ROI) چقدر است؟" },
        new { id = "financial_q3", section = "financial", title = "ریسک مالی پروژه چقدر است؟" },
        new { id = "financial_q4", section = "financial", title = "هزینه‌های ثابت و متغیر چقدر است؟" },
        new { id = "financial_q5", section = "financial", title = "پتانسیل سودآوری در بلندمدت چقدر است؟" },
    ];
}
