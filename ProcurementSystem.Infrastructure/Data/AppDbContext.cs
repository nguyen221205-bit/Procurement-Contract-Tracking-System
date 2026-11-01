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
        public DbSet<BidPackageEvaluator> BidPackageEvaluators => Set<BidPackageEvaluator>();
        public DbSet<Contract> Contracts => Set<Contract>();
        public DbSet<ContractMilestone> ContractMilestones => Set<ContractMilestone>();
        public DbSet<ProgressUpdate> ProgressUpdates => Set<ProgressUpdate>();
        public DbSet<Acceptance> Acceptances => Set<Acceptance>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<EvaluatorProposal> EvaluatorProposals => Set<EvaluatorProposal>();
        public DbSet<ProcuringEntity> ProcuringEntities => Set<ProcuringEntity>();
        public DbSet<ContractGuarantee> ContractGuarantees => Set<ContractGuarantee>();
        public DbSet<ContractAddendum> ContractAddenda => Set<ContractAddendum>();
        public DbSet<ContractorEvaluation> ContractorEvaluations => Set<ContractorEvaluation>();
        public DbSet<PackageStatusApprovalRequest> PackageStatusApprovalRequests => Set<PackageStatusApprovalRequest>();
        public DbSet<BidClarification> BidClarifications => Set<BidClarification>();
        public DbSet<ProcuringEntityMember> ProcuringEntityMembers => Set<ProcuringEntityMember>();
        public DbSet<ProcuringEntityInvitation> ProcuringEntityInvitations => Set<ProcuringEntityInvitation>();
        public DbSet<SigningAuthority> SigningAuthorities => Set<SigningAuthority>();

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

            modelBuilder.Entity<BidPackage>()
                .HasOne(bp => bp.ProcuringEntity)
                .WithMany()
                .HasForeignKey(bp => bp.ProcuringEntityId)
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

            modelBuilder.Entity<EvaluationCriteria>()
                .Property(ec => ec.IsPriceCriteria)
                .HasDefaultValue(false);

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

            // BidPackageEvaluator - composite key (N-N)
            modelBuilder.Entity<BidPackageEvaluator>()
                .HasKey(pe => new { pe.BidPackageId, pe.EvaluatorId });

            modelBuilder.Entity<BidPackageEvaluator>()
                .HasOne(pe => pe.BidPackage)
                .WithMany(bp => bp.PackageEvaluators)
                .HasForeignKey(pe => pe.BidPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BidPackageEvaluator>()
                .HasOne(pe => pe.Evaluator)
                .WithMany()
                .HasForeignKey(pe => pe.EvaluatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BidPackageEvaluator>()
                .HasOne(pe => pe.Assigner)
                .WithMany()
                .HasForeignKey(pe => pe.AssignedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Contract
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.BidPackage)
                .WithMany(bp => bp.Contracts)
                .HasForeignKey(c => c.BidPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mỗi gói thầu chỉ có tối đa 1 hợp đồng chưa bị chấm dứt (Terminated = 3) tại một thời điểm
            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.BidPackageId)
                .IsUnique()
                .HasFilter("[Status] <> 3");

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
                .HasIndex(a => new { a.MilestoneId, a.Revision })
                .IsUnique();

            modelBuilder.Entity<Acceptance>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                modelBuilder.Entity<Acceptance>()
                    .Property(a => a.RowVersion)
                    .HasDefaultValueSql("randomblob(8)");

                modelBuilder.Entity<ContractMilestone>()
                    .Property(m => m.RowVersion)
                    .HasDefaultValueSql("randomblob(8)");

                modelBuilder.Entity<SigningAuthority>()
                    .Property(sa => sa.RowVersion)
                    .HasDefaultValueSql("randomblob(8)");

                modelBuilder.Entity<ProcuringEntityMember>()
                    .Property(pem => pem.RowVersion)
                    .HasDefaultValueSql("randomblob(8)");
            }
            else
            {
                modelBuilder.Entity<Acceptance>()
                    .Property(a => a.RowVersion)
                    .IsRowVersion();

                modelBuilder.Entity<ContractMilestone>()
                    .Property(m => m.RowVersion)
                    .IsRowVersion();

                modelBuilder.Entity<SigningAuthority>()
                    .Property(sa => sa.RowVersion)
                    .IsRowVersion();

                modelBuilder.Entity<ProcuringEntityMember>()
                    .Property(pem => pem.RowVersion)
                    .IsRowVersion();
            }

            modelBuilder.Entity<Acceptance>()
                .HasOne(a => a.TechnicalReviewer)
                .WithMany()
                .HasForeignKey(a => a.TechnicalReviewedBy)
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
            modelBuilder.Entity<BidPackage>().HasIndex(bp => bp.ProcuringEntityId);
            modelBuilder.Entity<BidSubmission>().HasIndex(bs => new { bs.BidPackageId, bs.Status });
            modelBuilder.Entity<BidSubmission>().HasIndex(bs => bs.TotalScore);
            modelBuilder.Entity<Contract>().HasIndex(c => c.Status);
            modelBuilder.Entity<Contract>().HasIndex(c => c.EndDate);
            modelBuilder.Entity<Contract>().HasIndex(c => new { c.ContractorId, c.Status });
            modelBuilder.Entity<ContractMilestone>().HasIndex(cm => new { cm.ContractId, cm.Status });
            modelBuilder.Entity<EvaluationCriteria>().HasIndex(ec => ec.BidPackageId);

            // EvaluatorProposal relationships & indexes
            modelBuilder.Entity<EvaluatorProposal>()
                .HasOne(ep => ep.ProposerUser)
                .WithMany()
                .HasForeignKey(ep => ep.ProposerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluatorProposal>()
                .HasOne(ep => ep.ReviewedByUser)
                .WithMany()
                .HasForeignKey(ep => ep.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EvaluatorProposal>()
                .HasOne(ep => ep.CreatedUser)
                .WithMany()
                .HasForeignKey(ep => ep.CreatedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EvaluatorProposal>().HasIndex(ep => ep.Status);
            modelBuilder.Entity<EvaluatorProposal>().HasIndex(ep => ep.Email);
            modelBuilder.Entity<EvaluatorProposal>().HasIndex(ep => ep.ProposerUserId);

            // ProcuringEntity relationships & indexes
            modelBuilder.Entity<ProcuringEntity>()
                .HasOne(pe => pe.User)
                .WithOne(u => u.ProcuringEntity)
                .HasForeignKey<ProcuringEntity>(pe => pe.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcuringEntity>()
                .HasOne(pe => pe.ReviewedByUser)
                .WithMany()
                .HasForeignKey(pe => pe.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcuringEntity>().HasIndex(pe => pe.TaxCode);
            modelBuilder.Entity<ProcuringEntity>().HasIndex(pe => pe.VerificationStatus);
            modelBuilder.Entity<ProcuringEntity>().HasIndex(pe => pe.UserId).IsUnique();

            // ProcuringEntityMember relationships & indexes
            modelBuilder.Entity<ProcuringEntityMember>()
                .HasOne(pem => pem.ProcuringEntity)
                .WithMany(pe => pe.Members)
                .HasForeignKey(pem => pem.ProcuringEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcuringEntityMember>()
                .HasOne(pem => pem.User)
                .WithMany(u => u.EntityMemberships)
                .HasForeignKey(pem => pem.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcuringEntityMember>()
                .HasOne(pem => pem.InvitedByUser)
                .WithMany()
                .HasForeignKey(pem => pem.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcuringEntityMember>()
                .HasOne(pem => pem.RevokedByUser)
                .WithMany()
                .HasForeignKey(pem => pem.RevokedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcuringEntityMember>()
                .HasIndex(pem => new { pem.ProcuringEntityId, pem.UserId })
                .IsUnique();
            modelBuilder.Entity<ProcuringEntityMember>().HasIndex(pem => pem.Status);
            modelBuilder.Entity<ProcuringEntityMember>().HasIndex(pem => pem.RoleInEntity);

            // ProcuringEntityInvitation relationships & indexes
            modelBuilder.Entity<ProcuringEntityInvitation>()
                .HasOne(pei => pei.ProcuringEntity)
                .WithMany(pe => pe.Invitations)
                .HasForeignKey(pei => pei.ProcuringEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcuringEntityInvitation>()
                .HasOne(pei => pei.InvitedByUser)
                .WithMany()
                .HasForeignKey(pei => pei.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcuringEntityInvitation>()
                .HasOne(pei => pei.AcceptedByUser)
                .WithMany()
                .HasForeignKey(pei => pei.AcceptedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProcuringEntityInvitation>()
                .HasIndex(pei => pei.TokenHash)
                .IsUnique();
            modelBuilder.Entity<ProcuringEntityInvitation>().HasIndex(pei => pei.Email);
            modelBuilder.Entity<ProcuringEntityInvitation>().HasIndex(pei => pei.Status);
            modelBuilder.Entity<ProcuringEntityInvitation>().HasIndex(pei => pei.ExpiresAt);

            // ContractGuarantee relationships & indexes
            modelBuilder.Entity<ContractGuarantee>()
                .HasOne(cg => cg.Contract)
                .WithMany(c => c.Guarantees)
                .HasForeignKey(cg => cg.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContractGuarantee>().HasIndex(cg => cg.GuaranteeNumber);
            modelBuilder.Entity<ContractGuarantee>().HasIndex(cg => cg.ExpiryDate);
            modelBuilder.Entity<ContractGuarantee>().HasIndex(cg => cg.Status);

            // ContractAddendum relationships & indexes
            modelBuilder.Entity<ContractAddendum>()
                .HasOne(ca => ca.Contract)
                .WithMany(c => c.Addenda)
                .HasForeignKey(ca => ca.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContractAddendum>()
                .HasOne(ca => ca.Creator)
                .WithMany()
                .HasForeignKey(ca => ca.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractAddendum>()
                .HasOne(ca => ca.Approver)
                .WithMany()
                .HasForeignKey(ca => ca.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => ca.AddendumNumber);
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => ca.ContractId);

            // ContractorEvaluation relationships & indexes
            modelBuilder.Entity<ContractorEvaluation>()
                .HasOne(ce => ce.Contract)
                .WithMany(c => c.Evaluations)
                .HasForeignKey(ce => ce.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContractorEvaluation>()
                .HasOne(ce => ce.Contractor)
                .WithMany(c => c.Evaluations)
                .HasForeignKey(ce => ce.ContractorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractorEvaluation>()
                .HasOne(ce => ce.EvaluatorUser)
                .WithMany()
                .HasForeignKey(ce => ce.EvaluatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractorEvaluation>().HasIndex(ce => ce.ContractorId);
            modelBuilder.Entity<ContractorEvaluation>().HasIndex(ce => new { ce.ContractId, ce.ContractorId });
            // PackageStatusApprovalRequest relationships & indexes
            modelBuilder.Entity<PackageStatusApprovalRequest>()
                .HasOne(r => r.BidPackage)
                .WithMany()
                .HasForeignKey(r => r.BidPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PackageStatusApprovalRequest>()
                .HasOne(r => r.RequestedByUser)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PackageStatusApprovalRequest>()
                .HasOne(r => r.ReviewedByUser)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PackageStatusApprovalRequest>().HasIndex(r => r.Status);
            modelBuilder.Entity<PackageStatusApprovalRequest>().HasIndex(r => r.BidPackageId);

            // BidClarification relationships & indexes
            modelBuilder.Entity<BidClarification>()
                .HasOne(c => c.BidPackage)
                .WithMany()
                .HasForeignKey(c => c.BidPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BidClarification>()
                .HasOne(c => c.BidSubmission)
                .WithMany()
                .HasForeignKey(c => c.BidSubmissionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BidClarification>()
                .HasOne(c => c.AskedByUser)
                .WithMany()
                .HasForeignKey(c => c.AskedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BidClarification>()
                .HasOne(c => c.AnsweredByUser)
                .WithMany()
                .HasForeignKey(c => c.AnsweredByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BidClarification>().HasIndex(c => c.BidPackageId);
            modelBuilder.Entity<BidClarification>().HasIndex(c => c.BidSubmissionId);
            modelBuilder.Entity<BidClarification>().HasIndex(c => c.Type);

            // SigningAuthority relationships & indexes
            modelBuilder.Entity<SigningAuthority>()
                .HasOne(sa => sa.ProcuringEntity)
                .WithMany(pe => pe.SigningAuthorities)
                .HasForeignKey(sa => sa.ProcuringEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SigningAuthority>()
                .HasOne(sa => sa.User)
                .WithMany(u => u.SigningAuthorities)
                .HasForeignKey(sa => sa.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SigningAuthority>()
                .HasOne(sa => sa.RequestedByUser)
                .WithMany()
                .HasForeignKey(sa => sa.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SigningAuthority>()
                .HasOne(sa => sa.VerifiedByUser)
                .WithMany()
                .HasForeignKey(sa => sa.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SigningAuthority>()
                .HasOne(sa => sa.RevokedByUser)
                .WithMany()
                .HasForeignKey(sa => sa.RevokedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SigningAuthority>().HasIndex(sa => new { sa.ProcuringEntityId, sa.UserId, sa.Status });
            modelBuilder.Entity<SigningAuthority>().HasIndex(sa => sa.Status);
            modelBuilder.Entity<SigningAuthority>().HasIndex(sa => sa.UserId);

            // Acceptance.SigningAuthority relationship
            modelBuilder.Entity<Acceptance>()
                .HasOne(a => a.AppliedSigningAuthority)
                .WithMany()
                .HasForeignKey(a => a.SigningAuthorityId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
