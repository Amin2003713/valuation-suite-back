using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Application.Common;
using Application.Assessments;
using Application.Assessments.Commands;
using Application.Assessments.Queries;
using Application.Attempts.Commands;
using Application.Results.Commands;
using Application.Companies.Commands;
using Application.Companies.Queries;

namespace Infrastructure;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssessmentDbContext>());

        services.AddScoped<IAssessmentRepository, Infrastructure.Persistence.Repositories.AssessmentRepository>();
        services.AddScoped<IAssessmentVersionRepository, Infrastructure.Persistence.Repositories.AssessmentVersionRepository>();
        services.AddScoped<IAssessmentAttemptRepository, Infrastructure.Persistence.Repositories.AssessmentAttemptRepository>();
        services.AddScoped<IAnswerRepository, Infrastructure.Persistence.Repositories.AnswerRepository>();
        services.AddScoped<ICompanyRepository, Infrastructure.Persistence.Repositories.CompanyRepository>();
        services.AddScoped<IUserRepository, Infrastructure.Persistence.Repositories.UserRepository>();
        services.AddScoped<IUnitOfWork, Infrastructure.Persistence.UnitOfWork>();

        services.AddSingleton<IQuestionEngine, Application.Assessments.QuestionEngine>();
        services.AddSingleton<IMathEngine, Application.Assessments.MathEngine>();

        return services;
    }
}
