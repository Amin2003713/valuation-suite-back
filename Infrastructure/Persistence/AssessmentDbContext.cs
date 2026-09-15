using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Configurations;
using Domain.Assessments;
using Domain.Common;
using Domain.Companies;
using Domain.Users;
using Domain.Attempts;
using Domain.Answers;
using Domain.Results;

namespace Infrastructure.Persistence;

public partial class AssessmentDbContext : DbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentVersion> AssessmentVersions => Set<AssessmentVersion>();
    public DbSet<Step> Steps => Set<Step>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<ValidationRule> ValidationRules => Set<ValidationRule>();
    public DbSet<VisibilityCondition> VisibilityConditions => Set<VisibilityCondition>();
    public DbSet<MathExpression> MathExpressions => Set<MathExpression>();
    public DbSet<MathVariable> MathVariables => Set<MathVariable>();
    public DbSet<MathOperation> MathOperations => Set<MathOperation>();
    public DbSet<CalculationConfig> CalculationConfigs => Set<CalculationConfig>();
    public DbSet<Domain.Users.User> Users => Set<Domain.Users.User>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();

    public AssessmentDbContext(DbContextOptions<AssessmentDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=ValuationSuite;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new AssessmentConfiguration());
        modelBuilder.ApplyConfiguration(new AssessmentVersionConfiguration());
        modelBuilder.ApplyConfiguration(new StepConfiguration());
        modelBuilder.ApplyConfiguration(new QuestionConfiguration());
        modelBuilder.ApplyConfiguration(new OptionConfiguration());
        modelBuilder.ApplyConfiguration(new ValidationRuleConfiguration());
        modelBuilder.ApplyConfiguration(new VisibilityConditionConfiguration());
        modelBuilder.ApplyConfiguration(new MathExpressionConfiguration());
        modelBuilder.ApplyConfiguration(new MathVariableConfiguration());
        modelBuilder.ApplyConfiguration(new MathOperationConfiguration());
        modelBuilder.ApplyConfiguration(new CalculationConfigConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AssessmentAttemptConfiguration());
        modelBuilder.ApplyConfiguration(new AnswerConfiguration());
        modelBuilder.ApplyConfiguration(new AssessmentResultConfiguration());
    }
}
