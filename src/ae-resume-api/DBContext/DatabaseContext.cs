﻿using Microsoft.EntityFrameworkCore;

namespace ae_resume_api.DBContext
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Add associative table key
            builder.Entity<TemplateSectorEntity>().
                HasKey(x => new { x.TemplateId, x.TypeId });

            // Add unique name constraints
            builder.Entity<SectorTypeEntity>().
                HasIndex(st => st.Title).IsUnique();

            // Add unique proposal number constraints
            builder.Entity<WorkspaceEntity>().
                HasIndex(w => w.Proposal_Number).IsUnique();

            // Force cascade on workspaces
            builder.Entity<WorkspaceEntity>()
                .HasMany(e => e.Resumes)
                .WithOne(e => e.Workspace)
                .OnDelete(DeleteBehavior.ClientCascade);
        }

        // Entity tables
        public DbSet<EmployeeEntity> Employee { get; set; } = null!;
        public DbSet<ResumeEntity> Resume { get; set; } = null!;
        public DbSet<SectorEntity> Sector { get; set; } = null!;
        public DbSet<SectorTypeEntity> SectorType { get; set; } = null!;
        public DbSet<TemplateEntity> Template { get; set; } = null!;
        public DbSet<TemplateSectorEntity> TemplateSector { get; set; } = null!;
        public DbSet<WorkspaceEntity> Workspace { get; set; } = null!;

        public new async Task<int> SaveChanges()
        {
            return await base.SaveChangesAsync();
        }
    }
}