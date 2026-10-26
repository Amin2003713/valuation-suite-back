namespace Application.Adviser;

/// <summary>Context handed to the answer generator.</summary>
public sealed record AdviserContext(
    string ToolCode,
    string ToolTitle,
    double? OverallScore,
    IReadOnlyList<(string Key, double Score)> Dimensions,
    string? CompanyName,
    string? Industry,
    Dictionary<string, (double Min, double Max, double Avg, int Count)> Peers);

/// <summary>
///     Rule-based, context-aware answer generation for the "Talk to the
///     Adviser" chat. It is deliberately deterministic (no external LLM): the
///     answers are built from the caller's latest stored result, their company
///     profile and anonymous peer aggregates. Every answer is honest about
///     being an automated summary and points at the paid expert review for a
///     human deep-dive.
/// </summary>
public static class AdviserAnswerBot
{
    public static string Answer(AdviserContext ctx, string question)
    {
        var fa = ContainsPersian(question);
        var q = question.Trim();

        // Language of the reply: match the question's script; default fa.
        return fa ? AnswerFa(ctx, q) : AnswerEn(ctx, q);
    }

    // ── Persian ─────────────────────────────────────────────────────────
    private static string AnswerFa(AdviserContext ctx, string q)
    {
        var best = ctx.Dimensions.OrderBy(d => d.Score).ToList();
        var lowest = best.Take(3).Where(d => d.Score < 60).ToList();
        var strongest = best.Count > 0 ? best.MaxBy(d => d.Score) : default;

        var head = ctx.OverallScore is { } s
            ? $"در جدیدترین اجرای «{ctx.ToolTitle}» نمره کلی شما {Round(s)} از ۱۰۰ است."
            : $"در جدیدترین اجرای «{ctx.ToolTitle}» هنوز نمره کلی محاسبه نشده است.";

        if (strongest.Key is not null)
            head += $" قوی‌ترین بعد شما «{strongest.Key}» با {Round(strongest.Score)} است.";

        if (IsImprove(q))
        {
            if (lowest.Count > 0)
            {
                var list = string.Join("، ", lowest.Select(d => $"«{d.Key}» ({Round(d.Score)})"));
                return head
                    + "\n\nبرای بهبود نمره، از بخش‌های ضعیف شروع کنید:\n"
                    + list
                    + "\n\n«ارزیابی کارشناسی» روی همین نتیجه را می‌توانید با دکمهٔ خرید بررسی کارشناسی سفارش دهید تا راهکار دقیق بر اساس داده‌هایتان ارائه شود.";
            }
            return head + "\n\nهمهٔ ابعاد تان در وضعیت قابل قبول (بالای ۶۰) است؛ حفظ روند و تکرار دورهٔ ارزیابی توصیه می‌شود.";
        }

        if (IsCompare(q) && ctx.Peers.Count > 0)
        {
            var lines = new List<string>();
            foreach (var d in ctx.Dimensions.OrderByDescending(d => d.Score).Take(5))
            {
                if (ctx.Peers.TryGetValue(d.Key, out var p) && p.Count >= 3)
                {
                    var diff = d.Score - p.Avg;
                    var dir = diff >= 0 ? "بالاتر از" : "پایین‌تر از";
                    lines.Add($"• «{d.Key}»: {Round(d.Score)} — {dir} میانگین سایر شرکت‌ها ({Round(p.Avg)})");
                }
            }
            var baseNote = ctx.CompanyName is { } c
                ? $"\n\nمقایسه بر اساس حوزهٔ فعالیت شما ({ctx.Industry ?? "—"}) و {ctx.Peers.Count} شاخص است و فقط دادهٔ تجمیعی و ناشناس شرکت‌های دیگر استفاده می‌شود."
                : "";
            return head + "\n\n" + string.Join("\n", lines) + baseNote
                   + "\n\nنمودار مقایسه‌ای همین نتیجه، رادار «مقایسه با سایرین» است.";
        }

        if (IsWhatResult(q))
        {
            var dims = string.Join("، ", ctx.Dimensions.OrderByDescending(d => d.Score).Select(d => $"«{d.Key}» {Round(d.Score)}"));
            return head + (dims.Length > 0 ? "\n\nامتیاز بخش‌ها: " + dims : "")
                + "\n\nمی‌توانید نتیجه را ذخیره یا دریافت PDF کنید یا سؤال دقیق‌تری دربارهٔ هر بخش بپرسید (مثلاً «بعد X را چطور بهبود بدهم؟»).";
        }

        // Generic fallback: recap the context and invite a specific question.
        var tail = FaExtras(ctx);
        return head + tail
            + "\n\nبرای پاسخ دقیق‌تر سؤال خود را مشخص‌تر بپرسید؛ مثلاً «قوی‌ترین و ضعیف‌ترین بخش من کدام است؟» یا «چطور نمرهٔ خود را بهبود دهم؟».";
    }

    // ── English ─────────────────────────────────────────────────────────
    private static string AnswerEn(AdviserContext ctx, string q)
    {
        var best = ctx.Dimensions.OrderBy(d => d.Score).ToList();
        var lowest = best.Take(3).Where(d => d.Score < 60).ToList();
        var strongest = best.Count > 0 ? best.MaxBy(d => d.Score) : default;

        var head = ctx.OverallScore is { } s
            ? $"Your latest \"{ctx.ToolTitle}\" run scored {Round(s)} out of 100."
            : $"Your latest \"{ctx.ToolTitle}\" run has no overall score yet.";

        if (strongest.Key is not null)
            head += $" Your strongest area is \"{strongest.Key}\" at {Round(strongest.Score)}.";

        if (IsImprove(q))
        {
            if (lowest.Count > 0)
            {
                var list = string.Join(", ", lowest.Select(d => $"\"{d.Key}\" ({Round(d.Score)})"));
                return head
                    + "\n\nTo improve your score, start with the weakest areas:\n"
                    + list
                    + "\n\nYou can also order the paid expert review of this result for a detailed, human assessment.";
            }
            return head + "\n\nAll your areas are above 60 — keep the momentum and re-run the assessment periodically.";
        }

        if (IsCompare(q) && ctx.Peers.Count > 0)
        {
            var lines = new List<string>();
            foreach (var d in ctx.Dimensions.OrderByDescending(d => d.Score).Take(5))
            {
                if (ctx.Peers.TryGetValue(d.Key, out var p) && p.Count >= 3)
                {
                    var diff = d.Score - p.Avg;
                    lines.Add($"• \"{d.Key}\": {Round(d.Score)} — {(diff >= 0 ? "above" : "below")} the peer average ({Round(p.Avg)})");
                }
            }
            return head + "\n\n" + string.Join("\n", lines)
                + "\n\nComparisons use aggregated, anonymous data from other submissions in your industry only."
                + "\n\nThe radar chart shows the same comparison visually.";
        }

        if (IsWhatResult(q))
        {
            var dims = string.Join(", ", ctx.Dimensions.OrderByDescending(d => d.Score).Select(d => $"\"{d.Key}\" {Round(d.Score)}"));
            return head + (dims.Length > 0 ? "\n\nArea scores: " + dims : "");
        }

        return head + EnExtras(ctx)
            + "\n\nAsk something more specific, e.g. \"Which of my areas is weakest?\" or \"How do I improve my score?\"";
    }

    private static string FaExtras(AdviserContext ctx)
    {
        var parts = new List<string>();
        if (ctx.CompanyName is { } c) parts.Add($"شرکت شما: {c}");
        if (ctx.Industry is { } i) parts.Add($"حوزهٔ فعالیت: {i}");
        if (ctx.Dimensions.Count > 0)
            parts.Add($"ضعیف‌ترین حوزه‌ها: " + string.Join("، ", ctx.Dimensions.OrderBy(d => d.Score).Take(2).Select(d => $"«{d.Key}» ({Round(d.Score)})")));
        if (ctx.Peers.Count > 0) parts.Add($"میانگین سایر شرکت‌ها روی {ctx.Peers.Count} شاخص در دسترس است.");
        return parts.Count > 0 ? "\n\n" + string.Join(" · ", parts) : "";
    }

    private static string EnExtras(AdviserContext ctx)
    {
        var parts = new List<string>();
        if (ctx.CompanyName is { } c) parts.Add($"Company: {c}");
        if (ctx.Industry is { } i) parts.Add($"Area of work: {i}");
        if (ctx.Dimensions.Count > 0)
            parts.Add("Weakest areas: " + string.Join(", ", ctx.Dimensions.OrderBy(d => d.Score).Take(2).Select(d => $"\"{d.Key}\" ({Round(d.Score)})")));
        if (ctx.Peers.Count > 0) parts.Add($"Peer averages are available across {ctx.Peers.Count} metrics.");
        return parts.Count > 0 ? "\n\n" + string.Join(" · ", parts) : "";
    }

    // ── Intent detection (bilingual, forgiving) ─────────────────────────
    private static bool IsImprove(string q)
    {
        return Contains(q, "بهبود", "بهتر", "رشد", "ضعف", "خالص", "راهکار", "توصیه", "علت")
            || ContainsAny(q, "improve", "better", "weak", "advice", "recommend", "how can i", "why");
    }

    private static bool IsCompare(string q)
    {
        return Contains(q, "مقایسه", "میانگین", "سایر", "دیگران", "بازار", "رتبه", "کجا ایستاده")
            || ContainsAny(q, "compare", "average", "benchmark", "peer", "others", "rank", "percentile", "versus", "vs");
    }

    private static bool IsWhatResult(string q)
    {
        return Contains(q, "نتیجه", "امتیاز", "خروجی", "معنی", "یعنی چی", "حالم")
            || ContainsAny(q, "result", "score", "mean", "report", "summary", "what does");
    }

    private static bool ContainsAny(string text, params string[] needles)
        => needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static bool Contains(string text, params string[] needles)
        => ContainsAny(text, needles);

    private static bool ContainsPersian(string s)
        => s.Any(c => c >= '\u0600' && c <= '\u06FF');

    private static string Round(double v) => Math.Round(v, 1).ToString("0.#");
}
