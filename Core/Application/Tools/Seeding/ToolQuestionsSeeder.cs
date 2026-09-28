using Domain.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Tools.Seeding;

/// <summary>
///     Seeds editable question content (texts + option labels) for the questionnaire
///     tools, mirroring the client's data files. Admins can then retitle questions and
///     option labels at runtime via the admin questions endpoints. Idempotent —
///     (toolCode, questionId) rows already present are skipped, so admin edits survive
///     restarts; newly added questions are inserted on later runs.
/// </summary>
public static class ToolQuestionsSeeder
{
    public static async Task SeedAsync(IToolFormsDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var existingPairs = await db.Set<ToolQuestion>()
            .Select(q => new { q.ToolCode, q.QuestionId })
            .ToListAsync(ct);
        var existing = existingPairs.Select(p => $"{p.ToolCode}|{p.QuestionId}").ToHashSet();

        var defs = Definitions();
        var missing = defs.Where(d => !existing.Contains($"{d.ToolCode}|{d.Id}")).ToList();
        if (missing.Count == 0)
        {
            logger.LogInformation("Tool questions seed already present, skipping");
            return;
        }

        foreach (var d in missing)
        {
            db.Set<ToolQuestion>().Add(ToolQuestion.Create(
                d.ToolCode, d.Id, d.SectionKey, d.SectionTitle, d.Text, d.OptionsJson, d.Sort));
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} tool question rows", missing.Count);
    }

    private static (string ToolCode, string Id, string SectionKey, string SectionTitle, string Text, string OptionsJson, int Sort) Def(
        string tool, string id, string section, string title, string text, int sort, string[]? options = null)
        => (tool, id, section, title, text, options is null ? "[]" : Options(options), sort);

    private static string Options(string[] labels) =>
        System.Text.Json.JsonSerializer.Serialize(
            labels.Select((l, i) => new { value = i, label = l }).ToArray());

    // ─── Content mirrors the client data files (fa) ─────────────────────

    private static (string ToolCode, string Id, string SectionKey, string SectionTitle, string Text, string OptionsJson, int Sort)[] Definitions() =>
    [
        // ── IDEA-ASSESS: 27 scored questions (scale 1..5) ──
        Def("IDEA-ASSESS", "innovation_q1", "innovation", "نوآوری", "ایده شما چقدر در مقایسه با رقبا متمایز است؟", 1),
        Def("IDEA-ASSESS", "innovation_q2", "innovation", "نوآوری", "راه حل ایده شما چقدر مشکل را بهبود می‌بخشد؟", 2),
        Def("IDEA-ASSESS", "innovation_q3", "innovation", "نوآوری", "ایده شما چقدر از فناوری‌های جدید استفاده می‌کند؟", 3),
        Def("IDEA-ASSESS", "innovation_q4", "innovation", "نوآوری", "ایده شما چقدر پتانسیل ایجاد بازار جدید دارد؟", 4),
        Def("IDEA-ASSESS", "innovation_q5", "innovation", "نوآوری", "ایده شما چقدر قابل کپی‌برداری توسط رقبا است؟", 5),
        Def("IDEA-ASSESS", "team_q1", "team", "تیم", "تجربه تیم شما در صنعت مرتبط چقدر است؟", 10),
        Def("IDEA-ASSESS", "team_q2", "team", "تیم", "توانمندی‌های فنی تیم شما چقدر است؟", 11),
        Def("IDEA-ASSESS", "team_q3", "team", "تیم", "چسبندگی تیم (Team Cohesion) چقدر است؟", 12),
        Def("IDEA-ASSESS", "team_q4", "team", "تیم", "دسترسی تیم به مشاوران و متخصصان چقدر است؟", 13),
        Def("IDEA-ASSESS", "team_q5", "team", "تیم", "تعهد تیم به موفقیت پروژه چقدر است؟", 14),
        Def("IDEA-ASSESS", "team_q6", "team", "تیم", "تنوع تخصص‌ها در تیم چقدر است؟", 15),
        Def("IDEA-ASSESS", "technology_q1", "technology", "فناوری", "پیچیدگی فنی ایده شما چقدر است؟", 20),
        Def("IDEA-ASSESS", "technology_q2", "technology", "فناوری", "موانع فنی برای اجرای ایده چقدر است؟", 21),
        Def("IDEA-ASSESS", "technology_q3", "technology", "فناوری", "پایداری فناوری در بلندمدت چقدر است؟", 22),
        Def("IDEA-ASSESS", "technology_q4", "technology", "فناوری", "قابلیت توسعه‌پذیری فناوری چقدر است؟", 23),
        Def("IDEA-ASSESS", "technology_q5", "technology", "فناوری", "هزینه توسعه فناوری چقدر است؟", 24),
        Def("IDEA-ASSESS", "technology_q6", "technology", "فناوری", "امنیت و قابلیت اطمینان فناوری چقدر است؟", 25),
        Def("IDEA-ASSESS", "market_q1", "market", "بازار", "اندازه بازار هدف چقدر است؟", 30),
        Def("IDEA-ASSESS", "market_q2", "market", "بازار", "رشد بازار هدف چقدر است؟", 31),
        Def("IDEA-ASSESS", "market_q3", "market", "بازار", "شدت نیاز مشتری به راه حل شما چقدر است؟", 32),
        Def("IDEA-ASSESS", "market_q4", "market", "بازار", "توانایی پرداخت مشتریان چقدر است؟", 33),
        Def("IDEA-ASSESS", "market_q5", "market", "بازار", "تکرارپذیری خرید مشتریان چقدر است؟", 34),
        Def("IDEA-ASSESS", "financial_q1", "financial", "مالی", "پتانسیل سودآوری ایده شما چقدر است؟", 40),
        Def("IDEA-ASSESS", "financial_q2", "financial", "مالی", "نرخ بازگشت سرمایه (ROI) چقدر است؟", 41),
        Def("IDEA-ASSESS", "financial_q3", "financial", "مالی", "ریسک مالی پروژه چقدر است؟", 42),
        Def("IDEA-ASSESS", "financial_q4", "financial", "مالی", "هزینه‌های ثابت و متغیر چقدر است؟", 43),
        Def("IDEA-ASSESS", "financial_q5", "financial", "مالی", "پتانسیل سودآوری در بلندمدت چقدر است؟", 44),

        // ── INNOV-READ: 9 readiness indicators (scale 0..10) ──
        Def("INNOV-READ", "crl", "kth", "آمادگی مشتری (CRL)", "آمادگی مشتری — سطح فعلی (۰ تا ۱۰)", 1),
        Def("INNOV-READ", "trl", "kth", "آمادگی فناوری (TRL)", "آمادگی فناوری — سطح فعلی (۰ تا ۱۰)", 2),
        Def("INNOV-READ", "brl", "kth", "آمادگی کسب‌وکار (BRL)", "آمادگی کسب‌وکار — سطح فعلی (۰ تا ۱۰)", 3),
        Def("INNOV-READ", "iprl", "kth", "آمادگی مالکیت فکری (IPRL)", "آمادگی مالکیت فکری — سطح فعلی (۰ تا ۱۰)", 4),
        Def("INNOV-READ", "tmrl", "kth", "آمادگی تیم (TmRL)", "آمادگی تیم — سطح فعلی (۰ تا ۱۰)", 5),
        Def("INNOV-READ", "frl", "kth", "آمادگی تأمین مالی (FRL)", "آمادگی تأمین مالی — سطح فعلی (۰ تا ۱۰)", 6),
        Def("INNOV-READ", "lrl", "kth", "آمادگی قانونی (LRL)", "آمادگی قانونی — سطح فعلی (۰ تا ۱۰)", 7),
        Def("INNOV-READ", "scrl", "kth", "آمادگی اجتماعی (SCRL)", "آمادگی اجتماعی — سطح فعلی (۰ تا ۱۰)", 8),
        Def("INNOV-READ", "srl", "kth", "آمادگی پایداری (SRL)", "آمادگی پایداری — سطح فعلی (۰ تا ۱۰)", 9),

        // ── WIPO-DIAG: yes/no(/not-sure) with exact client wording ──
        Def("WIPO-DIAG", "tmRegistered", "trademark", "علامت‌های تجاری", "آیا علامت تجاری در دفتر مالکیت فکری ملی یا منطقه‌ای ثبت کرده‌اید یا برای آن درخواست داده‌اید؟", 1, ["بله", "خیر"]),
        Def("WIPO-DIAG", "tmUsage", "trademark", "علامت‌های تجاری", "آیا از علامت تجاری ثبت شده خود برای بازاریابی کالاها و خدمات خود استفاده می‌کنید؟", 2, ["بله", "خیر", "مطمئن نیستم"]),
        Def("WIPO-DIAG", "tmInvestors", "trademark", "علامت‌های تجاری", "آیا به جذب سرمایه‌گذاران برای شرکت خود علاقه دارید؟", 3, ["بله", "خیر", "مطمئن نیستم"]),
        Def("WIPO-DIAG", "tmLicense", "trademark", "علامت‌های تجاری", "آیا به واگذاری حق استفاده از علامت تجاری خود به دیگری فکر کرده‌اید؟", 4, ["بله", "خیر"]),
        Def("WIPO-DIAG", "tmLicenseObtain", "trademark", "علامت‌های تجاری", "آیا به دریافت حق استفاده از علامت تجاری دیگران در کسب‌وکار خود فکر کرده‌اید؟", 5, ["بله", "خیر"]),
        Def("WIPO-DIAG", "tmInfringement", "trademark", "علامت‌های تجاری", "آیا فکر می‌کنید دیگران از علامت‌های مشابه یا یکسان برای کالاها یا خدمات مشابه بدون مجوز شما استفاده می‌کنند؟", 6, ["بله", "خیر"]),
        Def("WIPO-DIAG", "tmExternal", "trademark", "علامت‌های تجاری", "آیا علامت تجاری شما توسط شخص خارجی برای شما طراحی شده است؟", 7, ["بله", "خیر"]),
        Def("WIPO-DIAG", "confidentialProtection", "confidential", "اطلاعات محرمانه", "آیا اقدامات معقولی برای محافظت از اطلاعات کسب‌وکار خود که ارزشمند می‌دانید، انجام داده‌اید؟", 10, ["بله", "خیر", "مطمئن نیستم"]),
        Def("WIPO-DIAG", "confidentialAccess", "confidential", "اطلاعات محرمانه", "آیا دسترسی به این اطلاعات کنترل می‌شود (رمز عبور، خرد کردن اسناد، علامت‌گذاری محرمانه و غیره)؟", 11, ["بله", "خیر"]),
        Def("WIPO-DIAG", "confidentialNDA", "confidential", "اطلاعات محرمانه", "آیا از اشخاص خارجی خواسته می‌شود قراردادهای عدم افشا امضا کنند؟", 12, ["بله", "خیر"]),
        Def("WIPO-DIAG", "confidentialSystems", "confidential", "اطلاعات محرمانه", "آیا سیستم‌هایی برای تعیین دسترسی به اطلاعات محرمانه در محل قرار دارند؟", 13, ["بله", "خیر", "مطمئن نیستم"]),
        Def("WIPO-DIAG", "confidentialProduct", "confidential", "اطلاعات محرمانه", "آیا اطلاعات محرمانه در مورد محصولی که تولید می‌کنید، متجسم شده است؟", 14, ["بله", "خیر", "مطمئن نیستم"]),
        Def("WIPO-DIAG", "designRegistered", "designs", "طرح‌های صنعتی", "آیا حق طرح صنعتی در دفتر مالکیت فکری ثبت کرده‌اید یا برای آن درخواست داده‌اید؟", 20, ["بله", "خیر"]),
        Def("WIPO-DIAG", "designInvestors", "designs", "طرح‌های صنعتی", "آیا به جذب سرمایه‌گذاران برای شرکت خود علاقه دارید؟", 21, ["بله", "خیر", "مطمئن نیستم"]),
        Def("WIPO-DIAG", "designLicense", "designs", "طرح‌های صنعتی", "آیا به واگذاری حق استفاده از طرح خود به شرکتی فکر کرده‌اید؟", 22, ["بله", "خیر"]),
        Def("WIPO-DIAG", "designLicenseObtain", "designs", "طرح‌های صنعتی", "آیا به دریافت حق استفاده از طرح دیگران فکر کرده‌اید؟", 23, ["بله", "خیر"]),
        Def("WIPO-DIAG", "designInfringement", "designs", "طرح‌های صنعتی", "آیا فکر می‌کنید دیگران از طرح‌های شما روی محصولات خود بدون مجوز شما استفاده می‌کنند؟", 24, ["بله", "خیر"]),
        Def("WIPO-DIAG", "designExternal", "designs", "طرح‌های صنعتی", "آیا طرحی که استفاده می‌کنید توسط شخص خارجی برای شما توسعه داده شده است؟", 25, ["بله", "خیر"]),
        Def("WIPO-DIAG", "patentGranted", "inventive", "اختراعات و نوآوری", "آیا برای اختراع خود پتنت دریافت کرده‌اید یا در دفتر پتنت ملی، منطقه‌ای یا بین‌المللی برای آن درخواست داده‌اید؟", 30, ["بله", "خیر"]),
        Def("WIPO-DIAG", "patentDatabases", "inventive", "اختراعات و نوآوری", "آیا از پایگاه‌های داده پتنت رایگان آگاه هستید؟", 31, ["بله", "خیر"]),
        Def("WIPO-DIAG", "inventiveInvestors", "inventive", "اختراعات و نوآوری", "آیا به جذب سرمایه‌گذاران برای شرکت خود علاقه دارید؟", 32, ["بله", "خیر", "مطمئن نیستم"]),
        Def("WIPO-DIAG", "inventiveLicense", "inventive", "اختراعات و نوآوری", "آیا به واگذاری حق استفاده از فناوری خود فکر کرده‌اید؟", 33, ["بله", "خیر"]),
        Def("WIPO-DIAG", "inventiveLicenseObtain", "inventive", "اختراعات و نوآوری", "آیا به دریافت حق استفاده از فناوری دیگران فکر کرده‌اید؟", 34, ["بله", "خیر"]),
        Def("WIPO-DIAG", "inventiveInfringement", "inventive", "اختراعات و نوآوری", "آیا فکر می‌کنید دیگران از فناوری شما بدون رضایت شما استفاده می‌کنند؟", 35, ["بله", "خیر"]),
        Def("WIPO-DIAG", "inventiveExternal", "inventive", "اختراعات و نوآوری", "آیا فناوری جدید شما توسط شخص خارجی برای شما توسعه داده شده است؟", 36, ["بله", "خیر"]),
        Def("WIPO-DIAG", "employeeInventions", "employment", "کارکنان", "آیا کارکنان شما در اختراع محصولات یا فرآیندهای جدید نقش دارند؟", 40, ["بله", "خیر"]),
        Def("WIPO-DIAG", "employeeDesigns", "employment", "کارکنان", "آیا کارکنان شما در توسعه مواد خلاقانه، طرح‌ها یا علائم تجاری نقش دارند؟", 41, ["بله", "خیر"]),
        Def("WIPO-DIAG", "employeeConfidential", "employment", "کارکنان", "آیا در توافق‌نامه‌ها با کارکنان، مواردی برای حفاظت از اطلاعات محرمانه گنجانده‌اید؟", 42, ["بله", "خیر"]),
        Def("WIPO-DIAG", "employeeTraining", "employment", "کارکنان", "آیا کارکنان خود را درباره محافظت از اطلاعات محرمانه آموزش می‌دهید؟", 43, ["بله", "خیر"]),
        Def("WIPO-DIAG", "employeeHiring", "employment", "کارکنان", "آیا هنگام استخدام مطمئن می‌شوید اطلاعات تجاری کارفرمایان قبلی را نمی‌آورند؟", 44, ["بله", "خیر"]),
        Def("WIPO-DIAG", "domainRegistered", "website", "وب‌سایت", "آیا نام دامنه برای وب‌سایت خود ثبت کرده‌اید؟", 50, ["بله", "خیر"]),
        Def("WIPO-DIAG", "domainTrademark", "website", "وب‌سایت", "آیا نام دامنه شما علامت تجاری شماست؟", 51, ["بله", "خیر"]),
        Def("WIPO-DIAG", "domainTrademarkOther", "website", "وب‌سایت", "آیا نام دامنه شما شامل علامت تجاری ثبت‌شده توسط شخص دیگری است؟", 52, ["بله", "خیر"]),
        Def("WIPO-DIAG", "websiteDevelopment", "website", "وب‌سایت", "آیا وب‌سایت خود را به طور مستقل توسعه داده‌اید؟", 53, ["بله", "خیر"]),
        Def("WIPO-DIAG", "websiteMaterials", "website", "وب‌سایت", "آیا مواد آپلودشده در وب‌سایت در شرکت توسعه داده شده‌اند؟", 54, ["بله", "خیر"]),
        Def("WIPO-DIAG", "websiteLinks", "website", "وب‌سایت", "آیا وب‌سایت شما لینک‌هایی به مواد وب‌سایت‌های دیگر دارد؟", 55, ["بله", "خیر"]),
        Def("WIPO-DIAG", "websiteUsers", "website", "وب‌سایت", "آیا کاربران مواد را روی وب‌سایت شما آپلود می‌کنند؟", 56, ["بله", "خیر"]),

        // ── IPSCORE (patent-valuation): 40 scored questions, groups A–E ──
        Def("IPSCORE", "A1", "A", "A — وضعیت حقوقی", "وضعیت ثبت پتنت چگونه است؟", 1),
        Def("IPSCORE", "A2", "A", "A — وضعیت حقوقی", "موقعیت و استحکام قانونی پتنت چگونه است؟", 2),
        Def("IPSCORE", "A3", "A", "A — وضعیت حقوقی", "برای چه مدت پتنت هنوز دارای اعتبار است؟", 3),
        Def("IPSCORE", "A4", "A", "A — وضعیت حقوقی", "ادعاهای پتنت تا چه حد گسترده و جامع هستند؟", 4),
        Def("IPSCORE", "A5", "A", "A — وضعیت حقوقی", "آیا پوشش جغرافیایی پتنت شامل بازارهای هدف می‌شود؟", 5),
        Def("IPSCORE", "A6", "A", "A — وضعیت حقوقی", "آیا پایش منظم برای شناسایی نقض پتنت انجام می‌شود؟", 6),
        Def("IPSCORE", "A7", "A", "A — وضعیت حقوقی", "آیا دعاوی حقوقی در بازار مربوطه مرسوم است؟", 7),
        Def("IPSCORE", "A8", "A", "A — وضعیت حقوقی", "آیا شرکت توان پیگیری و اجرای حقوق پتنت را دارد؟", 8),
        Def("IPSCORE", "B1", "B", "B — فناوری", "آیا اختراع یک فناوری منحصر به‌فرد است؟", 10),
        Def("IPSCORE", "B2", "B", "B — فناوری", "آیا اختراع از نظر فنی بر فناوری‌های جایگزین برتری دارد؟", 11),
        Def("IPSCORE", "B3", "B", "B — فناوری", "اختراع تا چه حد تست و آزمایش شده است؟", 12),
        Def("IPSCORE", "B4", "B", "B — فناوری", "آیا فناوری نیازمند مهارت‌ها یا تجهیزات جدید است؟", 13),
        Def("IPSCORE", "B5", "B", "B — فناوری", "چه زمانی قبل از تجاری‌سازی کامل موردنیاز است؟", 14),
        Def("IPSCORE", "B6", "B", "B — فناوری", "آیا تولید محصولات کپی‌برداری‌شده آسان است؟", 15),
        Def("IPSCORE", "B7", "B", "B — فناوری", "آیا شناسایی محصولات ناقض پتنت آسان است؟", 16),
        Def("IPSCORE", "B8", "B", "B — فناوری", "آیا پیاده‌سازی فناوری به مجوزهای دیگران وابسته است؟", 17),
        Def("IPSCORE", "B9", "B", "B — فناوری", "آیا فناوری دارای ارزش بازاریابی (ارزش برای مشتری) است؟", 18),
        Def("IPSCORE", "C1", "C", "C — بازار", "گزینه‌ها و روش‌های بازاریابی چیست؟", 20),
        Def("IPSCORE", "C2", "C", "C — بازار", "نرخ رشد بازار در حوزه استفاده از فناوری چقدر است؟", 21),
        Def("IPSCORE", "C3", "C", "C — بازار", "طول عمر پیش‌بینی‌شده فناوری در بازار چقدر است؟", 22),
        Def("IPSCORE", "C4", "C", "C — بازار", "آیا محصولات رقابتی یا جایگزین در بازار فعال هستند؟", 23),
        Def("IPSCORE", "C5", "C", "C — بازار", "قیمت نهایی پرداختی مشتری در مقایسه با محصولات موجود؟", 24),
        Def("IPSCORE", "C6", "C", "C — بازار", "پتانسیل گردش مالی اضافی حاصل از به‌کارگیری فناوری؟", 25),
        Def("IPSCORE", "C7", "C", "C — بازار", "میزان دانش شرکت از پتانسیل کاربرد و فرصت‌های تجاری؟", 26),
        Def("IPSCORE", "C8", "C", "C — بازار", "آیا پتنت پتانسیل درآمدزایی از محل لایسنس دارد؟", 27),
        Def("IPSCORE", "C9", "C", "C — بازار", "آیا فعالیت‌های تجاری نیازمند مجوزهای خاص است؟", 28),
        Def("IPSCORE", "D1", "D", "D — مالی", "آیا حفظ خروجی فعلی بازار بدون فناوری پتنت‌شده امکان‌پذیر است؟", 30),
        Def("IPSCORE", "D2", "D", "D — مالی", "هزینه‌های توسعه آتی موردنیاز چقدر است؟", 31),
        Def("IPSCORE", "D3", "D", "D — مالی", "شاخص هزینه تولید هنگام اجرای فناوری پتنت‌شده؟", 32),
        Def("IPSCORE", "D4", "D", "D — مالی", "میزان سرمایه‌گذاری لازم برای تجهیزات تولید؟", 33),
        Def("IPSCORE", "D5", "D", "D — مالی", "آیا شرکت توان مالی پوشش هزینه‌های تمدید پتنت را دارد؟", 34),
        Def("IPSCORE", "D6", "D", "D — مالی", "سهم فناوری پتنت‌شده در سود شرکت چقدر است؟", 35),
        Def("IPSCORE", "E1", "E", "E — استراتژی", "آیا هدف پتنت تثبیت موقعیت در بازارهای موجود است؟", 40),
        Def("IPSCORE", "E2", "E", "E — استراتژی", "آیا هدف پتنت فتح بازارهای جدید است؟", 41),
        Def("IPSCORE", "E3", "E", "E — استراتژی", "آیا پتنت بخشی از فرآیند اعتبار و برندسازی است؟", 42),
        Def("IPSCORE", "E4", "E", "E — استراتژی", "آیا هدف پتنت تضمین آزادی عمل (Freedom to Operate) است؟", 43),
        Def("IPSCORE", "E5", "E", "E — استراتژی", "آیا هدف پتنت محدود کردن توسعه رقباست؟", 44),
        Def("IPSCORE", "E6", "E", "E — استراتژی", "آیا شرکت از پتنت برای قراردادهای لایسنس یا فروش استفاده می‌کند؟", 45),
        Def("IPSCORE", "E7", "E", "E — استراتژی", "آیا پتنت بخشی از فناوری‌های کلیدی و هسته‌ای شرکت است؟", 46),
        Def("IPSCORE", "E8", "E", "E — استراتژی", "آیا همراستایی کامل بین پتنت و استراتژی کسب‌وکار وجود دارد؟", 47),

        // ── JOB-EVAL: the two pick-list questions (options = the 16 values) ──
        Def("JOB-EVAL", "alwaysValues", "values", "۱۶ ارزش بنیادین", "ارزش‌های حیاتی (محرک زندگی) — حداکثر ۵ انتخاب", 1,
            ["۱ آزادی", "۲ امنیت", "۳ موفقیت", "۴ لذت", "۵ خلاقیت", "۶ همدلی", "۷ احترام", "۸ قدرت", "۹ تعلق", "۱۰ معنویت", "۱۱ شجاعت", "۱۲ دانش", "۱۳ زیبایی", "۱۴ عدالت", "۱۵ سلامتی", "۱۶ صداقت"]),
        Def("JOB-EVAL", "neverValues", "values", "۱۶ ارزش بنیادین", "ارزش‌های ناچیز (کم‌اهمیت) — حداکثر ۵ انتخاب", 2,
            ["۱ آزادی", "۲ امنیت", "۳ موفقیت", "۴ لذت", "۵ خلاقیت", "۶ همدلی", "۷ احترام", "۸ قدرت", "۹ تعلق", "۱۰ معنویت", "۱۱ شجاعت", "۱۲ دانش", "۱۳ زیبایی", "۱۴ عدالت", "۱۵ سلامتی", "۱۶ صداقت"]),
    ];
}
