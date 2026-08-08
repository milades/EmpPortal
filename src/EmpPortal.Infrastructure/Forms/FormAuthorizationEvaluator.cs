using EmpPortal.Application.Authorization;
using EmpPortal.Application.Forms;
using EmpPortal.Domain.Forms;
using EmpPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmpPortal.Infrastructure.Forms;

internal static class FormAuthorizationEvaluator
{
    private const FormAccessRights AllRights =
        FormAccessRights.View |
        FormAccessRights.Submit |
        FormAccessRights.Manage |
        FormAccessRights.ViewSubmissions |
        FormAccessRights.Export;

    public static bool CanCreate(FormActor actor) =>
        IsInRole(actor, PortalRoles.SystemAdministrator) ||
        IsInRole(actor, PortalRoles.FormAdministrator) ||
        IsInRole(actor, PortalRoles.FormDesigner);

    public static bool CanPublish(FormActor actor) =>
        IsInRole(actor, PortalRoles.SystemAdministrator) ||
        IsInRole(actor, PortalRoles.FormAdministrator) ||
        IsInRole(actor, PortalRoles.FormPublisher);

    public static bool CanViewReports(FormActor actor) =>
        IsInRole(actor, PortalRoles.SystemAdministrator) ||
        IsInRole(actor, PortalRoles.FormAdministrator) ||
        IsInRole(actor, PortalRoles.SubmissionViewer) ||
        IsInRole(actor, PortalRoles.ReportExporter);

    public static bool CanExportReports(FormActor actor) =>
        IsInRole(actor, PortalRoles.SystemAdministrator) ||
        IsInRole(actor, PortalRoles.FormAdministrator) ||
        IsInRole(actor, PortalRoles.ReportExporter);

    public static bool IsGlobalAdministrator(FormActor actor) =>
        IsInRole(actor, PortalRoles.SystemAdministrator) ||
        IsInRole(actor, PortalRoles.FormAdministrator);

    public static async Task<bool> HasRightsAsync(
        PortalDbContext dbContext,
        FormDefinition form,
        FormActor actor,
        FormAccessRights requiredRights,
        CancellationToken cancellationToken)
    {
        if (IsGlobalAdministrator(actor) || form.CreatedByUserId == actor.UserId)
        {
            return (AllRights & requiredRights) == requiredRights;
        }

        if (((requiredRights & FormAccessRights.ViewSubmissions) != 0 && CanViewReports(actor)) ||
            ((requiredRights & FormAccessRights.Export) != 0 && CanExportReports(actor)))
        {
            return true;
        }

        string userKey = actor.UserId.ToString("D");
        string[] roleKeys = actor.Roles.ToArray();
        FormAccessRights[] grantedRights = await dbContext.FormAccessRules
            .AsNoTracking()
            .Where(rule => rule.FormId == form.Id &&
                (rule.SubjectType == FormAccessSubjectType.Everyone ||
                 rule.SubjectType == FormAccessSubjectType.User && rule.SubjectKey == userKey ||
                 rule.SubjectType == FormAccessSubjectType.Role && roleKeys.Contains(rule.SubjectKey)))
            .Select(rule => rule.Rights)
            .ToArrayAsync(cancellationToken);

        FormAccessRights effectiveRights = FormAccessRights.None;
        foreach (FormAccessRights rights in grantedRights)
        {
            effectiveRights |= rights;
        }

        return (effectiveRights & requiredRights) == requiredRights;
    }

    private static bool IsInRole(FormActor actor, string role) =>
        actor.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
