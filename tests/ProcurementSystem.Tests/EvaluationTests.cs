using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProcurementSystem.Core.DTOs.Evaluation;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Repositories;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests;

public class EvaluationTests
{
    [Fact]
    public async Task UploadRejectsUndefinedFileTypeBeforeWritingFiles()
    {
        await using var f = await Fixture.Create();
        var bidder = new Contractor { User = f.Evaluator, CompanyName = "Upload fixture" };
        f.Db.Add(bidder);
        await f.Db.SaveChangesAsync();
        var service = new BidSubmissionService(new UnitOfWork(f.Db), null!, null!);
        var files = Enumerable.Range(0, 3).Select(i => (Microsoft.AspNetCore.Http.IFormFile)
            new Microsoft.AspNetCore.Http.FormFile(Stream.Null, 0, 1, "files", $"{i}.pdf")).ToList();
        var result = await service.SubmitBidAsync(f.Package.Id, bidder.UserId, 800m, files,
            new() { SubmissionFileType.Quotation, SubmissionFileType.Capability, (SubmissionFileType)999 });
        Assert.False(result.Success);
        Assert.Contains("không hợp lệ", result.Message);
        Assert.Empty(await f.Db.SubmissionFiles.ToListAsync());
    }

    [Fact]
    public async Task ReplacementRollsBackDeletionWhenInsertFails()
    {
        await using var f = await Fixture.Create();
        var request = f.Replacement();
        f.Failure.Enabled = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.ReplaceCriteriaAsync(f.Package.Id, request, f.Owner.Id, false));
        var persisted = await f.Db.EvaluationCriteria.AsNoTracking().ToListAsync();
        Assert.Single(persisted);
        Assert.Equal("Original", persisted[0].Name);
        Assert.Equal(f.Criterion.Id, persisted[0].Id);
    }

    [Fact]
    public async Task ReplacementIsAtomicAndReturnsNewCriteria()
    {
        await using var f = await Fixture.Create();
        var result = await f.Service.ReplaceCriteriaAsync(f.Package.Id, f.Replacement(), f.Owner.Id, false);
        Assert.True(result.Success);
        Assert.Equal("Replacement", Assert.Single(result.Data!).Name);
        Assert.Equal(100m, (await f.Db.EvaluationCriteria.ToListAsync()).Sum(c => c.Weight));
    }

    [Fact]
    public async Task ReplacementRejectsStaleSnapshotAndWrongOwner()
    {
        await using var f = await Fixture.Create();
        var request = f.Replacement();
        request.ExpectedCriteria[0].Weight = 99;
        var stale = await f.Service.ReplaceCriteriaAsync(f.Package.Id, request, f.Owner.Id, false);
        Assert.Equal(409, stale.StatusCode);
        var forbidden = await f.Service.ReplaceCriteriaAsync(f.Package.Id, f.Replacement(), -1, false);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal("Original", (await f.Db.EvaluationCriteria.AsNoTracking().SingleAsync()).Name);
    }

    [Theory]
    [InlineData(BidPackageStatus.Evaluating)]
    [InlineData(BidPackageStatus.Awarded)]
    [InlineData(BidPackageStatus.Contracted)]
    [InlineData(BidPackageStatus.Cancelled)]
    public async Task ReplacementRejectsLockedStates(BidPackageStatus status)
    {
        await using var f = await Fixture.Create();
        f.Package.Status = status;
        await f.Db.SaveChangesAsync();
        Assert.False((await f.Service.ReplaceCriteriaAsync(f.Package.Id, f.Replacement(), f.Owner.Id, true)).Success);
        Assert.Equal("Original", (await f.Db.EvaluationCriteria.AsNoTracking().SingleAsync()).Name);
    }

    [Fact]
    public async Task ReplacementRejectsInvalidTotalAndAlreadyScoredCriteria()
    {
        await using var f = await Fixture.Create();
        var request = f.Replacement();
        request.Items[0].Weight = 99;
        Assert.False((await f.Service.ReplaceCriteriaAsync(f.Package.Id, request, f.Owner.Id, false)).Success);
        await f.AddScores();
        Assert.False((await f.Service.ReplaceCriteriaAsync(f.Package.Id, f.Replacement(), f.Owner.Id, true)).Success);
    }

    [Fact]
    public async Task BreakdownNormalizesScoresAndExcludesRetiredEvaluator()
    {
        await using var f = await Fixture.Create();
        await f.AddScores();
        f.Package.Status = BidPackageStatus.Evaluating;
        await f.Db.SaveChangesAsync();
        var result = await f.Service.GetPackageRankingsAsync(f.Package.Id, f.Owner.Id, true);
        Assert.True(result.Success);
        var submission = Assert.Single(result.Data!);
        var breakdown = Assert.Single(submission.CriteriaBreakdown);
        Assert.Equal(30m, breakdown.AverageScore); // 40 and 20; retired evaluator's 50 is excluded.
        Assert.Equal(60m, breakdown.WeightedScore);
        Assert.Equal(80m, submission.EvaluatorSummaries.Single(e => e.EvaluatorId == f.Evaluator.Id).ManualWeightedScore);
        Assert.DoesNotContain(submission.Scores, s => s.EvaluatorId == f.Retired.Id);
    }

    [Fact]
    public async Task EvaluatorCannotReadAnotherEvaluatorsScoresOrAggregates()
    {
        await using var f = await Fixture.Create();
        await f.AddScores();
        f.Package.Status = BidPackageStatus.Evaluating;
        await f.Db.SaveChangesAsync();
        var result = await f.Service.GetPackageRankingsAsync(f.Package.Id, f.Evaluator.Id, false);
        var submission = Assert.Single(result.Data!);
        Assert.Null(submission.TotalScore);
        Assert.Null(submission.Rank);
        Assert.Equal(f.Evaluator.Id, Assert.Single(submission.Scores).EvaluatorId);
        Assert.Equal(40m, Assert.Single(submission.CriteriaBreakdown).AverageScore);
        Assert.Equal(f.Evaluator.Id, Assert.Single(submission.EvaluatorSummaries).EvaluatorId);
        Assert.False((await f.Service.GetPackageRankingsAsync(f.Package.Id, f.Retired.Id, false)).Success);
    }

    [Fact]
    public async Task PriceBreakdownIsComputedOnBackendAndHiddenDuringIndependentScoring()
    {
        await using var f = await Fixture.Create();
        await f.AddScores();
        f.Package.Status = BidPackageStatus.Evaluating;
        f.Criterion.Weight = 70;
        var price = new EvaluationCriteria { BidPackage = f.Package, Name = "Price", MaxScore = 100, Weight = 30, IsPriceCriteria = true };
        var bidder = new User { Email = "lower@test.invalid", FullName = "Lower bidder", PasswordHash = "unused" };
        f.Db.AddRange(price, new BidSubmission { BidPackage = f.Package, BidPrice = 600,
            Contractor = new Contractor { User = bidder, CompanyName = "Lower price" } });
        await f.Db.SaveChangesAsync();
        var admin = await f.Service.GetPackageRankingsAsync(f.Package.Id, f.Owner.Id, true);
        var original = admin.Data!.Single(s => s.BidPrice == 800);
        Assert.Equal(75m, original.CriteriaBreakdown.Single(c => c.CriteriaId == price.Id).AverageScore);
        Assert.Equal(22.5m, original.CriteriaBreakdown.Single(c => c.CriteriaId == price.Id).WeightedScore);
        var own = await f.Service.GetPackageRankingsAsync(f.Package.Id, f.Evaluator.Id, false);
        Assert.All(own.Data!, s => Assert.Null(s.CriteriaBreakdown.Single(c => c.CriteriaId == price.Id).AverageScore));
    }

    [Fact]
    public async Task OpenPackageDoesNotExposeBreakdown()
    {
        await using var f = await Fixture.Create();
        await f.AddScores();
        Assert.False((await f.Service.GetPackageRankingsAsync(f.Package.Id, f.Owner.Id, true)).Success);
    }

    [Fact]
    public async Task FinalizeReadinessComesFromBackendAndHonorsActorAndCompletedScores()
    {
        await using var f = await Fixture.Create();
        await f.AddScores();
        f.Package.Status = BidPackageStatus.Evaluating;
        var submission = await f.Db.BidSubmissions.SingleAsync();
        submission.Status = BidSubmissionStatus.Evaluated;
        await f.Db.SaveChangesAsync();
        var owner = await f.Service.GetEvaluationSummaryAsync(f.Package.Id, f.Owner.Id, true);
        Assert.True(owner.Success);
        Assert.True(owner.Data!.CanFinalize);
        Assert.Contains(submission.Id, owner.Data.EligibleSubmissionIds);
        var unrelated = await f.Service.GetEvaluationSummaryAsync(f.Package.Id, f.Other.Id, true);
        Assert.False(unrelated.Data!.CanFinalize);
        Assert.Empty(unrelated.Data.EligibleSubmissionIds);
        var evaluator = await f.Service.GetEvaluationSummaryAsync(f.Package.Id, f.Evaluator.Id, false);
        Assert.False(evaluator.Data!.CanFinalize);
        Assert.Null(evaluator.Data.HighestScore);
    }

    [Fact]
    public async Task PackageReadinessRequiresCommitteeAndFullCriteriaWeights()
    {
        await using var f = await Fixture.Create();
        f.Package.Status = BidPackageStatus.Closed;
        await f.Db.SaveChangesAsync();
        var service = new BidPackageService(new UnitOfWork(f.Db), null!, f.Service, null!);
        var before = await service.GetBidPackageByIdAsync(f.Package.Id);
        Assert.False(before.Data!.CanStartEvaluation);
        Assert.NotEmpty(before.Data.EvaluationBlockingReasons);
        foreach (var evaluator in new[] { f.Evaluator, f.Other, f.Retired })
            f.Db.Add(new BidPackageEvaluator { BidPackage = f.Package, Evaluator = evaluator });
        await f.Db.SaveChangesAsync();
        var ready = await service.GetBidPackageByIdAsync(f.Package.Id);
        Assert.True(ready.Data!.IsCommitteeValid);
        Assert.True(ready.Data.CanStartEvaluation);
        f.Criterion.Weight = 99m;
        await f.Db.SaveChangesAsync();
        Assert.False((await service.GetBidPackageByIdAsync(f.Package.Id)).Data!.CanStartEvaluation);
    }
}

internal sealed class InsertFailure : SaveChangesInterceptor
{
    public bool Enabled { get; set; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Enabled && eventData.Context!.ChangeTracker.Entries<EvaluationCriteria>().Any(e => e.State == EntityState.Added))
            throw new InvalidOperationException("Injected insert failure after old criteria were deleted");
        return ValueTask.FromResult(result);
    }
}

internal sealed class Fixture : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    public AppDbContext Db { get; }
    public InsertFailure Failure { get; }
    public EvaluationService Service { get; }
    public User Owner { get; } = new() { FullName = "Owner", Email = "owner@test.invalid", PasswordHash = "unused" };
    public User Evaluator { get; } = new() { FullName = "Evaluator A", Email = "a@test.invalid", PasswordHash = "unused" };
    public User Other { get; } = new() { FullName = "Evaluator B", Email = "b@test.invalid", PasswordHash = "unused" };
    public User Retired { get; } = new() { FullName = "Retired", Email = "retired@test.invalid", PasswordHash = "unused" };
    public BidPackage Package { get; }
    public EvaluationCriteria Criterion { get; }

    private Fixture(SqliteConnection connection, AppDbContext db, InsertFailure failure)
    {
        this.connection = connection; Db = db; Failure = failure;
        Service = new(new UnitOfWork(db), null!, null!);
        Package = new() { Code = "FIXTURE", Name = "Fixture", Budget = 1000, Creator = Owner, Deadline = DateTime.UtcNow.AddDays(1) };
        Criterion = new() { BidPackage = Package, Name = "Original", MaxScore = 50, Weight = 100 };
    }

    public static async Task<Fixture> Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var failure = new InsertFailure();
        var db = new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).AddInterceptors(failure).Options);
        await db.Database.EnsureCreatedAsync();
        var f = new Fixture(connection, db, failure);
        db.AddRange(f.Owner, f.Evaluator, f.Other, f.Retired, f.Package, f.Criterion);
        await db.SaveChangesAsync();
        return f;
    }

    public ReplaceCriteriaRequest Replacement() => new()
    {
        Items = new() { new() { Name = "Replacement", MaxScore = 100, Weight = 100 } },
        ExpectedCriteria = new() { new() { Id = Criterion.Id, Name = Criterion.Name, MaxScore = Criterion.MaxScore, Weight = Criterion.Weight } }
    };

    public async Task AddScores()
    {
        var bidder = new User { Email = "bidder@test.invalid", FullName = "Bidder", PasswordHash = "unused" };
        var contractor = new Contractor { User = bidder, CompanyName = "Fixture company" };
        var submission = new BidSubmission { BidPackage = Package, Contractor = contractor, BidPrice = 800, TotalScore = 60, Rank = 1 };
        Db.AddRange(submission, new BidPackageEvaluator { BidPackage = Package, Evaluator = Evaluator },
            new BidPackageEvaluator { BidPackage = Package, Evaluator = Other });
        foreach (var pair in new[] { (Evaluator, 40m), (Other, 20m), (Retired, 50m) })
            Db.Add(new EvaluationScore { BidSubmission = submission, Criteria = Criterion, Evaluator = pair.Item1, Score = pair.Item2 });
        await Db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
}

internal sealed class TestDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // SQLite cannot order decimal SQL types. Production SQL Server keeps decimal.
        foreach (var property in builder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                property.SetValueConverter(new ValueConverter<decimal, double>(v => (double)v, v => (decimal)v));
    }
}
