using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.Authorization;

public static class Permissions
{
    public const string ClaimType = "permission";

    public const string TemplatesView = "Templates.View";
    public const string TemplatesManage = "Templates.Manage";
    public const string DocumentsCreate = "Documents.Create";
    public const string DocumentsViewOwn = "Documents.ViewOwn";
    public const string DocumentsEditOwn = "Documents.EditOwn";
    public const string UsersView = "Users.View";
    public const string UsersManage = "Users.Manage";
    public const string AuditLogsView = "AuditLogs.View";

    public static IReadOnlyList<string> All { get; } =
    [
        TemplatesView,
        TemplatesManage,
        DocumentsCreate,
        DocumentsViewOwn,
        DocumentsEditOwn,
        UsersView,
        UsersManage,
        AuditLogsView
    ];

    private static IReadOnlyList<string> UserPermissions { get; } =
    [
        TemplatesView,
        DocumentsCreate,
        DocumentsViewOwn,
        DocumentsEditOwn
    ];

    public static IReadOnlyList<string> ForRole(UserRole role) => role switch
    {
        UserRole.Admin => All,
        UserRole.User => UserPermissions,
        _ => []
    };
}
