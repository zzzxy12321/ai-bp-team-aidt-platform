using AIDT.Domain.Common;
using AIDT.Domain.Scenarios;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class ScenarioRulesTests
{
    [Fact]
    public void Startable_RequiresPublishedVersionAndValidReferences()
    {
        Should.Throw<ConflictException>(() =>
                ScenarioRules.EnsureStartable(
                    ScenarioLifecycleStatus.Published,
                    ScenarioVersionStatus.Draft,
                    true,
                    []))
            .ErrorCode.ShouldBe(ErrorCodes.ScenarioNotPublished);

        Should.Throw<ConflictException>(() =>
                ScenarioRules.EnsureStartable(
                    ScenarioLifecycleStatus.Published,
                    ScenarioVersionStatus.Published,
                    false,
                    ["流程版本已停用"]))
            .Message.ShouldContain("流程版本已停用");
    }

    [Fact]
    public void DisabledScene_BlocksNewLaunchButKeepsVersionRules()
    {
        Should.Throw<ConflictException>(() =>
                ScenarioRules.EnsureStartable(
                    ScenarioLifecycleStatus.Disabled,
                    ScenarioVersionStatus.Published,
                    true,
                    []))
            .ErrorCode.ShouldBe(ErrorCodes.ScenarioDisabled);
    }

    [Fact]
    public void PublishedVersion_IsImmutable()
    {
        Should.Throw<ConflictException>(() =>
                ScenarioRules.EnsureVersionEditable(
                    ScenarioVersionStatus.Published,
                    isServiceIdentity: false))
            .ErrorCode.ShouldBe(ErrorCodes.ScenarioVersionConflict);
    }

    [Fact]
    public void PermanentHumanBoundaries_AreMandatory()
    {
        var missingOne = ScenarioRules.PermanentHumanBoundaries
            .Where(item => item != "财务付款")
            .ToArray();

        Should.Throw<ValidationException>(() =>
                ScenarioRules.EnsureNoAutomaticHighRiskAction(missingOne))
            .ErrorCode.ShouldBe(ErrorCodes.ScenarioBoundaryMissing);
    }

    [Fact]
    public void ServiceIdentity_CannotGovernOrLaunchScenes()
    {
        Should.Throw<PermissionException>(() =>
                ScenarioRules.EnsureMetadataEditable(
                    ScenarioLifecycleStatus.Draft,
                    isServiceIdentity: true))
            .ErrorCode.ShouldBe(ErrorCodes.PermissionDenied);
    }
}
