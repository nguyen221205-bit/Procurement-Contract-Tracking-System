using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Infrastructure.Entities;

namespace ProcurementSystem.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Contractor> Contractors => Set<Contractor>();
        public DbSet<BidPackage> BidPackages => Set<BidPackage>();
        public DbSet<BidDocument> BidDocuments => Set<BidDocument>();
        public DbSet<BidSubmission> BidSubmissions => Set<BidSubmission>();
        public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
        public DbSet<EvaluationCriteria> EvaluationCriteria => Set<EvaluationCriteria>();
        public DbSet<EvaluationScore> EvaluationScores => Set<EvaluationScore>();
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<ContractMilestone> ContractMilestones => Set<ContractMilestone>();
        public DbSet<ProgressUpdate> ProgressUpdates => Set<ProgressUpdate>();
        public DbSet<Acceptance> Acceptances => Set<Acceptance>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserRole - composite key (N-N)
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - unique email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Contractor - one-to-one with User
            modelBuilder.Entity<Contractor>()
                .HasOne(c => c.User)
                .WithOne(u => u.Contractor)
                .HasForeignKey<Contractor>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // BidPackage - unique code
            modelBuilder.Entity<BidPackage>()
                .HasIndex(bp => bp.Code)
                .IsUnique();

            modelBuilder.Entity<BidPackage>()
                .HasOne(bp => bp.Creator)
                .WithMany()
                .HasForeignKey(bp => bp.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // BidDocument -> BidPackage
            modelBuilder.Entity<BidDocument>()
                .HasOne(bd => bd.BidPackage)
                .WithMany(bp => bp.BidDocuments)
                .HasForeignKey(bd => bd.BidPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            // BidSubmission
            modelBuilder.Entity<BidSubmission>()
                .HasOne(bs => bs.BidPackage)
                .WithMany(bp => bp.BidSubmissions)
                .HasForeignKey(bs => bs.BidPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BidSubmission>()
                .HasOne(bs => bs.Contractor)
                .WithMany(c => c.BidSubmissions)
                .HasForeignKey(bs => bs.ContractorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one contractor per bid package
            modelBuilder.Entity<BidSubmission>()
                .HasIndex(bs => new { bs.BidPackageId, bs.ContractorId })
                .IsUnique();

            // SubmissionFile -> BidSubmission
            modelBuilder.Entity<SubmissionFile>()
                .HasOne(sf => sf.BidSubmission)
                .WithMany(bs => bs.SubmissionFiles)
                .HasForeignKey(sf => sf.BidSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // EvaluationCriteria -> BidPackage
            modelBuilder.Entity<EvaluationCriteria>()
                .HasOne(ec => ec.BidPackage)
                .WithMany(bp => bp.EvaluationCriteria)
                .HasForeignKey(ec => ec.BidPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            // EvaluationScore
            modelBuilder.Entity<EvaluationScore>()
                .HasOne(es => es.BidSubmission)
                .WithMany(bs => bs.EvaluationScores)
                .HasForeignKey(es => es.BidSubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluationScore>()
                .HasOne(es => es.Criteria)
                .WithMany(ec => ec.EvaluationScores)
                .HasForeignKey(es => es.CriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluationScore>()
                .HasOne(es => es.Evaluator)
                .WithMany()
                .HasForeignKey(es => es.EvaluatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique: one evaluator per submission per criteria
            modelBuilder.Entity<EvaluationScore>()
                .HasIndex(es => new { es.BidSubmissionId, es.CriteriaId, es.EvaluatorId })
                .IsUnique();

            // Contract
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.BidPackage)
                .WithOne(bp => bp.Contract)
                .HasForeignKey<Contract>(c => c.BidPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Contractor)
                .WithMany(co => co.Contracts)
                .HasForeignKey(c => c.ContractorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.ContractNumber)
                .IsUnique();

            // ContractMilestone -> Contract
            modelBuilder.Entity<ContractMilestone>()
                .HasOne(cm => cm.Contract)
                .WithMany(c => c.Milestones)
                .HasForeignKey(cm => cm.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProgressUpdate -> Contract
            modelBuilder.Entity<ProgressUpdate>()
                .HasOne(pu => pu.Contract)
                .WithMany(c => c.ProgressUpdates)
                .HasForeignKey(pu => pu.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique: one progress update per contract per week
            modelBuilder.Entity<ProgressUpdate>()
                .HasIndex(pu => new { pu.ContractId, pu.WeekNumber })
                .IsUnique();

            // Acceptance
            modelBuilder.Entity<Acceptance>()
                .HasOne(a => a.Contract)
                .WithMany(c => c.Acceptances)
                .HasForeignKey(a => a.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Acceptance>()
                .HasOne(a => a.Milestone)
                .WithMany(m => m.Acceptances)
                .HasForeignKey(a => a.MilestoneId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Acceptance>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification -> User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // AuditLog -> User (optional)
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            modelBuilder.Entity<AuditLog>().HasIndex(al => al.Timestamp);
            modelBuilder.Entity<AuditLog>().HasIndex(al => al.EntityType);
            modelBuilder.Entity<Notification>().HasIndex(n => n.UserId);
            modelBuilder.Entity<Notification>().HasIndex(n => n.IsRead);
            modelBuilder.Entity<BidPackage>().HasIndex(bp => bp.Status);
            modelBuilder.Entity<BidPackage>().HasIndex(bp => bp.Deadline);
            modelBuilder.Entity<BidPackage>().HasIndex(bp => new { bp.Status, bp.Deadline });
            modelBuilder.Entity<BidPackage>().HasIndex(bp => bp.CreatedBy);
            modelBuilder.Entity<BidSubmission>().HasIndex(bs => new { bs.BidPackageId, bs.Status });
            modelBuilder.Entity<BidSubmission>().HasIndex(bs => bs.TotalScore);
            modelBuilder.Entity<Contract>().HasIndex(c => c.Status);
            modelBuilder.Entity<Contract>().HasIndex(c => c.EndDate);
            modelBuilder.Entity<Contract>().HasIndex(c => new { c.ContractorId, c.Status });
            modelBuilder.Entity<ContractMilestone>().HasIndex(cm => new { cm.ContractId, cm.Status });
            modelBuilder.Entity<EvaluationCriteria>().HasIndex(ec => ec.BidPackageId);
        }
    }
}
