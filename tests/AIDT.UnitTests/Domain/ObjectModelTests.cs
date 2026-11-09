using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class ObjectModelTests
{
    [Theory]
    [InlineData("string", ObjectFieldTypes.Text)]
    [InlineData("text", ObjectFieldTypes.LongText)]
    [InlineData("Text", ObjectFieldTypes.Text)]
    [InlineData("LongText", ObjectFieldTypes.LongText)]
    [InlineData("decimal", ObjectFieldTypes.Number)]
    [InlineData("Number", ObjectFieldTypes.Number)]
    [InlineData("Date", ObjectFieldTypes.Date)]
    [InlineData("DateTime", ObjectFieldTypes.DateTime)]
    [InlineData("Boolean", ObjectFieldTypes.Boolean)]
    [InlineData("Select", ObjectFieldTypes.Select)]
    [InlineData("User", ObjectFieldTypes.User)]
    [InlineData("Department", ObjectFieldTypes.Department)]
    public void FieldTypes_NormalizeCanonicalNamesAndLegacyAliases(
        string source,
        string expected)
    {
        ObjectFieldTypes.Normalize(source).ShouldBe(expected);
    }

    [Fact]
    public void Draft_Publish_Retire_Restore_FollowLifecycle()
    {
        var model = ObjectType.CreateDraft(
            "TEST_MODEL",
            "测试模型",
            [
                new ObjectFieldDefinition(
                    "Status",
                    "状态",
                    ObjectFieldTypes.Select,
                    true,
                    20,
                    FieldSensitivity.Internal,
                    Options: ["Open", "Closed"],
                    ShowInList: true,
                    Filterable: true,
                    SortOrder: 10)
            ]);

        model.ValidateModel(1);
        model.Publish(2, DateTimeOffset.UtcNow);
        model.Retire(3, DateTimeOffset.UtcNow);
        model.Restore(4, DateTimeOffset.UtcNow);

        model.Status.ShouldBe(ObjectTypeStatus.Published);
        model.Revision.ShouldBe(5);
        model.RetiredAt.ShouldBeNull();
    }

    [Fact]
    public void UpdateDraft_IncrementsSchemaVersion_AndKeepsRepositoryCodeStable()
    {
        var model = ObjectType.CreateDraft(
            "TEST_MODEL",
            "测试模型",
            [new ObjectFieldDefinition("Name", "名称", "string", true, 100, FieldSensitivity.Internal)]);

        model.UpdateDraft(
            "测试模型 V2",
            [
                new ObjectFieldDefinition("Name", "名称", "string", true, 120, FieldSensitivity.Internal),
                new ObjectFieldDefinition("Memo", "说明", "text", false, 2000, FieldSensitivity.Sensitive)
            ],
            1);

        model.Code.ShouldBe("TEST_MODEL");
        model.Version.ShouldBe(2);
        model.Revision.ShouldBe(2);
        model.Status.ShouldBe(ObjectTypeStatus.Draft);
        model.Fields.Select(field => field.NormalizedDataType)
            .ShouldBe([ObjectFieldTypes.Text, ObjectFieldTypes.LongText], ignoreOrder: true);
    }

    [Fact]
    public void DuplicateFieldCode_IsRejected()
    {
        var exception = Should.Throw<ValidationException>(() =>
            ObjectType.CreateDraft(
                "TEST_MODEL",
                "测试模型",
                [
                    new ObjectFieldDefinition("Name", "名称", "Text", true, 100, FieldSensitivity.Internal),
                    new ObjectFieldDefinition("name", "重复名称", "Text", false, 100, FieldSensitivity.Internal)
                ]));

        exception.Message.ShouldContain("不能重复");
    }

    [Theory]
    [InlineData("Number", "12.5", true)]
    [InlineData("Number", "abc", false)]
    [InlineData("Date", "2026-11-02", true)]
    [InlineData("Date", "2026/11/02", false)]
    [InlineData("Boolean", "true", true)]
    [InlineData("Boolean", "yes", false)]
    [InlineData("User", "10000000-0000-0000-0000-000000000010", true)]
    public void FieldValueValidator_EnforcesSupportedTypes(
        string dataType,
        string value,
        bool valid)
    {
        var field = new ObjectFieldDefinition(
            "Value",
            "值",
            dataType,
            false,
            100,
            FieldSensitivity.Internal,
            Options: dataType == ObjectFieldTypes.Select ? ["A"] : null);

        if (valid)
        {
            Should.NotThrow(() => ObjectFieldValueValidator.Validate(field, value));
        }
        else
        {
            Should.Throw<ValidationException>(
                () => ObjectFieldValueValidator.Validate(field, value));
        }
    }

    [Fact]
    public void Select_RequiresOptions_AndRejectsUnlistedValue()
    {
        Should.Throw<ValidationException>(() =>
            new ObjectFieldDefinition(
                "Choice",
                "选择",
                ObjectFieldTypes.Select,
                false,
                20,
                FieldSensitivity.Internal).Validate());

        var field = new ObjectFieldDefinition(
            "Choice",
            "选择",
            ObjectFieldTypes.Select,
            false,
            20,
            FieldSensitivity.Internal,
            Options: ["A", "B"]);
        Should.Throw<ValidationException>(
            () => ObjectFieldValueValidator.Validate(field, "C"));
    }

    [Fact]
    public void LongText_AllowsValuesBeyondLegacyShortTextLimit()
    {
        var field = new ObjectFieldDefinition(
            "Detail",
            "详细说明",
            ObjectFieldTypes.LongText,
            false,
            10_000,
            FieldSensitivity.Internal);
        var value = new string('A', 5_000);

        Should.NotThrow(() => ObjectFieldValueValidator.Validate(field, value));
        var businessObject = BusinessObject.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LONG-TEXT-001",
            new Dictionary<string, string?> { ["Detail"] = value },
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "test");
        businessObject.Fields["Detail"]!.Length.ShouldBe(5_000);
    }
}
