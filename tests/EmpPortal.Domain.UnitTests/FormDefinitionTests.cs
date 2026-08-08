using EmpPortal.Domain.Forms;

namespace EmpPortal.Domain.UnitTests;

public sealed class FormDefinitionTests
{
    private static readonly Guid ActorId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PublishedFormHonorsScheduleAndManualPause()
    {
        FormDefinition form = FormDefinition.Create("leave-request", "درخواست مرخصی", null, ActorId, Now);
        form.ConfigureSchedule(Now.AddHours(1), Now.AddHours(3), ActorId, Now);
        form.Publish(Guid.NewGuid(), ActorId, Now);

        Assert.False(form.IsAvailableAt(Now));
        Assert.True(form.IsAvailableAt(Now.AddHours(2)));
        Assert.False(form.IsAvailableAt(Now.AddHours(3)));

        form.Pause(ActorId, Now.AddHours(2));
        Assert.False(form.IsAvailableAt(Now.AddHours(2)));

        form.Resume(ActorId, Now.AddHours(2).AddMinutes(1));
        Assert.True(form.IsAvailableAt(Now.AddHours(2).AddMinutes(1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("فرم")]
    [InlineData("contains/slash")]
    [InlineData("1-starts-with-number")]
    [InlineData("contains_space")]
    public void InvalidSlugIsRejected(string slug)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            FormDefinition.Create(slug, "فرم", null, ActorId, Now));
    }

    [Fact]
    public void ArchivedFormCannotBeEdited()
    {
        FormDefinition form = FormDefinition.Create("survey", "نظرسنجی", null, ActorId, Now);
        form.Archive(ActorId, Now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            form.UpdateDetails("عنوان جدید", null, ActorId, Now.AddMinutes(2)));
    }
}
