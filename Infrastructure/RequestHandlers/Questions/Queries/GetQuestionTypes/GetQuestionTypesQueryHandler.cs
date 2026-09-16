using Application.Questions.Queries.GetQuestionTypes;

namespace RequestHandlers.Questions.Queries.GetQuestionTypes;

public sealed class GetQuestionTypesQueryHandler
    : IRequestHandler<GetQuestionTypesQuery, List<QuestionType>>
{
    public Task<List<QuestionType>> Handle(GetQuestionTypesQuery request, CancellationToken ct)
        => Task.FromResult(Enum.GetValues<QuestionType>().ToList());
}

public sealed class GetQuestionTypeDetailsQueryHandler
    : IRequestHandler<GetQuestionTypeDetailsQuery, List<QuestionTypeInfo>>
{
    public Task<List<QuestionTypeInfo>> Handle(GetQuestionTypeDetailsQuery request, CancellationToken ct)
    {
        var types = new List<QuestionTypeInfo>
        {
            new(QuestionType.Text, "Text", "Short text input", SupportsCalculation: false, SupportsConditional: true),
            new(QuestionType.LongText, "Long Text", "Multi-line text input", SupportsCalculation: false, SupportsConditional: true),
            new(QuestionType.Number, "Number", "Numeric input", SupportsCalculation: true, SupportsConditional: true),
            new(QuestionType.Decimal, "Decimal", "Decimal numeric input", SupportsCalculation: true, SupportsConditional: true),
            new(QuestionType.Boolean, "Boolean", "Yes/No toggle", SupportsCalculation: false, SupportsConditional: true),
            new(QuestionType.SingleChoice, "Single Choice", "Single selection list", SupportsCalculation: true, SupportsConditional: true),
            new(QuestionType.MultipleChoice, "Multiple Choice", "Multiple selection list", SupportsCalculation: true, SupportsConditional: true),
            new(QuestionType.Dropdown, "Dropdown", "Dropdown selection", SupportsCalculation: true, SupportsConditional: true),
            new(QuestionType.Date, "Date", "Date picker", SupportsCalculation: false, SupportsConditional: true),
            new(QuestionType.Rating, "Rating", "Rating scale (0-10)", SupportsCalculation: true, SupportsConditional: true),
            new(QuestionType.Mathematics, "Mathematics", "Calculated numeric expression", SupportsCalculation: true, SupportsConditional: false),
            new(QuestionType.Information, "Information", "Read-only info block", SupportsCalculation: false, SupportsConditional: false),
            new(QuestionType.Conditional, "Conditional", "Conditionally visible question", SupportsCalculation: false, SupportsConditional: true),
        };

        return Task.FromResult(types);
    }
}
