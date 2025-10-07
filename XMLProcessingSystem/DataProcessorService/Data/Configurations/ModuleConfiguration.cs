using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataProcessorService.Data.Configurations
{
    public class ModuleConfiguration : IEntityTypeConfiguration<Module>
    {
        public void Configure(EntityTypeBuilder<Module> builder)
        {
            builder.ToTable("modules");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.ModuleCategoryID)
                .HasColumnName("module_category_id");

            builder.Property(m => m.ModuleState)
                .HasColumnName("module_state");

            builder.Property(m => m.IndexWithinRole)
                .HasColumnName("index_within_role")
                .IsRequired(false);

            builder.Property(m => m.PackageID)
                .HasColumnName("package_id");

            builder.HasIndex(i => new { i.PackageID, i.ModuleCategoryID, i.IndexWithinRole })
                .IsUnique();
        }
    }
}
