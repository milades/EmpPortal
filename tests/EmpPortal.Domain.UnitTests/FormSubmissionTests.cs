using EmpPortal.Domain.Forms;

namespace EmpPortal.Domain.UnitTests;

public sealed class FormSubmissionTests
{
    [Fact]
    public void SubmittedResponseCannotBeEditedWhenPolicyDisallowsIt()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FormSubmission submission = FormSubmission.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "{}",
            "F-20260804-000001",
            now);
        submission.Submit("{\"name\":\"کاربر\"}", now.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() =>
            submission.Save("{\"name\":\"ویرایش\"}", false, now.AddMinutes(2)));
    }
}
