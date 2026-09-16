using Domain.Assessments;
using Domain.Common;
using Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Assessments.Seeding;

/// <summary>Write-side persistence contract used by seeders (implemented by Persistence).</summary>
public interface ISeedingDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Seeds the "IP Assessment" tool with the questions that used to be hard-coded in the
///     frontend (app/lib/ip-assessment/questions.ts). Scoring follows the original frontend
///     semantics: option index i scores (i + 1) * 20, so the LAST option is the "good" one.
///     No math engine is involved — these are pure choice questions.
/// </summary>
public static class IpAssessmentSeeder
{
    public const string AssessmentCode = "IP-ASSESS";

    // ── Section metadata (mirrors SECTION_LABELS in the frontend) ──
    private static readonly (string Key, string Title, string Desc, string Mark)[] Sections =
    [
        ("trademark", "علامت‌های تجاری", "برند، لوگو و هویت تجاری", "TM"),
        ("confidential", "اطلاعات محرمانه", "اسرار تجاری و دانش محرمانه", "TR"),
        ("designs", "طرح‌های صنعتی", "طرح‌های بصری و صنعتی", "DS"),
        ("inventive", "اختراعات و نوآوری", "پتنت و محصولات نوآور", "IN"),
        ("employment", "کارکنان", "حقوق کارکنان و قراردادها", "EM"),
        ("website", "وب‌سایت", "دامنه، محتوا و حقوق دیجیتال", "WB"),
    ];

    private static readonly (string Id, string Text, string[] Options)[] PreQuestions =
    [
        ("product", "آیا محصول، فرآیند، خدمت یا اصلاح فنی توسعه داده‌اید که آن را جدید، نوآور یا منحصر به‌فرد می‌دانید؟", ["بله", "خیر"]),
        ("materials", "آیا موادی مانند راهنما، بروشور، برچسب، ویدیو، نرم‌افزار یا خبرنامه تولید می‌کنید؟", ["بله", "خیر"]),
        ("designs", "آیا از ویژگی‌هایی مانند الگوها، خطوط، رنگ‌ها یا اشکال برای جذاب‌تر کردن ظاهر یا بسته‌بندی محصول خود استفاده می‌کنید؟", ["بله", "خیر"]),
        ("confidential", "آیا کسب‌وکار شما به اطلاعاتی وابسته است که آن را از نظر تجاری ارزشمند می‌دانید و نمی‌خواهید رقبا به آن دسترسی داشته باشند؟", ["بله", "خیر"]),
        ("trademark", "آیا از لوگو یا علائم دیگری برای متمایز کردن محصولات یا خدمات خود از دیگران استفاده می‌کنید؟", ["بله", "خیر"]),
        ("suppliers", "آیا برای تضمین موجودی، دریافت قطعات یا توسعه مواد به تأمین‌کنندگان خارجی وابسته هستید؟", ["بله", "خیر", "نمی‌دانم"]),
        ("website", "آیا وب‌سایت دارید یا قصد ایجاد آن را دارید؟", ["بله", "خیر", "نمی‌دانم"]),
        ("international", "آیا محصولات را خارج از کشوری که کسب‌وکار شما در آن متمرکز است تولید یا می‌فروشید؟", ["بله", "خیر"]),
        ("employees", "آیا کارمند دارید؟", ["بله", "خیر"]),
        ("ipServices", "آیا به خدمات یا تخصص مالکیت فکری دسترسی دارید؟", ["بله", "خیر"]),
    ];

    private static readonly (string Key, (string Id, string Text, string[] Options)[] Questions)[] Detailed =
    [
        ("trademark",
        [
            ("tmRegistered", "آیا علامت تجاری در دفتر مالکیت فکری ملی یا منطقه‌ای ثبت کرده‌اید یا برای آن درخواست داده‌اید؟", ["بله", "خیر"]),
            ("tmUsage", "آیا از علامت تجاری ثبت‌شده خود برای بازاریابی کالاها و خدمات خود استفاده می‌کنید؟", ["بله", "خیر", "مطمئن نیستم"]),
            ("tmInvestors", "آیا به جذب سرمایه‌گذاران برای شرکت خود علاقه دارید؟", ["بله", "خیر", "مطمئن نیستم"]),
            ("tmLicense", "آیا به واگذاری حق استفاده از علامت تجاری خود به دیگری فکر کرده‌اید؟", ["بله", "خیر"]),
            ("tmLicenseObtain", "آیا به دریافت حق استفاده از علامت تجاری دیگران در کسب‌وکار خود فکر کرده‌اید؟", ["بله", "خیر"]),
            ("tmInfringement", "آیا فکر می‌کنید دیگران از علامت‌های مشابه بدون مجوز شما استفاده می‌کنند؟", ["بله", "خیر"]),
            ("tmExternal", "آیا علامت تجاری شما توسط شخص خارجی برای شما طراحی شده است؟", ["بله", "خیر"]),
        ]),
        ("confidential",
        [
            ("confidentialProtection", "آیا اقدامات معقولی برای محافظت از اطلاعات کسب‌وکار خود که ارزشمند می‌دانید انجام داده‌اید؟", ["بله", "خیر", "مطمئن نیستم"]),
            ("confidentialAccess", "آیا دسترسی به این اطلاعات با مکانیزم‌هایی مانند رمز عبور، علامت‌گذاری اسناد محرمانه و سیاست‌های نیاز به دانستن کنترل می‌شود؟", ["بله", "خیر"]),
            ("confidentialNDA", "آیا از اشخاص خارجی (شرکا، همکاران، تأمین‌کنندگان) خواسته می‌شود قراردادهای محرمانگی امضا کنند؟", ["بله", "خیر"]),
            ("confidentialSystems", "آیا سیستم‌هایی برای تعیین اینکه چه کسانی باید به اطلاعات محرمانه دسترسی داشته باشند وجود دارد؟", ["بله", "خیر", "مطمئن نیستم"]),
            ("confidentialProduct", "آیا اطلاعات محرمانه در محصولی که تولید می‌کنید متجسم شده است؟", ["بله", "خیر", "مطمئن نیستم"]),
        ]),
        ("designs",
        [
            ("designRegistered", "آیا حق طرح صنعتی در دفتر مالکیت فکری ملی یا منطقه‌ای ثبت کرده‌اید یا برای آن درخواست داده‌اید؟", ["بله", "خیر"]),
            ("designInvestors", "آیا به جذب سرمایه‌گذاران برای شرکت خود علاقه دارید؟", ["بله", "خیر", "مطمئن نیستم"]),
            ("designLicense", "آیا به واگذاری حق استفاده از طرح خود به شرکتی فکر کرده‌اید؟", ["بله", "خیر"]),
            ("designLicenseObtain", "آیا به دریافت حق استفاده از طرح دیگران در کسب‌وکار خود فکر کرده‌اید؟", ["بله", "خیر"]),
            ("designInfringement", "آیا فکر می‌کنید دیگران از طرح‌های شما روی محصولات خود بدون مجوز شما استفاده می‌کنند؟", ["بله", "خیر"]),
            ("designExternal", "آیا طرحی که استفاده می‌کنید توسط شخص خارجی برای شما توسعه داده شده است؟", ["بله", "خیر"]),
        ]),
        ("inventive",
        [
            ("patentGranted", "آیا برای اختراع خود پتنت دریافت کرده‌اید یا در دفتر پتنت برای آن درخواست داده‌اید؟", ["بله", "خیر"]),
            ("patentDatabases", "آیا از وجود پایگاه‌های داده پتنت که می‌توان به طور رایگان به آنها مراجعه کرد آگاه هستید؟", ["بله", "خیر"]),
            ("inventiveInvestors", "آیا به جذب سرمایه‌گذاران برای شرکت خود علاقه دارید؟", ["بله", "خیر", "مطمئن نیستم"]),
            ("inventiveLicense", "آیا به واگذاری حق استفاده از فناوری مالکیت خود به دیگری فکر کرده‌اید؟", ["بله", "خیر"]),
            ("inventiveLicenseObtain", "آیا به دریافت حق استفاده از محصول نوآورانه یا فرآیند دیگران فکر کرده‌اید؟", ["بله", "خیر"]),
            ("inventiveInfringement", "آیا فکر می‌کنید دیگران از محصول نوآورانه یا فرآیند شما بدون رضایت شما استفاده می‌کنند؟", ["بله", "خیر"]),
            ("inventiveExternal", "آیا محصول جدید یا اصلاح فنی جدید شما توسط شخص خارجی توسعه داده شده است؟", ["بله", "خیر"]),
        ]),
        ("employment",
        [
            ("employeeInventions", "آیا کارکنان شما در اختراع محصولات جدید یا فرآیندهای شما نقش دارند؟", ["بله", "خیر"]),
            ("employeeDesigns", "آیا کارکنان شما در توسعه مواد خلاقانه، طرح‌ها یا علائم تجاری نقش دارند؟", ["بله", "خیر"]),
            ("employeeConfidential", "آیا در توافق‌نامه‌های خود با کارکنان، مواردی گنجانده‌اید که از اطلاعات محرمانه شما محافظت می‌کند؟", ["بله", "خیر"]),
            ("employeeTraining", "آیا کارکنان خود را در مورد نحوه محافظت از اطلاعات محرمانه آموزش می‌دهید؟", ["بله", "خیر"]),
            ("employeeHiring", "آیا هنگام استخدام اطمینان حاصل می‌کنید که کارکنان اطلاعات تجاری از کارفرمایان قبلی با خود نمی‌آورند؟", ["بله", "خیر"]),
        ]),
        ("website",
        [
            ("domainRegistered", "آیا نام دامنه برای وب‌سایت خود ثبت کرده‌اید؟", ["بله", "خیر"]),
            ("domainTrademark", "آیا نام دامنه شما به طور کامل یا بخشی علامت تجاری شماست؟", ["بله", "خیر"]),
            ("domainTrademarkOther", "آیا نام دامنه شما شامل علامت تجاری ثبت‌شده توسط شخص دیگری است؟", ["بله", "خیر"]),
            ("websiteDevelopment", "آیا وب‌سایت خود را به طور مستقل توسعه داده‌اید؟", ["بله", "خیر"]),
            ("websiteMaterials", "آیا مواد آپلود‌شده در وب‌سایت (محتوا، عکس، ویدیو) در شرکت توسعه یافته است؟", ["بله", "خیر"]),
            ("websiteLinks", "آیا وب‌سایت شما لینک‌هایی به مواد ذخیره‌شده در وب‌سایت‌های دیگر ارائه می‌دهد؟", ["بله", "خیر"]),
            ("websiteUsers", "آیا کاربران مواد را روی وب‌سایت شما آپلود می‌کنند؟", ["بله", "خیر"]),
        ]),
    ];

    /// <summary>Pre-questions that gate a detailed section: section key → gating pre-question id.</summary>
    private static readonly Dictionary<string, string> SectionGates = new()
    {
        ["trademark"] = "trademark",
        ["confidential"] = "confidential",
        ["designs"] = "designs",
        ["inventive"] = "product",
        ["employment"] = "employees",
        ["website"] = "website",
    };

    public static async Task SeedAsync(ISeedingDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var exists = await db.Set<Assessment>()
            .AnyAsync(a => a.Code == AssessmentCode, ct);
        if (exists)
        {
            logger.LogInformation("IP Assessment seed already present, skipping");
            return;
        }

        // A system company owns the public assessments.
        var company = await db.Set<Company>().FirstOrDefaultAsync(c => c.Slug == "system", ct);
        if (company is null)
        {
            company = Company.Create("Valuation Suite", "system");
            db.Set<Company>().Add(company);
            await db.SaveChangesAsync(ct);
        }

        var assessment = Assessment.Create("ارزیابی مالکیت فکری", AssessmentCode,
            "ارزیابی جامع وضعیت مالکیت فکری کسب‌وکار، شناسایی دارایی‌های فکری، ریسک‌ها و فرصت‌های ثبت و حفاظت",
            company.Id);
        assessment.CreateDraftVersion();
        var version = assessment.Versions.Single();
        version.Title = "نسخه ۱";

        int order = 1;

        // Step 0 — pre-questions
        var preStep = new Step
        {
            VersionId = version.Id,
            Title = "پیش‌سوالات",
            Description = "سوالات اولیه برای شناخت کسب‌وکار شما",
            Order = order++,
        };
        foreach (var (id, text, options) in PreQuestions)
        {
            var q = NewChoiceQuestion(id, text, options, order++);
            preStep.AddQuestion(q);
        }
        version.AddStep(preStep);

        // Steps 1..6 — detailed sections
        foreach (var (key, title, desc, _) in Sections)
        {
            var step = new Step
            {
                VersionId = version.Id,
                Title = title,
                Description = desc,
                Order = order++,
            };

            var gateId = SectionGates[key];
            int qOrder = 1;
            foreach (var (id, text, options) in Detailed.First(d => d.Key == key).Questions)
            {
                var q = NewChoiceQuestion(id, text, options, qOrder++);
                // Question is visible only when its gating pre-question was answered "بله" (option index 0).
                q.SetVisibilityConditions(
                [
                    new VisibilityCondition
                    {
                        TargetQuestionId = gateId,
                        Type = VisibilityConditionType.Equals,
                        Value = "0",
                    },
                ]);
                step.AddQuestion(q);
            }
            version.AddStep(step);
        }

        db.Set<Assessment>().Add(assessment);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded IP Assessment with {Steps} steps / {Questions} questions",
            version.Steps.Count, version.Steps.Sum(s => s.Questions.Count));
    }

    private static Question NewChoiceQuestion(string key, string text, string[] options, int order)
    {
        var q = new Question
        {
            StepId = Guid.Empty, // fixed up by EF when added through the step
            Text = text,
            Key = key,
            Type = QuestionType.SingleChoice,
            Order = order,
            IsRequired = true,
            HelpText = null,
        };

        for (int i = 0; i < options.Length; i++)
        {
            q.AddOption(new Option
            {
                Label = options[i],
                Value = i.ToString(), // stable machine value = original index
                Order = i,
                IsCorrect = false,
                // Preserve the original frontend scoring: index 0 → 20, last → options.Length * 20.
                Score = (i + 1) * 20,
            });
        }

        return q;
    }
}
