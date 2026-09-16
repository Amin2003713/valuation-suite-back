using System.Text.Json;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Job Evaluation (ارزیابی ارزش‌های شغلی) — ported from
 * app/job-evaluation/data.ts. User picks values they ALWAYS want,
 * SOMETIMES want and NEVER want; backend tags → persona + job matching.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class JobEvaluationRunner : IToolRunner
{
    public string ToolCode => "JOB-EVAL";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<JobInput>(input);

        var alwaysTags = TagCounts(i.AlwaysValues ?? []);
        var neverTags = i.NeverValues ?? [];
        var neverCount = i.NeverValues?.Count ?? 0;

        var (persona, details) = AnalyzePersona(alwaysTags, neverCount);
        var jobs = RecommendJobs(alwaysTags, neverTags);

        var result = new JobResult(persona, details, jobs.good, jobs.bad, alwaysTags.Keys.OrderByDescending(k => alwaysTags[k]).ToList());
        return new ToolRunOutcome(result, null, i.Name);
    }

    private static Dictionary<string, int> TagCounts(List<string> values)
    {
        var counts = new Dictionary<string, int>();
        foreach (var tag in values)
        {
            if (string.IsNullOrWhiteSpace(tag)) continue;
            counts[tag] = counts.GetValueOrDefault(tag) + 1;
        }
        return counts;
    }

    private static (string Persona, string Details) AnalyzePersona(Dictionary<string, int> tagCounts, int neverCount)
    {
        string dominant = "general";
        foreach (var kv in tagCounts)
            if (tagCounts.GetValueOrDefault(dominant) < kv.Value) dominant = kv.Key;

        string persona, details;
        switch (dominant)
        {
            case "creative":
                persona = "شما یک روان‌شناس خلاق و مستقل هستید.";
                details = "روح شما تشنه‌ی نوآوری است. روتین‌های خشک و ساختارهای سنگین شما را خفه می‌کنند. شما در محیط‌هایی که به شما آزادی عمل می‌دهند تا ایده‌هایتان را به شکل خاص خودتان پیاده کنید، شکوفا می‌شوید. زیبایی‌شناسی برای شما یک تفریح نیست، بلکه یک ضرورت زندگی است.";
                break;
            case "social":
                persona = "شما یک مدیر روابط و یاریرسان هستید.";
                details = "انرژی شما از تعامل با انسان‌های دیگر می‌آید. موفقیت برای شما بدون داشتن تاثیر مثبت بر زندگی دیگران معنایی ندارد. شما شنونده‌ی خوبی هستید و درک عمیقی از احساسات دارید. محیط‌های خشک و فنی بدون ارتباط انسانی، شما را فرسوده می‌کنند.";
                break;
            case "leadership":
                persona = "شما یک رهبر طبیعی و جاه‌طلب هستید.";
                details = "تمایل دارید سکان دار باشید و تغییرات را رقم بزنید. رقابت شما را زنده نگه می‌دارد و به دنبال دستاوردهای بزرگ هستید که دیده شوند. شما از ساختارهای شلوغ و بوروکراتیک که مانع تصمیم‌گیری سریع می‌شوند بیزارید.";
                break;
            case "stability":
                persona = "شما یک سازنده‌ی قابل اعتماد و منظم هستید.";
                details = "پایداری و پیش‌بینی‌پذیری برای شما آرامش می‌آورد. شما پایه‌های محکم می‌سازید و به جزئیات اهمیت زیادی می‌دهید. ریسک‌های ناگهانی و بی‌نظمی‌های کاری باعث اضطراب شما می‌شود.";
                break;
            case "intellectual":
                persona = "شما یک مفکر و تحلیلگر عمیق هستید.";
                details = "حقیقت و دانش برای شما از هر چیزی بالاتر است. شما به دنبال چرایی‌ها هستید، نه صرفاً چگونگی‌ها. کارهای تکراری بدون چالش ذهنی شما را خسته می‌کنند. شما نیاز به محیطی دارید که در آن بتوانید یاد بگیرید و کشف کنید.";
                break;
            default:
                persona = "شخصیتی متعادل و چندوجهی دارید.";
                details = "شما انعطاف‌پذیر هستید و می‌توانید خود را با شرایط مختلف وفق دهید. با این حال، ممکن است گاهی در اولویت‌بندی دچار سردرگمی شوید چون برای شما جنبه‌های مختلفی از زندگی اهمیت دارد.";
                break;
        }

        if (neverCount > 5)
            details += " نکته مهم: شما در تعیین چیزهایی که «زیاد مهم نیستند» برای خود بسیار قاطع هستید. این یک ویژگی مثبت است، چون مسیرهای اشتباه را سریع حذف می‌کنید، اما مراقب باشید که درهای فرصت را با سخت‌گیری زیاد روی خود نبندید.";

        return (persona, details);
    }

    private static (List<JobMatch> good, List<JobMatch> bad) RecommendJobs(Dictionary<string, int> alwaysTags, List<string> neverTags)
    {
        var jobs = new (string Title, string[] Tags)[]
        {
            ("طراح گرافیک / هنرمند", ["creative", "hedonism", "autonomy"]),
            ("کارآفرین / استارتاپ", ["autonomy", "achievement", "leadership", "courage"]),
            ("روانشناس / مشاور", ["social", "people", "empathy"]),
            ("مدیر ارشد (CEO)", ["leadership", "power", "achievement"]),
            ("معلم / استاد دانشگاه", ["intellectual", "social", "structure"]),
            ("پزشک", ["social", "intellectual", "health", "achievement"]),
            ("پرستار", ["social", "empathy", "health", "structure"]),
            ("وکیل / قاضی", ["ethics", "justice", "power", "achievement"]),
            ("برنامه‌نویس / مهندس نرم‌افزار", ["intellectual", "autonomy", "creative"]),
            ("مهندس (عمران/مکانیک/الکتریک)", ["structure", "intellectual", "stability"]),
            ("حسابدار / مدیر مالی", ["structure", "stability", "ethics"]),
            ("نویسنده / روزنامه‌نگار", ["creative", "autonomy", "intellectual"]),
            ("سیاستمدار", ["power", "social", "justice", "leadership"]),
            ("مربی ورزشی", ["health", "achievement", "social"]),
            ("نیروی نظامی / پلیس", ["structure", "courage", "security", "power"]),
            ("کشاورز / باغبان", ["nature", "stability", "health"]),
            ("محقق / دانشمند", ["intellectual", "truth", "autonomy"]),
            ("فریلنسر (آزادکار)", ["autonomy", "freedom", "flexibility"]),
            ("فروشنده / بازاریاب", ["social", "achievement", "autonomy", "power"]),
            ("تعمیرکار (تاسیسات/الکترونیک)", ["structure", "stability", "intellectual"]),
            ("مترجم", ["intellectual", "autonomy", "social"]),
        };

        var scored = jobs.Select(job =>
        {
            var score = job.Tags.Sum(tag => alwaysTags.GetValueOrDefault(tag) > 0 ? 2 : 0)
                      + (alwaysTags.ContainsKey("creative") && job.Tags.Contains("creative") ? 1 : 0)
                      - job.Tags.Count(neverTags.Contains) * 3;
            return new JobMatch(job.Title, score);
        })
        .OrderByDescending(j => j.Score)
        .ToList();

        return (scored.Where(j => j.Score > 0).Take(6).ToList(), scored.Where(j => j.Score < 0).Take(4).ToList());
    }

    public sealed class JobInput
    {
        public string? Name { get; set; }
        public List<string>? AlwaysValues { get; set; }
        public List<string>? NeverValues { get; set; }
    }

    public sealed record JobMatch(string Title, int Score);
    public sealed record JobResult(string Persona, string Details, List<JobMatch> GoodJobs, List<JobMatch> AvoidJobs, List<string> DominantTags);
}
