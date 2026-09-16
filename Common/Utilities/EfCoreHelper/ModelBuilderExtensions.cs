using Common.Base;
using Pluralize.NET;

namespace Common.Utilities.EfCoreHelper;

public static class ModelBuilderExtensions
{
    extension(ModelBuilder modelBuilder)
    {
        public void AddPluralizingTableNameConvention()
        {
            var pluralizer = new Pluralizer();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrEmpty(tableName))
                    continue;

                entityType.SetTableName(pluralizer.Pluralize(tableName));
            }
        }

        public void AddDecimalConvention()
        {
            var decimalProps = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(decimal));

            foreach (var property in decimalProps)
            {
                property.SetPrecision(18);
                property.SetScale(4);
            }
        }

        public void AddGlobalIsActiveFilter()
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;

                // Only for IEntity
                if (!typeof(IEntity).IsAssignableFrom(clrType))
                    continue;

                // IMPORTANT: only apply on EF root types (not CLR base type)
                if (entityType.BaseType != null)
                    continue;

                // find IsActive / IsDeleted (on type or base types)
                var isActiveProp = FindBoolProperty(clrType, "IsActive");
                var isDeletedProp = FindBoolProperty(clrType, "IsDeleted");

                if (isActiveProp == null && isDeletedProp == null)
                    continue;

                var param = Expression.Parameter(clrType, "e");

                Expression? predicate = null;

                if (isActiveProp != null)
                {
                    var isActiveExpr = Expression.Equal(
                        Expression.Property(param, isActiveProp),
                        Expression.Constant(true));

                    predicate = isActiveExpr;
                }

                if (isDeletedProp != null)
                {
                    var isDeletedExpr = Expression.Equal(
                        Expression.Property(param, isDeletedProp),
                        Expression.Constant(false));

                    predicate = predicate != null
                        ? Expression.AndAlso(predicate, isDeletedExpr)
                        : isDeletedExpr;
                }

                if (predicate == null)
                    continue;

                var lambda = Expression.Lambda(predicate, param);
                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }

            return;

            static PropertyInfo? FindBoolProperty(Type type, string name)
            {
                // Look on type + base types (public instance)
                while (type != null)
                {
                    var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (p is { PropertyType: var pt } && pt == typeof(bool))
                        return p;

                    type = type.BaseType!;
                }

                return null;
            }
        }

        public void AddRestrictDeleteBehaviorConvention()
        {
            var cascadeFKs = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => fk is { IsOwnership: false, DeleteBehavior: DeleteBehavior.Cascade });

            foreach (var fk in cascadeFKs)
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        public void RegisterAllEntities(params Assembly[] assemblies)
        {
            var excludedNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "IEntity", "BaseEntity", "BaseEntities"
            };

            var types = assemblies.SelectMany(a => a.GetExportedTypes())
                .Where(c =>
                    c.IsClass &&
                    !c.IsAbstract &&
                    c.IsPublic &&
                    typeof(IEntity).IsAssignableFrom(c) &&
                    !excludedNames.Contains(c.Name) &&
                    !c.Name.Contains("BaseEntity", StringComparison.Ordinal) &&
                    !c.Name.Contains("BaseEntities", StringComparison.Ordinal)
                );

            foreach (var type in types)
                modelBuilder.Entity(type);
        }
    }
}
