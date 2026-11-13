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
    /// <summary>
    ///     Question ids a tool no longer asks (explicit list). Removed on startup so a
    ///     stale row — especially a required one — can never reject a run of a reworked
    ///     tool. Tools with seeded definitions additionally get the generic sweep below,
    ///     which retires every row whose id is not (or is no longer) in the definitions.
    /// </summary>
    private static readonly Dictionary<string, string[]> RetiredQuestionIds = new()
    {
        // INNOV-READ moved from nine 0–10 sliders to the 27-question KTH
        // questionnaire (per-level question ids replace the indicator ids).
        ["INNOV-READ"] = ["crl", "trl", "brl", "iprl", "tmrl", "frl", "lrl", "scrl", "srl"],
    };

    public static async Task SeedAsync(IToolFormsDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var defs = Definitions();

        var retired = 0;

        /* Existing per-tool question rows + localization rows (only the tools that
         * actually have question content are inspected — keeps startup cheap). */
        var toolsInDefs = defs.Select(d => d.ToolCode).Distinct().ToList();
        var existingQuestion = new HashSet<string>();
        var existingLoc = new HashSet<string>();
        var existingRowsByTool = new Dictionary<string, List<ToolQuestionBase>>();
        foreach (var code in toolsInDefs)
        {
            if (!ToolRegistry.TryGetQuestion(code, out var qType)) continue;
            var rows = SetOf(db, qType).Cast<ToolQuestionBase>().ToList();
            existingRowsByTool[code] = rows;
            foreach (var q in rows) existingQuestion.Add($"{code}|{q.QuestionId}");

            if (!ToolRegistry.TryGetLocalization(code, out var lType)) continue;
            foreach (var l in SetOf(db, lType).Cast<ToolLocalizationBase>().ToList())
                existingLoc.Add($"{code}|{l.Language}|{l.QuestionId}");
        }

        /* Generic self-heal: for every tool whose questions are seeded from this file,
         * drop question/localization rows whose id is not part of the current
         * definitions (or is in RetiredQuestionIds). Renaming or removing a question
         * in the seed therefore can never leave behind a required row that rejects
         * every run — the exact failure the explicit retire list above was patched for.
         * Admin content edits only retitle existing rows, so no admin data is lost. */
        var definedIds = defs
            .GroupBy(d => d.ToolCode)
            .ToDictionary(g => g.Key, g => g.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        foreach (var (code, ids) in definedIds)
        {
            var retiredForTool = RetiredQuestionIds.TryGetValue(code, out var explicitIds)
                ? explicitIds
                : [];

            if (existingRowsByTool.TryGetValue(code, out var rows))
            {
                var stale = rows.Where(r =>
                        !ids.Contains(r.QuestionId) ||
                        retiredForTool.Contains(r.QuestionId, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                foreach (var row in stale)
                {
                    db.RemoveEntity(row);
                    rows.Remove(row);
                    existingQuestion.Remove($"{code}|{row.QuestionId}");
                    retired++;
                }
            }

            if (!ToolRegistry.TryGetLocalization(code, out var locType)) continue;
            /* Materialize before filtering: StringComparer-based Contains is not
             * translatable to SQL (EF would throw and abort the whole seed). */
            var staleLoc = SetOf(db, locType).Cast<ToolLocalizationBase>().ToList()
                .Where(l => l.QuestionId != ToolLocalizationBase.ToolLevelQuestionId &&
                            (!ids.Contains(l.QuestionId) ||
                             retiredForTool.Contains(l.QuestionId, StringComparer.OrdinalIgnoreCase)))
                .ToList();
            foreach (var row in staleLoc)
            {
                db.RemoveEntity(row);
                existingLoc.Remove($"{code}|{row.Language}|{row.QuestionId}");
            }
        }

        var missing = defs.Where(d => !existingQuestion.Contains($"{d.ToolCode}|{d.Id}")).ToList();

        foreach (var d in missing)
        {
            var meta = Meta.GetValueOrDefault(d.ToolCode);
            var row = ToolRegistry.CreateQuestion(d.ToolCode);
            row.QuestionId = d.Id;
            row.SectionKey = d.SectionKey;
            row.SectionTitle = d.SectionTitle;
            row.Text = d.Text;
            row.OptionsJson = d.OptionsJson;
            row.SortOrder = d.Sort;
            row.AnswerType = meta.AnswerType;
            row.IsRequired = meta.Required;
            row.ScaleMin = meta.Min;
            row.ScaleMax = meta.Max;
            db.AddEntity(row);
            existingQuestion.Add($"{d.ToolCode}|{d.Id}");
        }

        /* Validation metadata for rows copied from the legacy shared table (they arrive
         * with the empty default AnswerType). Applied only while the row is untouched —
         * never overwrites an admin or seed edit. */
        var filledMeta = 0;
        foreach (var rows in existingRowsByTool.Values)
        foreach (var row in rows)
        {
            var meta = Meta.GetValueOrDefault(row.ToolCode);
            if (meta.AnswerType != "text" && string.IsNullOrEmpty(row.AnswerType))
            {
                row.AnswerType = meta.AnswerType;
                row.IsRequired = meta.Required;
                row.ScaleMin = meta.Min;
                row.ScaleMax = meta.Max;
                filledMeta++;
            }
        }

        /* English localization rows for every question the corpus knows and that has
         * no en row yet — covers both freshly seeded and legacy rows. */
        var filledTranslations = 0;
        foreach (var d in defs)
        {
            if (!English.TryGetValue((d.ToolCode, d.Id), out var en) || en.Text is null) continue;
            if (existingLoc.Contains($"{d.ToolCode}|en|{d.Id}")) continue;

            var row = ToolRegistry.CreateLocalization(d.ToolCode, "en", d.Id);
            row.Text = en.Text;
            row.SectionTitle = en.SectionTitle;
            if (en.Options is { Count: > 0 })
                row.OptionsJson = System.Text.Json.JsonSerializer.Serialize(
                    en.Options.Select(kv => new { value = kv.Key, label = kv.Value }).ToArray());
            db.AddEntity(row);
            existingLoc.Add($"{d.ToolCode}|en|{d.Id}");
            filledTranslations++;
        }

        if (missing.Count == 0 && filledTranslations == 0 && filledMeta == 0 && retired == 0)
        {
            logger.LogInformation("Tool questions seed already present, skipping");
            return;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Seeded {Count} tool question rows; filled {Translations} en localization rows, {Meta} validation-metadata rows and retired {Retired} stale rows",
            missing.Count, filledTranslations, filledMeta, retired);
    }

    /// <summary>Runtime-typed DbSet for a per-tool entity (seeders).</summary>
    private static IQueryable SetOf(IToolFormsDbContext db, Type type) =>
        (IQueryable)typeof(IToolFormsDbContext)
            .GetMethod(nameof(IToolFormsDbContext.Set))!
            .MakeGenericMethod(type)
            .Invoke(db, null)!;

    /// <summary>
    ///     Validation metadata per tool (answer type, required, scale bounds) — drives
    ///     the server-side answer validator. "taglist" (JOB-EVAL) is intentionally
    ///     unvalidated: the client posts tag strings, not the seeded option values.
    /// </summary>
    private static readonly Dictionary<string, (string AnswerType, bool Required, int? Min, int? Max)> Meta = new()
    {
        ["IDEA-ASSESS"] = ("scale", true, 1, 5),     // client blocks incomplete runs
        ["INNOV-READ"] = ("scale", true, 1, 4),      // KTH: every question is answered (1..n options)
        ["IPSCORE"] = ("scale", false, 1, 5),        // client allows partial questionnaires
        ["WIPO-DIAG"] = ("choice", false, null, null), // yes/no/not-sure; partial runs allowed
        ["JOB-EVAL"] = ("taglist", false, null, null), // arrays of tags, not option values
    };

    // ─── English (non-default language) seed corpus ───────────────────

    private static readonly Dictionary<string, string> YesNoNotSure = new() { ["0"] = "Yes", ["1"] = "No", ["2"] = "Not sure" };
    private static readonly Dictionary<string, string> YesNo = new() { ["0"] = "Yes", ["1"] = "No" };

    private static readonly Dictionary<string, string> JobValueLabels = new()
    {
        ["0"] = "1 Freedom", ["1"] = "2 Security", ["2"] = "3 Achievement", ["3"] = "4 Pleasure",
        ["4"] = "5 Creativity", ["5"] = "6 Empathy", ["6"] = "7 Respect", ["7"] = "8 Power",
        ["8"] = "9 Belonging", ["9"] = "10 Spirituality", ["10"] = "11 Courage", ["11"] = "12 Knowledge",
        ["12"] = "13 Beauty", ["13"] = "14 Justice", ["14"] = "15 Health", ["15"] = "16 Honesty",
    };

    /// <summary>English question texts / section titles / option labels per (tool, question id).</summary>
    private static readonly Dictionary<(string Tool, string Id), (string Text, string? SectionTitle, Dictionary<string, string>? Options)> English = new()
    {
        // ── IDEA-ASSESS ──
        [("IDEA-ASSESS", "innovation_q1")] = ("How differentiated is your idea compared to competitors?", "Innovation", null),
        [("IDEA-ASSESS", "innovation_q2")] = ("How much does your idea's solution improve the problem?", "Innovation", null),
        [("IDEA-ASSESS", "innovation_q3")] = ("How much does your idea use new technologies?", "Innovation", null),
        [("IDEA-ASSESS", "innovation_q4")] = ("How much potential does your idea have to create a new market?", "Innovation", null),
        [("IDEA-ASSESS", "innovation_q5")] = ("How easily can your idea be copied by competitors?", "Innovation", null),
        [("IDEA-ASSESS", "team_q1")] = ("How much relevant industry experience does your team have?", "Team", null),
        [("IDEA-ASSESS", "team_q2")] = ("How strong is your team's technical capability?", "Team", null),
        [("IDEA-ASSESS", "team_q3")] = ("How strong is your team's cohesion?", "Team", null),
        [("IDEA-ASSESS", "team_q4")] = ("How much access does your team have to advisors and experts?", "Team", null),
        [("IDEA-ASSESS", "team_q5")] = ("How committed is your team to the project's success?", "Team", null),
        [("IDEA-ASSESS", "team_q6")] = ("How diverse is the mix of expertise in your team?", "Team", null),
        [("IDEA-ASSESS", "technology_q1")] = ("How technically complex is your idea?", "Technology", null),
        [("IDEA-ASSESS", "technology_q2")] = ("How high are the technical barriers to implementing the idea?", "Technology", null),
        [("IDEA-ASSESS", "technology_q3")] = ("How sustainable is the technology in the long run?", "Technology", null),
        [("IDEA-ASSESS", "technology_q4")] = ("How scalable is the technology?", "Technology", null),
        [("IDEA-ASSESS", "technology_q5")] = ("How costly is the technology development?", "Technology", null),
        [("IDEA-ASSESS", "technology_q6")] = ("How secure and reliable is the technology?", "Technology", null),
        [("IDEA-ASSESS", "market_q1")] = ("How large is your target market?", "Market", null),
        [("IDEA-ASSESS", "market_q2")] = ("How fast is your target market growing?", "Market", null),
        [("IDEA-ASSESS", "market_q3")] = ("How intense is the customer's need for your solution?", "Market", null),
        [("IDEA-ASSESS", "market_q4")] = ("How strong is customers' ability to pay?", "Market", null),
        [("IDEA-ASSESS", "market_q5")] = ("How repeatable are customer purchases?", "Market", null),
        [("IDEA-ASSESS", "financial_q1")] = ("How profitable is your idea?", "Financial", null),
        [("IDEA-ASSESS", "financial_q2")] = ("How high is the return on investment (ROI)?", "Financial", null),
        [("IDEA-ASSESS", "financial_q3")] = ("How high is the project's financial risk?", "Financial", null),
        [("IDEA-ASSESS", "financial_q4")] = ("How high are the fixed and variable costs?", "Financial", null),
        [("IDEA-ASSESS", "financial_q5")] = ("How strong is the long-term profit potential?", "Financial", null),

        // ── INNOV-READ (KTH: 9 levels × 3 questions) ──
        [("INNOV-READ", "crl-q1")] = ("How many active users/customers do you have?", "Customer readiness (CRL)", new Dictionary<string, string> { ["1"] = "None (Level 1-2)", ["2"] = "Small number (Level 3-4)", ["3"] = "Substantial number (Level 5-6)", ["4"] = "Large and growing (Level 7-9)" }),
        [("INNOV-READ", "crl-q2")] = ("Have you made commercial sales?", "Customer readiness (CRL)", new Dictionary<string, string> { ["1"] = "No (Level 1-3)", ["2"] = "First sales (Level 4-6)", ["3"] = "Widespread sales (Level 7-9)" }),
        [("INNOV-READ", "crl-q3")] = ("What is the status of your sales process?", "Customer readiness (CRL)", new Dictionary<string, string> { ["1"] = "Not implemented (Level 1-3)", ["2"] = "Implemented (Level 4-6)", ["3"] = "Scalable and repeatable (Level 7-9)" }),
        [("INNOV-READ", "trl-q1")] = ("What is the current state of your technology?", "Technology readiness (TRL)", new Dictionary<string, string> { ["1"] = "Research idea (Level 1-2)", ["2"] = "Lab prototype (Level 3-4)", ["3"] = "Field tested (Level 5-6)", ["4"] = "Operational (Level 7-9)" }),
        [("INNOV-READ", "trl-q2")] = ("Have you tested in a realistic environment?", "Technology readiness (TRL)", new Dictionary<string, string> { ["1"] = "No (Level 1-3)", ["2"] = "Lab testing (Level 4-5)", ["3"] = "Real environment (Level 6-9)" }),
        [("INNOV-READ", "trl-q3")] = ("Is your technology complete and functional?", "Technology readiness (TRL)", new Dictionary<string, string> { ["1"] = "Incomplete (Level 1-4)", ["2"] = "Complete but unproven (Level 5-6)", ["3"] = "Complete and proven (Level 7-9)" }),
        [("INNOV-READ", "brl-q1")] = ("Have you validated your business model?", "Business readiness (BRL)", new Dictionary<string, string> { ["1"] = "No model (Level 1-2)", ["2"] = "Model described (Level 3-4)", ["3"] = "Model tested (Level 5-6)", ["4"] = "Model proven (Level 7-9)" }),
        [("INNOV-READ", "brl-q2")] = ("Do you have financial projections?", "Business readiness (BRL)", new Dictionary<string, string> { ["1"] = "No projections (Level 1-3)", ["2"] = "Initial projections (Level 4-5)", ["3"] = "Detailed projections (Level 6-9)" }),
        [("INNOV-READ", "brl-q3")] = ("Have you identified your target market?", "Business readiness (BRL)", new Dictionary<string, string> { ["1"] = "No market identified (Level 1-2)", ["2"] = "Market identified (Level 3-5)", ["3"] = "Market validated (Level 6-9)" }),
        [("INNOV-READ", "iprl-q1")] = ("Have you identified potential Intellectual Property?", "IPR readiness (IPRL)", new Dictionary<string, string> { ["1"] = "No IP identified (Level 1)", ["2"] = "IP identified (Level 2-3)", ["3"] = "IP protection strategy (Level 4-6)", ["4"] = "IP protected (Level 7-9)" }),
        [("INNOV-READ", "iprl-q2")] = ("Have you filed for IP protection?", "IPR readiness (IPRL)", new Dictionary<string, string> { ["1"] = "No filings (Level 1-3)", ["2"] = "Draft filed (Level 4-5)", ["3"] = "IP granted (Level 6-9)" }),
        [("INNOV-READ", "iprl-q3")] = ("Do you have a freedom-to-operate analysis?", "IPR readiness (IPRL)", new Dictionary<string, string> { ["1"] = "No analysis (Level 1-3)", ["2"] = "Initial analysis (Level 4-5)", ["3"] = "Complete analysis (Level 6-9)" }),
        [("INNOV-READ", "tmrl-q1")] = ("Do you have a founding team?", "Team readiness (TMRL)", new Dictionary<string, string> { ["1"] = "Individual (Level 1-2)", ["2"] = "Small team (Level 3-4)", ["3"] = "Complete team (Level 5-7)", ["4"] = "Organized structure (Level 8-9)" }),
        [("INNOV-READ", "tmrl-q2")] = ("Do you have all necessary competencies?", "Team readiness (TMRL)", new Dictionary<string, string> { ["1"] = "Limited competencies (Level 1-3)", ["2"] = "Key competencies (Level 4-6)", ["3"] = "All competencies (Level 7-9)" }),
        [("INNOV-READ", "tmrl-q3")] = ("Is the team committed?", "Team readiness (TMRL)", new Dictionary<string, string> { ["1"] = "Not committed (Level 1-2)", ["2"] = "Partially committed (Level 3-5)", ["3"] = "Fully committed (Level 6-9)" }),
        [("INNOV-READ", "frl-q1")] = ("Have you secured funding?", "Funding readiness (FRL)", new Dictionary<string, string> { ["1"] = "No funding (Level 1-2)", ["2"] = "Seed funding (Level 3-4)", ["3"] = "Series funding (Level 5-7)", ["4"] = "Sustainable funding (Level 8-9)" }),
        [("INNOV-READ", "frl-q2")] = ("Do you have a funding strategy?", "Funding readiness (FRL)", new Dictionary<string, string> { ["1"] = "No strategy (Level 1-3)", ["2"] = "Initial strategy (Level 4-5)", ["3"] = "Developed strategy (Level 6-9)" }),
        [("INNOV-READ", "frl-q3")] = ("Do you have financial monitoring?", "Funding readiness (FRL)", new Dictionary<string, string> { ["1"] = "No monitoring (Level 1-3)", ["2"] = "Basic monitoring (Level 4-6)", ["3"] = "Advanced monitoring (Level 7-9)" }),
        [("INNOV-READ", "lrl-q1")] = ("Is your solution legally compliant?", "Legal readiness (LRL)", new Dictionary<string, string> { ["1"] = "No compliance (Level 1-2)", ["2"] = "Compliance described (Level 3-4)", ["3"] = "Compliance validated (Level 5-7)", ["4"] = "Compliance proven (Level 8-9)" }),
        [("INNOV-READ", "lrl-q2")] = ("Have you completed a legal compliance audit?", "Legal readiness (LRL)", new Dictionary<string, string> { ["1"] = "No audit (Level 1-3)", ["2"] = "Audit planned (Level 4-5)", ["3"] = "Audit completed (Level 6-9)" }),
        [("INNOV-READ", "lrl-q3")] = ("Have you refined your solution within the legal system?", "Legal readiness (LRL)", new Dictionary<string, string> { ["1"] = "No refinement (Level 1-3)", ["2"] = "Initial refinement (Level 4-6)", ["3"] = "Refinement complete (Level 7-9)" }),
        [("INNOV-READ", "scrl-q1")] = ("Have you identified societal needs?", "Society readiness (SCRL)", new Dictionary<string, string> { ["1"] = "No identification (Level 1)", ["2"] = "Needs identified (Level 2-3)", ["3"] = "Needs validated (Level 4-6)", ["4"] = "Needs proven (Level 7-9)" }),
        [("INNOV-READ", "scrl-q2")] = ("Have you shared with stakeholders?", "Society readiness (SCRL)", new Dictionary<string, string> { ["1"] = "No sharing (Level 1-2)", ["2"] = "Initial sharing (Level 3-4)", ["3"] = "Extensive sharing (Level 5-9)" }),
        [("INNOV-READ", "scrl-q3")] = ("Have you demonstrated your solution with stakeholders?", "Society readiness (SCRL)", new Dictionary<string, string> { ["1"] = "No demonstration (Level 1-3)", ["2"] = "Controlled demo (Level 4-6)", ["3"] = "Real environment demo (Level 7-9)" }),
        [("INNOV-READ", "srl-q1")] = ("Are you aware of sustainability aspects?", "Sustainability readiness (SRL)", new Dictionary<string, string> { ["1"] = "No awareness (Level 1)", ["2"] = "Some awareness (Level 2)", ["3"] = "Awareness defined (Level 3-4)", ["4"] = "Awareness integrated (Level 5-9)" }),
        [("INNOV-READ", "srl-q2")] = ("Have you tested your business concept with sustainability?", "Sustainability readiness (SRL)", new Dictionary<string, string> { ["1"] = "No testing (Level 1-3)", ["2"] = "Initial testing (Level 4-5)", ["3"] = "Comprehensive testing (Level 6-9)" }),
        [("INNOV-READ", "srl-q3")] = ("Is sustainability part of your business model?", "Sustainability readiness (SRL)", new Dictionary<string, string> { ["1"] = "Not part (Level 1-3)", ["2"] = "Partially included (Level 4-6)", ["3"] = "Fully integrated (Level 7-9)" }),

        // ── WIPO-DIAG ──
        [("WIPO-DIAG", "tmRegistered")] = ("Have you registered a trademark with a national or regional IP office, or filed an application for one?", "Trademarks", YesNo),
        [("WIPO-DIAG", "tmUsage")] = ("Do you use your registered trademark to market your goods and services?", "Trademarks", YesNoNotSure),
        [("WIPO-DIAG", "tmInvestors")] = ("Are you interested in attracting investors to your company?", "Trademarks", YesNoNotSure),
        [("WIPO-DIAG", "tmLicense")] = ("Have you considered licensing your trademark to another party?", "Trademarks", YesNo),
        [("WIPO-DIAG", "tmLicenseObtain")] = ("Have you considered obtaining a license to use someone else's trademark in your business?", "Trademarks", YesNo),
        [("WIPO-DIAG", "tmInfringement")] = ("Do you believe others are using similar or identical marks for similar goods or services without your permission?", "Trademarks", YesNo),
        [("WIPO-DIAG", "tmExternal")] = ("Was your trademark designed for you by an outside party?", "Trademarks", YesNo),
        [("WIPO-DIAG", "confidentialProtection")] = ("Have you taken reasonable steps to protect business information you consider valuable?", "Confidential information", YesNoNotSure),
        [("WIPO-DIAG", "confidentialAccess")] = ("Is access to this information controlled (passwords, document shredding, confidential marking, etc.)?", "Confidential information", YesNo),
        [("WIPO-DIAG", "confidentialNDA")] = ("Are outside parties asked to sign non-disclosure agreements?", "Confidential information", YesNo),
        [("WIPO-DIAG", "confidentialSystems")] = ("Are systems in place to determine who can access confidential information?", "Confidential information", YesNoNotSure),
        [("WIPO-DIAG", "confidentialProduct")] = ("Is confidential information embodied in the products you produce?", "Confidential information", YesNoNotSure),
        [("WIPO-DIAG", "designRegistered")] = ("Have you registered an industrial design with an IP office, or filed an application for one?", "Industrial designs", YesNo),
        [("WIPO-DIAG", "designInvestors")] = ("Are you interested in attracting investors to your company?", "Industrial designs", YesNoNotSure),
        [("WIPO-DIAG", "designLicense")] = ("Have you considered licensing your design to another company?", "Industrial designs", YesNo),
        [("WIPO-DIAG", "designLicenseObtain")] = ("Have you considered obtaining a license to use another company's design?", "Industrial designs", YesNo),
        [("WIPO-DIAG", "designInfringement")] = ("Do you believe others are using your designs on their products without your permission?", "Industrial designs", YesNo),
        [("WIPO-DIAG", "designExternal")] = ("Was the design you use developed for you by an outside party?", "Industrial designs", YesNo),
        [("WIPO-DIAG", "patentGranted")] = ("Have you obtained a patent for your invention, or filed an application with a national, regional or international office?", "Inventions & innovation", YesNo),
        [("WIPO-DIAG", "patentDatabases")] = ("Are you aware of free patent databases?", "Inventions & innovation", YesNo),
        [("WIPO-DIAG", "inventiveInvestors")] = ("Are you interested in attracting investors to your company?", "Inventions & innovation", YesNoNotSure),
        [("WIPO-DIAG", "inventiveLicense")] = ("Have you considered licensing out your technology?", "Inventions & innovation", YesNo),
        [("WIPO-DIAG", "inventiveLicenseObtain")] = ("Have you considered licensing in someone else's technology?", "Inventions & innovation", YesNo),
        [("WIPO-DIAG", "inventiveInfringement")] = ("Do you believe others are using your technology without your consent?", "Inventions & innovation", YesNo),
        [("WIPO-DIAG", "inventiveExternal")] = ("Was your new technology developed for you by an outside party?", "Inventions & innovation", YesNo),
        [("WIPO-DIAG", "employeeInventions")] = ("Do your employees play a role in inventing new products or processes?", "Employees", YesNo),
        [("WIPO-DIAG", "employeeDesigns")] = ("Do your employees contribute to developing creative materials, designs or trademarks?", "Employees", YesNo),
        [("WIPO-DIAG", "employeeConfidential")] = ("Do your employment agreements include provisions for protecting confidential information?", "Employees", YesNo),
        [("WIPO-DIAG", "employeeTraining")] = ("Do you train your employees on protecting confidential information?", "Employees", YesNo),
        [("WIPO-DIAG", "employeeHiring")] = ("When hiring, do you make sure candidates do not bring a previous employer's trade secrets?", "Employees", YesNo),
        [("WIPO-DIAG", "domainRegistered")] = ("Have you registered a domain name for your website?", "Website", YesNo),
        [("WIPO-DIAG", "domainTrademark")] = ("Is your domain name your trademark?", "Website", YesNo),
        [("WIPO-DIAG", "domainTrademarkOther")] = ("Is your domain name a trademark registered by someone else?", "Website", YesNo),
        [("WIPO-DIAG", "websiteDevelopment")] = ("Did you develop your website independently?", "Website", YesNo),
        [("WIPO-DIAG", "websiteMaterials")] = ("Were the materials uploaded to the website developed by your company?", "Website", YesNo),
        [("WIPO-DIAG", "websiteLinks")] = ("Does your website contain links to materials on other websites?", "Website", YesNo),
        [("WIPO-DIAG", "websiteUsers")] = ("Do users upload materials to your website?", "Website", YesNo),

        // ── IPSCORE ──
        [("IPSCORE", "A1")] = ("What is the registration status of the patent?", "A — Legal status", null),
        [("IPSCORE", "A2")] = ("How sound is the patent's position and legal strength?", "A — Legal status", null),
        [("IPSCORE", "A3")] = ("How much longer does the patent remain in force?", "A — Legal status", null),
        [("IPSCORE", "A4")] = ("How broad and comprehensive are the patent claims?", "A — Legal status", null),
        [("IPSCORE", "A5")] = ("Does the patent's geographic coverage include your target markets?", "A — Legal status", null),
        [("IPSCORE", "A6")] = ("Is regular monitoring performed to identify patent infringement?", "A — Legal status", null),
        [("IPSCORE", "A7")] = ("Are patent lawsuits common in the relevant market?", "A — Legal status", null),
        [("IPSCORE", "A8")] = ("Does the company have the ability to pursue and enforce its patent rights?", "A — Legal status", null),
        [("IPSCORE", "B1")] = ("Is the invention a unique technology?", "B — Technology", null),
        [("IPSCORE", "B2")] = ("Is the invention technically superior to alternative technologies?", "B — Technology", null),
        [("IPSCORE", "B3")] = ("To what extent has the invention been tested and trialled?", "B — Technology", null),
        [("IPSCORE", "B4")] = ("Does the technology require new skills or equipment?", "B — Technology", null),
        [("IPSCORE", "B5")] = ("How long is needed before full commercialisation?", "B — Technology", null),
        [("IPSCORE", "B6")] = ("Is it easy to produce copycat products of the invention?", "B — Technology", null),
        [("IPSCORE", "B7")] = ("Is it easy to identify products that infringe the patent?", "B — Technology", null),
        [("IPSCORE", "B8")] = ("Does implementing the technology depend on others' licenses?", "B — Technology", null),
        [("IPSCORE", "B9")] = ("Does the technology have market value (value for the customer)?", "B — Technology", null),
        [("IPSCORE", "C1")] = ("What are the marketing options and methods?", "C — Market", null),
        [("IPSCORE", "C2")] = ("What is the growth rate of the market for this technology's use?", "C — Market", null),
        [("IPSCORE", "C3")] = ("What is the technology's expected useful life in the market?", "C — Market", null),
        [("IPSCORE", "C4")] = ("Are competing or substitute products active in the market?", "C — Market", null),
        [("IPSCORE", "C5")] = ("How does the final price paid by customers compare to existing products?", "C — Market", null),
        [("IPSCORE", "C6")] = ("What is the additional turnover potential from applying the technology?", "C — Market", null),
        [("IPSCORE", "C7")] = ("How well does the company understand the application potential and commercial opportunities?", "C — Market", null),
        [("IPSCORE", "C8")] = ("Does the patent have licensing revenue potential?", "C — Market", null),
        [("IPSCORE", "C9")] = ("Do the commercial activities require special permits?", "C — Market", null),
        [("IPSCORE", "D1")] = ("Can the market's current output be maintained without the patented technology?", "D — Financial", null),
        [("IPSCORE", "D2")] = ("How high are the required future development costs?", "D — Financial", null),
        [("IPSCORE", "D3")] = ("What is the production cost index when the patented technology is used?", "D — Financial", null),
        [("IPSCORE", "D4")] = ("How much investment is required for production equipment?", "D — Financial", null),
        [("IPSCORE", "D5")] = ("Can the company afford the patent renewal fees?", "D — Financial", null),
        [("IPSCORE", "D6")] = ("How much of the company's profit comes from the patented technology?", "D — Financial", null),
        [("IPSCORE", "E1")] = ("Is the patent's purpose to consolidate a position in existing markets?", "E — Strategy", null),
        [("IPSCORE", "E2")] = ("Is the patent's purpose to open new markets?", "E — Strategy", null),
        [("IPSCORE", "E3")] = ("Is the patent part of a credibility and branding process?", "E — Strategy", null),
        [("IPSCORE", "E4")] = ("Is the patent's purpose to secure freedom to operate?", "E — Strategy", null),
        [("IPSCORE", "E5")] = ("Is the patent's purpose to hinder competitors' development?", "E — Strategy", null),
        [("IPSCORE", "E6")] = ("Does the company use the patent in licensing or sales agreements?", "E — Strategy", null),
        [("IPSCORE", "E7")] = ("Is the patent part of the company's key, core technologies?", "E — Strategy", null),
        [("IPSCORE", "E8")] = ("Is there full alignment between the patent and the business strategy?", "E — Strategy", null),

        // ── JOB-EVAL (pick-lists; the client posts tags, options are display-only) ──
        [("JOB-EVAL", "alwaysValues")] = ("Vital values (driving forces) — choose up to 5", "16 core values", JobValueLabels),
        [("JOB-EVAL", "neverValues")] = ("Least important values — choose up to 5", "16 core values", JobValueLabels),
    };

    // ─── Content mirrors the client data files (fa) ─────────────────────

    private static (string ToolCode, string Id, string SectionKey, string SectionTitle, string Text, string OptionsJson, int Sort) Def(
        string tool, string id, string section, string title, string text, int sort, string[]? options = null, int optionsStart = 0)
        => (tool, id, section, title, text, options is null ? "[]" : Options(options, optionsStart), sort);

    /// <summary><paramref name="start"/> is the value of the first option (WIPO/yes-no is 0-based).</summary>
    private static string Options(string[] labels, int start = 0) =>
        System.Text.Json.JsonSerializer.Serialize(
            labels.Select((l, i) => new { value = i + start, label = l }).ToArray());

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

        // ── INNOV-READ: KTH questionnaire — 27 questions, 9 readiness levels × 3 ──
        // Option values are 1-based (they are the raw option picked on the client).
        Def("INNOV-READ", "crl-q1", "crl", "آمادگی مشتری (CRL)", "تعداد کاربران/مشتریان فعال شما چقدر است؟", 1,
            ["هیچ (سطح ۱-۲)", "تعداد کم (سطح ۳-۴)", "تعداد قابل توجه (سطح ۵-۶)", "زیاد و در حال رشد (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "crl-q2", "crl", "آمادگی مشتری (CRL)", "آیا فروش تجاری داشته‌اید؟", 2,
            ["خیر (سطح ۱-۳)", "فروش اولیه (سطح ۴-۶)", "فروش گسترده (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "crl-q3", "crl", "آمادگی مشتری (CRL)", "وضعیت فرآیند فروش شما چیست؟", 3,
            ["پیاده‌سازی نشده (سطح ۱-۳)", "پیاده‌سازی شده (سطح ۴-۶)", "مقیاس‌پذیر و تکرارپذیر (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "trl-q1", "trl", "آمادگی فناوری (TRL)", "وضعیت فعلی فناوری شما چیست؟", 4,
            ["ایده تحقیقاتی (سطح ۱-۲)", "نمونه اولیه آزمایشگاهی (سطح ۳-۴)", "آزمون‌شده در میدان (سطح ۵-۶)", "عملیاتی (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "trl-q2", "trl", "آمادگی فناوری (TRL)", "آیا در محیط واقعی آزمایش کرده‌اید؟", 5,
            ["خیر (سطح ۱-۳)", "آزمون آزمایشگاهی (سطح ۴-۵)", "محیط واقعی (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "trl-q3", "trl", "آمادگی فناوری (TRL)", "آیا فناوری شما کامل و کارآمد است؟", 6,
            ["ناقص (سطح ۱-۴)", "کامل اما اثبات‌نشده (سطح ۵-۶)", "کامل و اثبات‌شده (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "brl-q1", "brl", "آمادگی کسب‌وکار (BRL)", "آیا مدل کسب‌وکار خود را اعتبارسنجی کرده‌اید؟", 7,
            ["بدون مدل (سطح ۱-۲)", "مدل توصیف‌شده (سطح ۳-۴)", "مدل آزمون‌شده (سطح ۵-۶)", "مدل اثبات‌شده (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "brl-q2", "brl", "آمادگی کسب‌وکار (BRL)", "آیا پیش‌بینی مالی دارید؟", 8,
            ["بدون پیش‌بینی (سطح ۱-۳)", "پیش‌بینی اولیه (سطح ۴-۵)", "پیش‌بینی تفصیلی (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "brl-q3", "brl", "آمادگی کسب‌وکار (BRL)", "آیا بازار هدف خود را شناسایی کرده‌اید؟", 9,
            ["بازار شناسایی نشده (سطح ۱-۲)", "بازار شناسایی‌شده (سطح ۳-۵)", "بازار اعتبارسنجی‌شده (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "iprl-q1", "iprl", "آمادگی مالکیت فکری (IPRL)", "آیا مالکیت فکری بالقوه را شناسایی کرده‌اید؟", 10,
            ["مالکیت فکری شناسایی نشده (سطح ۱)", "مالکیت فکری شناسایی‌شده (سطح ۲-۳)", "راهبرد حفاظت از مالکیت فکری (سطح ۴-۶)", "مالکیت فکری حفاظت‌شده (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "iprl-q2", "iprl", "آمادگی مالکیت فکری (IPRL)", "آیا برای حفاظت از مالکیت فکری درخواست ثبت داده‌اید؟", 11,
            ["بدون ثبت (سطح ۱-۳)", "پیش‌نویس ثبت‌شده (سطح ۴-۵)", "مالکیت فکری اعطاشده (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "iprl-q3", "iprl", "آمادگی مالکیت فکری (IPRL)", "آیا تحلیل آزادی عمل (Freedom to Operate) دارید؟", 12,
            ["بدون تحلیل (سطح ۱-۳)", "تحلیل اولیه (سطح ۴-۵)", "تحلیل کامل (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "tmrl-q1", "tmrl", "آمادگی تیم (TMRL)", "آیا تیم بنیان‌گذار دارید؟", 13,
            ["فردی (سطح ۱-۲)", "تیم کوچک (سطح ۳-۴)", "تیم کامل (سطح ۵-۷)", "ساختار سازمان‌یافته (سطح ۸-۹)"], 1),
        Def("INNOV-READ", "tmrl-q2", "tmrl", "آمادگی تیم (TMRL)", "آیا تمام شایستگی‌های لازم را دارید؟", 14,
            ["شایستگی‌های محدود (سطح ۱-۳)", "شایستگی‌های کلیدی (سطح ۴-۶)", "همه شایستگی‌ها (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "tmrl-q3", "tmrl", "آمادگی تیم (TMRL)", "آیا تیم متعهد است؟", 15,
            ["غیرمتعهد (سطح ۱-۲)", "تا حدی متعهد (سطح ۳-۵)", "کاملاً متعهد (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "frl-q1", "frl", "آمادگی تأمین مالی (FRL)", "آیا تأمین مالی کرده‌اید؟", 16,
            ["بدون تأمین مالی (سطح ۱-۲)", "سرمایه بذری (سطح ۳-۴)", "سرمایه‌گذاری مرحله‌ای (سطح ۵-۷)", "تأمین مالی پایدار (سطح ۸-۹)"], 1),
        Def("INNOV-READ", "frl-q2", "frl", "آمادگی تأمین مالی (FRL)", "آیا راهبرد تأمین مالی دارید؟", 17,
            ["بدون راهبرد (سطح ۱-۳)", "راهبرد اولیه (سطح ۴-۵)", "راهبرد توسعه‌یافته (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "frl-q3", "frl", "آمادگی تأمین مالی (FRL)", "آیا پایش مالی دارید؟", 18,
            ["بدون پایش (سطح ۱-۳)", "پایش پایه (سطح ۴-۶)", "پایش پیشرفته (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "lrl-q1", "lrl", "آمادگی قانونی (LRL)", "آیا راهکار شما از نظر قانونی منطبق است؟", 19,
            ["بدون انطباق (سطح ۱-۲)", "انطباق توصیف‌شده (سطح ۳-۴)", "انطباق اعتبارسنجی‌شده (سطح ۵-۷)", "انطباق اثبات‌شده (سطح ۸-۹)"], 1),
        Def("INNOV-READ", "lrl-q2", "lrl", "آمادگی قانونی (LRL)", "آیا ممیزی انطباق قانونی انجام داده‌اید؟", 20,
            ["بدون ممیزی (سطح ۱-۳)", "ممیزی برنامه‌ریزی‌شده (سطح ۴-۵)", "ممیزی انجام‌شده (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "lrl-q3", "lrl", "آمادگی قانونی (LRL)", "آیا راهکار خود را در چارچوب قانونی اصلاح کرده‌اید؟", 21,
            ["بدون اصلاح (سطح ۱-۳)", "اصلاح اولیه (سطح ۴-۶)", "اصلاح کامل (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "scrl-q1", "scrl", "آمادگی اجتماعی (SCRL)", "آیا نیازهای اجتماعی را شناسایی کرده‌اید؟", 22,
            ["بدون شناسایی (سطح ۱)", "نیازها شناسایی‌شده (سطح ۲-۳)", "نیازها اعتبارسنجی‌شده (سطح ۴-۶)", "نیازها اثبات‌شده (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "scrl-q2", "scrl", "آمادگی اجتماعی (SCRL)", "آیا با ذی‌نفعان به اشتراک گذاشته‌اید؟", 23,
            ["بدون اشتراک‌گذاری (سطح ۱-۲)", "اشتراک‌گذاری اولیه (سطح ۳-۴)", "اشتراک‌گذاری گسترده (سطح ۵-۹)"], 1),
        Def("INNOV-READ", "scrl-q3", "scrl", "آمادگی اجتماعی (SCRL)", "آیا راهکار خود را به ذی‌نفعان نشان داده‌اید؟", 24,
            ["بدون نمایش (سطح ۱-۳)", "نمایش کنترل‌شده (سطح ۴-۶)", "نمایش در محیط واقعی (سطح ۷-۹)"], 1),
        Def("INNOV-READ", "srl-q1", "srl", "آمادگی پایداری (SRL)", "آیا از جنبه‌های پایداری آگاه هستید؟", 25,
            ["بدون آگاهی (سطح ۱)", "آگاهی اندک (سطح ۲)", "آگاهی تعریف‌شده (سطح ۳-۴)", "آگاهی یکپارچه (سطح ۵-۹)"], 1),
        Def("INNOV-READ", "srl-q2", "srl", "آمادگی پایداری (SRL)", "آیا مفهوم کسب‌وکار خود را با رویکرد پایداری آزموده‌اید؟", 26,
            ["بدون آزمون (سطح ۱-۳)", "آزمون اولیه (سطح ۴-۵)", "آزمون جامع (سطح ۶-۹)"], 1),
        Def("INNOV-READ", "srl-q3", "srl", "آمادگی پایداری (SRL)", "آیا پایداری بخشی از مدل کسب‌وکار شماست؟", 27,
            ["بخشی از مدل نیست (سطح ۱-۳)", "تا حدی شامل شده (سطح ۴-۶)", "کاملاً یکپارچه (سطح ۷-۹)"], 1),

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
