export type UserRole = "Admin" | "User";
export type Permission =
  | "Templates.View"
  | "Templates.Manage"
  | "Documents.Create"
  | "Documents.ViewOwn"
  | "Documents.EditOwn"
  | "Users.View"
  | "Users.Manage"
  | "AuditLogs.View";
export type TemplateStatus = "Draft" | "Active" | "Inactive";
export type TemplateVersionStatus = "Draft" | "Published";
export type DocumentStatus = "Draft" | "Finalized";
export type PlaceholderDataType = "Text" | "Number" | "Date" | "Email";
export type ContentFormat = "Html" | "Json";

export type UserDto = Readonly<{
  id: string;
  username: string;
  fullName: string;
  email: string;
  role: UserRole;
  permissions: readonly Permission[];
}>;

export type LoginRequestDto = Readonly<{
  username: string;
  password: string;
}>;

export type RegisterRequestDto = Readonly<{
  username: string;
  fullName: string;
  email: string;
  password: string;
}>;

export type ForgotPasswordRequestDto = Readonly<{ email: string }>;
export type ForgotPasswordResponseDto = Readonly<{ message: string }>;
export type ResetPasswordRequestDto = Readonly<{
  token: string;
  newPassword: string;
}>;
export type UpdateOwnProfileRequestDto = Readonly<{
  fullName: string;
  email: string;
}>;
export type ChangePasswordRequestDto = Readonly<{
  currentPassword: string;
  newPassword: string;
}>;

export type LoginResponseDto = Readonly<{
  accessToken: string;
  tokenType: "Bearer";
  expiresAt: string;
  user: UserDto;
}>;

export type CategoryReferenceDto = Readonly<{
  id: string;
  name: string;
}>;

export type CurrentTemplateVersionSummaryDto = Readonly<{
  id: string;
  versionNumber: number;
  status: TemplateVersionStatus;
  isCurrent: boolean;
  contentFormat: ContentFormat;
  updatedAt: string;
  placeholderCount: number;
}>;

export type TemplateGalleryItemDto = Readonly<{
  id: string;
  name: string;
  status: TemplateStatus;
  category: CategoryReferenceDto;
  currentVersion: CurrentTemplateVersionSummaryDto;
}>;

export type TemplateDetailDto = Readonly<{
  id: string;
  name: string;
  status: TemplateStatus;
  category: CategoryReferenceDto;
  createdAt: string;
  currentVersion: CurrentTemplateVersionSummaryDto;
}>;

export type TemplateVersionDetailDto = Readonly<{
  id: string;
  templateId: string;
  versionNumber: number;
  content: string;
  contentFormat: ContentFormat;
  status: TemplateVersionStatus;
  isCurrent: boolean;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
}>;

export type PlaceholderDto = Readonly<{
  id: string;
  templateVersionId: string;
  key: string;
  label: string;
  dataType: PlaceholderDataType;
  isRequired: boolean;
  defaultValue: string | null;
}>;

export type CreateDraftDocumentRequestDto = Readonly<{
  templateVersionId: string;
  title: string;
}>;

export type PlaceholderValueInputDto = Readonly<{
  placeholderId: string;
  value: string;
}>;

export type UpdateDraftDocumentRequestDto = Readonly<{
  title?: string;
  content?: string;
  placeholderValues?: readonly PlaceholderValueInputDto[];
}>;

export type PreviewDocumentRequestDto = Readonly<{
  content: string;
  placeholderValues: readonly PlaceholderValueInputDto[];
}>;

export type FinalizeDocumentRequestDto = Readonly<{
  title: string;
  content: string;
  placeholderValues: readonly PlaceholderValueInputDto[];
}>;

export type DocumentSourceDto = Readonly<{
  templateId: string;
  templateName: string;
  templateVersionId: string;
  versionNumber: number;
}>;

export type DocumentPlaceholderValueDto = Readonly<{
  id: string;
  placeholderId: string | null;
  placeholderKeySnapshot: string;
  labelSnapshot: string;
  dataTypeSnapshot: PlaceholderDataType;
  value: string;
}>;

export type DocumentDetailDto = Readonly<{
  id: string;
  title: string;
  status: DocumentStatus;
  source: DocumentSourceDto;
  content: string;
  contentFormat: ContentFormat;
  placeholderValues: readonly DocumentPlaceholderValueDto[];
  createdAt: string;
  updatedAt: string;
  finalizedAt: string | null;
}>;

export type DocumentSummaryDto = Readonly<{
  id: string;
  title: string;
  status: DocumentStatus;
  source: DocumentSourceDto;
  createdAt: string;
  updatedAt: string;
  finalizedAt: string | null;
}>;

export type PreviewDocumentResponseDto = Readonly<{
  documentId: string;
  renderedContent: string;
  contentFormat: ContentFormat;
}>;

export type ValidationErrorDto = Readonly<{
  field: string;
  code: string;
  message: string;
  placeholderKey?: string | null;
}>;

export type ErrorResponseDto = Readonly<{
  status: number;
  code: string;
  title: string;
  detail?: string | null;
  traceId: string;
  errors?: readonly ValidationErrorDto[] | null;
}>;

export type AdminCategoryDto = Readonly<{
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
}>;

export type CreateCategoryRequestDto = Readonly<{ name: string }>;
export type UpdateCategoryRequestDto = Readonly<{ name: string }>;

export type AdminTemplateVersionSummaryDto = Readonly<{
  id: string;
  versionNumber: number;
  status: TemplateVersionStatus;
  isCurrent: boolean;
  contentFormat: ContentFormat;
  placeholderCount: number;
  createdAt: string;
  updatedAt: string;
}>;

export type AdminTemplateSummaryDto = Readonly<{
  id: string;
  name: string;
  status: TemplateStatus;
  category: CategoryReferenceDto;
  createdAt: string;
  versionCount: number;
  currentVersionNumber: number | null;
}>;

export type AdminTemplateDetailDto = Readonly<{
  id: string;
  name: string;
  status: TemplateStatus;
  category: CategoryReferenceDto;
  createdAt: string;
  versions: readonly AdminTemplateVersionSummaryDto[];
}>;

export type CreateTemplateRequestDto = Readonly<{
  name: string;
  categoryId: string;
}>;

export type UpdateDraftTemplateRequestDto = Readonly<{
  name?: string;
  categoryId?: string;
}>;

export type AdminTemplateVersionDetailDto = Readonly<{
  id: string;
  templateId: string;
  versionNumber: number;
  content: string;
  contentFormat: ContentFormat;
  status: TemplateVersionStatus;
  isCurrent: boolean;
  placeholderCount: number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
}>;

export type UpdateDraftTemplateVersionRequestDto = Readonly<{
  content: string;
}>;

export type CreatePlaceholderRequestDto = Readonly<{
  key: string;
  label: string;
  dataType: PlaceholderDataType;
  isRequired: boolean;
  defaultValue: string | null;
}>;

export type UpdatePlaceholderRequestDto = CreatePlaceholderRequestDto;

export type AdminUserDto = Readonly<{
  id: string;
  username: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}>;

export type UpdateUserRoleRequestDto = Readonly<{
  role: UserRole;
}>;

export type UpdateAdminUserRequestDto = Readonly<{
  username: string;
  fullName: string;
  email: string;
}>;

export type CreateAdminUserRequestDto = Readonly<{
  username: string;
  fullName: string;
  email: string;
  initialPassword: string;
  role: UserRole;
}>;

export type AuditActorDto = Readonly<{
  id: string;
  username: string;
  fullName: string;
}>;

export type AdminAuditLogDto = Readonly<{
  id: string;
  performedBy: AuditActorDto;
  actionType: string;
  entityType: string;
  entityId: string;
  description: string;
  createdAt: string;
}>;
