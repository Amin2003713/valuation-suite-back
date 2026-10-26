namespace Domain.Tools;

/* ═══════════════════════════════════════════════════════════════════════
 * One submissions table per tool.
 *
 * Every tool code registered in ToolFormsSeeder gets an empty concrete
 * subclass of ToolSubmissionBase; EF maps each class to its own table
 * (name = class name pluralized by the project's table-name convention,
 * e.g. IdeaAssessmentSubmission → IdeaAssessmentSubmissions).
 *
 * Adding a tool = one class here + one registry line (ToolSubmissionRegistry)
 * + one migration. All columns/indexes/audit rules come from the base.
 * ═══════════════════════════════════════════════════════════════════════ */

public sealed class IdeaAssessmentSubmission : ToolSubmissionBase;

public sealed class InnovationReadinessSubmission : ToolSubmissionBase;

public sealed class IpAuditSubmission : ToolSubmissionBase;

public sealed class JobEvaluationSubmission : ToolSubmissionBase;

public sealed class WipoDiagnosticsSubmission : ToolSubmissionBase;

public sealed class IpscoreSubmission : ToolSubmissionBase;


public sealed class StartupValuationSubmission : ToolSubmissionBase;

public sealed class BrandValuationSubmission : ToolSubmissionBase;

public sealed class StartupAssessmentSubmission : ToolSubmissionBase;

public sealed class TrademarkValuationSubmission : ToolSubmissionBase;

public sealed class PatentValuationSubmission : ToolSubmissionBase;

public sealed class PharmaPatentValuationSubmission : ToolSubmissionBase;

public sealed class IntangibleAssetsSubmission : ToolSubmissionBase;

public sealed class IconScorecardSubmission : ToolSubmissionBase;

public sealed class BinomialValuationSubmission : ToolSubmissionBase;

public sealed class SoftwareValuationSubmission : ToolSubmissionBase;

public sealed class RnpvCalculatorSubmission : ToolSubmissionBase;

public sealed class PatentValueIndexSubmission : ToolSubmissionBase;

public sealed class KnowhowProSubmission : ToolSubmissionBase;

public sealed class ExportReadinessSubmission : ToolSubmissionBase;

public sealed class DigitalAiReadinessSubmission : ToolSubmissionBase;

public sealed class MaturityAssessmentSubmission : ToolSubmissionBase;
