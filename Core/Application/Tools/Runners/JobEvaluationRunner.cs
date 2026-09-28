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
        var loc = new ToolLocalization(i.Lang);

        var alwaysTags = TagCounts(i.AlwaysValues ?? []);
        var neverTags = i.NeverValues ?? [];
        var neverCount = i.NeverValues?.Count ?? 0;

        var (persona, details) = AnalyzePersona(alwaysTags, neverCount, loc);
        var jobs = RecommendJobs(alwaysTags, neverTags, loc);

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

    private static (string Persona, string Details) AnalyzePersona(Dictionary<string, int> tagCounts, int neverCount, ToolLocalization loc)
    {
        string dominant = "general";
        foreach (var kv in tagCounts)
            if (tagCounts.GetValueOrDefault(dominant) < kv.Value) dominant = kv.Key;

        string persona, details;
        switch (dominant)
        {
            case "creative":
                persona = loc.Pick("شما یک روان‌شناس خلاق و مستقل هستید.", "You are a creative and independent spirit.");
                details = loc.Pick("روح شما تشنه‌ی نوآوری است. روتین‌های خشک و ساختارهای سنگین شما را خفه می‌کنند. شما در محیط‌هایی که به شما آزادی عمل می‌دهند تا ایده‌هایتان را به شکل خاص خودتان پیاده کنید، شکوفا می‌شوید. زیبایی‌شناسی برای شما یک تفریح نیست، بلکه یک ضرورت زندگی است.", "Your soul thirsts for innovation. Rigid routines and heavy structures suffocate you. You thrive in environments that give you freedom to implement your ideas in your own unique way. Aesthetics are not a pastime for you — they are a necessity of life.");
                break;
            case "social":
                persona = loc.Pick("شما یک مدیر روابط و یاریرسان هستید.", "You are a relationship builder and helper.");
                details = loc.Pick("انرژی شما از تعامل با انسان‌های دیگر می‌آید. موفقیت برای شما بدون داشتن تاثیر مثبت بر زندگی دیگران معنایی ندارد. شما شنونده‌ی خوبی هستید و درک عمیقی از احساسات دارید. محیط‌های خشک و فنی بدون ارتباط انسانی، شما را فرسوده می‌کنند.", "Your energy comes from interacting with other people. Success means nothing to you without a positive impact on others' lives. You are a good listener with a deep understanding of emotions. Dry, technical environments without human connection drain you.");
                break;
            case "leadership":
                persona = loc.Pick("شما یک رهبر طبیعی و جاه‌طلب هستید.", "You are a natural, ambitious leader.");
                details = loc.Pick("تمایل دارید سکان دار باشید و تغییرات را رقم بزنید. رقابت شما را زنده نگه می‌دارد و به دنبال دستاوردهای بزرگ هستید که دیده شوند. شما از ساختارهای شلوغ و بوروکراتیک که مانع تصمیم‌گیری سریع می‌شوند بیزارید.", "You want to steer the ship and drive change. Competition keeps you alive and you chase big, visible achievements. You despise cluttered, bureaucratic structures that block fast decision-making.");
                break;
            case "stability":
                persona = loc.Pick("شما یک سازنده‌ی قابل اعتماد و منظم هستید.", "You are a dependable, organized builder.");
                details = loc.Pick("پایداری و پیش‌بینی‌پذیری برای شما آرامش می‌آورد. شما پایه‌های محکم می‌سازید و به جزئیات اهمیت زیادی می‌دهید. ریسک‌های ناگهانی و بی‌نظمی‌های کاری باعث اضطراب شما می‌شود.", "Stability and predictability bring you calm. You build solid foundations and care deeply about details. Sudden risks and workplace disorder cause you anxiety.");
                break;
            case "intellectual":
                persona = loc.Pick("شما یک مفکر و تحلیلگر عمیق هستید.", "You are a deep thinker and analyst.");
                details = loc.Pick("حقیقت و دانش برای شما از هر چیزی بالاتر است. شما به دنبال چرایی‌ها هستید، نه صرفاً چگونگی‌ها. کارهای تکراری بدون چالش ذهنی شما را خسته می‌کنند. شما نیاز به محیطی دارید که در آن بتوانید یاد بگیرید و کشف کنید.", "Truth and knowledge outrank everything else for you. You seek the why, not merely the how. Repetitive work without mental challenge tires you. You need an environment where you can learn and discover.");
                break;
            default:
                persona = loc.Pick("شخصیتی متعادل و چندوجهی دارید.", "You have a balanced, multi-faceted personality.");
                details = loc.Pick("شما انعطاف‌پذیر هستید و می‌توانید خود را با شرایط مختلف وفق دهید. با این حال، ممکن است گاهی در اولویت‌بندی دچار سردرگمی شوید چون برای شما جنبه‌های مختلفی از زندگی اهمیت دارد.", "You are adaptable and can adjust to different circumstances. That said, prioritizing can sometimes feel confusing because many aspects of life matter to you.");
                break;
        }

        if (neverCount > 5)
            details += " " + loc.Pick(
                "نکته مهم: شما در تعیین چیزهایی که «زیاد مهم نیستند» برای خود بسیار قاطع هستید. این یک ویژگی مثبت است، چون مسیرهای اشتباه را سریع حذف می‌کنید، اما مراقب باشید که درهای فرصت را با سخت‌گیری زیاد روی خود نبندید.",
                "Important note: you are decisive about what does not matter much to you. That is a strength — it eliminates wrong paths quickly — but be careful not to close doors of opportunity through excessive strictness.");

        return (persona, details);
    }

    private static (List<JobMatch> good, List<JobMatch> bad) RecommendJobs(Dictionary<string, int> alwaysTags, List<string> neverTags, ToolLocalization loc)
    {
        var jobs = new (string Fa, string En, string[] Tags)[]
        {
            ("طراح گرافیک / هنرمند", "Graphic designer / Artist", ["creative", "hedonism", "autonomy"]),
            ("کارآفرین / استارتاپ", "Entrepreneur / Startup founder", ["autonomy", "achievement", "leadership", "courage"]),
            ("روانشناس / مشاور", "Psychologist / Counselor", ["social", "people", "empathy"]),
            ("مدیر ارشد (CEO)", "Chief Executive (CEO)", ["leadership", "power", "achievement"]),
            ("معلم / استاد دانشگاه", "Teacher / Professor", ["intellectual", "social", "structure"]),
            ("پزشک", "Physician", ["social", "intellectual", "health", "achievement"]),
            ("پرستار", "Nurse", ["social", "empathy", "health", "structure"]),
            ("وکیل / قاضی", "Lawyer / Judge", ["ethics", "justice", "power", "achievement"]),
            ("برنامه‌نویس / مهندس نرم‌افزار", "Software developer / Engineer", ["intellectual", "autonomy", "creative"]),
            ("مهندس (عمران/مکانیک/الکتریک)", "Engineer (Civil/Mechanical/Electrical)", ["structure", "intellectual", "stability"]),
            ("حسابدار / مدیر مالی", "Accountant / Finance manager", ["structure", "stability", "ethics"]),
            ("نویسنده / روزنامه‌نگار", "Writer / Journalist", ["creative", "autonomy", "intellectual"]),
            ("سیاستمدار", "Politician", ["power", "social", "justice", "leadership"]),
            ("مربی ورزشی", "Sports coach", ["health", "achievement", "social"]),
            ("نیروی نظامی / پلیس", "Military / Police officer", ["structure", "courage", "security", "power"]),
            ("کشاورز / باغبان", "Farmer / Gardener", ["nature", "stability", "health"]),
            ("محقق / دانشمند", "Researcher / Scientist", ["intellectual", "truth", "autonomy"]),
            ("فریلنسر (آزادکار)", "Freelancer", ["autonomy", "freedom", "flexibility"]),
            ("فروشنده / بازاریاب", "Sales / Marketing", ["social", "achievement", "autonomy", "power"]),
            ("تعمیرکار (تاسیسات/الکترونیک)", "Technician (HVAC/Electronics)", ["structure", "stability", "intellectual"]),
            ("مترجم", "Translator", ["intellectual", "autonomy", "social"]),
        };

        var scored = jobs.Select(job =>
        {
            var score = job.Tags.Sum(tag => alwaysTags.GetValueOrDefault(tag) > 0 ? 2 : 0)
                      + (alwaysTags.ContainsKey("creative") && job.Tags.Contains("creative") ? 1 : 0)
                      - job.Tags.Count(neverTags.Contains) * 3;
            return new JobMatch(loc.Pick(job.Fa, job.En), score);
        })
        .OrderByDescending(j => j.Score)
        .ToList();

        return (scored.Where(j => j.Score > 0).Take(6).ToList(), scored.Where(j => j.Score < 0).Take(4).ToList());
    }

    public sealed class JobInput
    {
        public string? Name { get; set; }
        public string? Lang { get; set; }
        public List<string>? AlwaysValues { get; set; }
        public List<string>? NeverValues { get; set; }
    }

    public sealed record JobMatch(string Title, int Score);
    public sealed record JobResult(string Persona, string Details, List<JobMatch> GoodJobs, List<JobMatch> AvoidJobs, List<string> DominantTags);
}
