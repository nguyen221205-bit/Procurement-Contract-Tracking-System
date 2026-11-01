using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcurementSystem.Core.DTOs.BidPackage;
using ProcurementSystem.Core.DTOs.Contract;
using ProcurementSystem.Core.Enums;
using ProcurementSystem.Core.Helpers;
using ProcurementSystem.Infrastructure.Entities;
using ProcurementSystem.Infrastructure.Repositories;
using ProcurementSystem.Infrastructure.Services;
using Xunit;

namespace ProcurementSystem.Tests;

public class ContractAndEnumTests
{
    [Fact]
    public async Task SummaryUsesWholeFilteredQueryBeforePaginationAndPreservesOwnerScope()
    {
        await using var f = await Fixture.Create();
        var contractor = new Contractor { CompanyName = "First company", User = f.Evaluator };
        var otherContractor = new Contractor { CompanyName = "Other company", User = f.Other };
        for (var i = 1; i <= 3; i++)
        {
            var package = new BidPackage { Code = $"CONTRACT-{i}", Name = "Test", Creator = f.Owner, Status = BidPackageStatus.Awarded };
            var contract = new Contract { BidPackage = package, Contractor = i == 3 ? otherContractor : contractor,
                ContractNumber = $"TEST-{i}", Value = i * 100m, Status = i == 2 ? ContractStatus.Draft : ContractStatus.Active };
            contract.Milestones.Add(new() { Title = "Payment", Amount = i * 10m, Status = MilestoneStatus.Completed });
            f.Db.Add(contract);
        }
        await f.Db.SaveChangesAsync();
        var service = new ContractService(new UnitOfWork(f.Db), null!, null!, null!);
        var page1 = await service.GetContractsAsync(new() { PageSize = 1, PageIndex = 1 }, f.Owner.Id, true);
        var page2 = await service.GetContractsAsync(new() { PageSize = 1, PageIndex = 2 }, f.Owner.Id, true);
        Assert.True(page1.Success);
        Assert.Single(page1.Data!.Items);
        Assert.Equal(600m, page1.Data.Summary.TotalValue);
        Assert.Equal(60m, page1.Data.Summary.TotalDisbursedAmount);
        Assert.Equal(page1.Data.Summary.TotalValue, page2.Data!.Summary.TotalValue);
        var filtered = await service.GetContractsAsync(new() { Status = ContractStatus.Active }, f.Owner.Id, true);
        Assert.Equal(400m, filtered.Data!.Summary.TotalValue);
        var scoped = await service.GetContractsAsync(new(), f.Evaluator.Id, false);
        Assert.Equal(300m, scoped.Data!.Summary.TotalValue);
        Assert.Equal(2, scoped.Data.Summary.TotalContracts);
    }

    [Fact]
    public void HalfDongRemainingIsNotFullyAllocated()
    {
        var dto = new ContractDto { Value = 100.5m, Status = ContractStatus.Draft,
            Milestones = new() { new() { Amount = 100m } } };
        Assert.False(dto.IsFullyAllocated);
        Assert.True(dto.CanAddMilestone);
        Assert.Equal(.5m, dto.UnallocatedAmount);
    }

    [Theory]
    [InlineData(40, 50, 100, 80)]
    [InlineData(80, 100, 30, 24)]
    [InlineData(800, 1000, 100, 80)]
    public void ScoringNormalizesDifferentScales(decimal score, decimal max, decimal weight, decimal expected) =>
        Assert.Equal(expected, EvaluationMath.Weighted(score, max, weight));

    [Fact]
    public void NumericZeroAndLegacyNamesAreAcceptedButUnknownEnumFailsValidation()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        foreach (var json in new[] { "{\"newStatus\":0}", "{\"newStatus\":\"Open\"}" })
        {
            var request = JsonSerializer.Deserialize<ChangeBidPackageStatusRequest>(json, options)!;
            Assert.Equal(BidPackageStatus.Open, request.NewStatus);
            Assert.True(Validator.TryValidateObject(request, new(request), new List<ValidationResult>(), true));
        }
        var invalid = JsonSerializer.Deserialize<ChangeBidPackageStatusRequest>("{\"newStatus\":999}", options)!;
        Assert.False(Validator.TryValidateObject(invalid, new(invalid), new List<ValidationResult>(), true));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ChangeBidPackageStatusRequest>("{}", options));
    }
}
