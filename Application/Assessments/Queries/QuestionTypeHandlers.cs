using MediatR;
using Domain.Assessments;
using Domain.Evaluation;
using Domain.Common;

namespace Application.Assessments.Queries;

public class GetQuestionTypesHandler : IRequestHandler<GetQuestionTypesQuery, List<QuestionType>>
{
    public Task<List<QuestionType>> Handle(GetQuestionTypesQuery request, CancellationToken ct)
        => Task.FromResult(Enum.GetValues<QuestionType>().ToList());
}

public class GetQuestionTypeDetailsHandler : IRequestHandler<GetQuestionTypeDetailsQuery, List<QuestionTypeInfo>>
{
    public Task<List<QuestionTypeInfo>> Handle(GetQuestionTypeDetailsQuery request, CancellationToken ct)
        => Task.FromResult(new List<QuestionTypeInfo>
        {
            new(QuestionType.Text, "Text", "Short text answer", false, false),
            new(QuestionType.LongText, "Long Text", "Paragraph text answer", false, false),
            new(QuestionType.Number, "Number", "Numeric integer answer", true, true),
            new(QuestionType.Decimal, "Decimal", "Decimal number answer", true, true),
            new(QuestionType.Boolean, "Boolean", "Yes/No answer", false, false),
            new(QuestionType.SingleChoice, "Single Choice", "Pick one option", false, false),
            new(QuestionType.MultipleChoice, "Multiple Choice", "Pick multiple options", false, false),
            new(QuestionType.Dropdown, "Dropdown", "Select from dropdown", false, false),
            new(QuestionType.Date, "Date", "Date picker", false, false),
            new(QuestionType.Rating, "Rating", "Star rating", true, false),
            new(QuestionType.Mathematics, "Mathematics", "Mathematical calculation", true, true),
            new(QuestionType.Information, "Information", "Display-only information", false, false),
            new(QuestionType.Conditional, "Conditional", "Conditional display logic", false, false),
        });
}
