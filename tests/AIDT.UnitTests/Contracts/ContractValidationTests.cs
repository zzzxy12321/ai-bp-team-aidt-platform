using System.Text.Json;
using AIDT.Application.Contracts;
using AIDT.Application.Validation;
using AIDT.Domain.Common;
using AIDT.Domain.Workflows;
using Shouldly;

namespace AIDT.UnitTests.Contracts;

public sealed class ContractValidationTests
{
    private static readonly JsonSerializerOptions WebJsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public void ErrorCodes_AreNonEmptyUniqueAndUppercase()
    {
        ErrorCodes.All.ShouldNotBeEmpty();
        ErrorCodes.All.ShouldAllBe(code => code.All(character => !char.IsLower(character)));
        ErrorCodes.All.Count.ShouldBe(ErrorCodes.All.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CreateBusinessObjectRequest_UsesCamelCaseContract()
    {
        var request = new CreateBusinessObjectRequest(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            "LOT-2026-001",
            new Dictionary<string, string?> { ["Customer"] = "A" });

        var json = JsonSerializer.Serialize(request, WebJsonOptions);

        json.ShouldContain("\"objectTypeId\"");
        json.ShouldContain("\"objectCode\"");
        json.ShouldContain("\"fields\"");
    }

    [Fact]
    public void TaskDecisionReturn_WithoutReason_IsRejectedBeforeUseCaseWork()
    {
        var request = new TaskDecisionRequest(
            WorkflowDecision.Return,
            1,
            " ",
            "idempotency-001");

        Should.Throw<ValidationException>(() => RequestValidation.Validate(request))
            .ErrorCode.ShouldBe(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public void InvalidObjectCode_IsRejected()
    {
        var request = new CreateBusinessObjectRequest(
            Guid.NewGuid(),
            "lot-lowercase",
            new Dictionary<string, string?>());

        Should.Throw<ValidationException>(() => RequestValidation.Validate(request))
            .ErrorCode.ShouldBe(ErrorCodes.ValidationFailed);
    }
}
