export type TemplateCategory =
  | "Business"
  | "Finance"
  | "Human resources"
  | "Operations";

export type PlaceholderDataType = "Text" | "Number" | "Date" | "Email";

export type PlaceholderDefinition = Readonly<{
  key: string;
  label: string;
  dataType: PlaceholderDataType;
  isRequired: boolean;
  defaultValue?: string;
  example?: string;
}>;

export type TemplateSummary = Readonly<{
  id: string;
  name: string;
  category: TemplateCategory;
  description: string;
  versionNumber: number;
  updatedAt: string;
  placeholderCount: number;
}>;

export type TemplateDetail = TemplateSummary &
  Readonly<{
    content: string;
    placeholders: readonly PlaceholderDefinition[];
  }>;

export type DocumentStatus = "Draft" | "Finalized";

export type DocumentRecord = Readonly<{
  id: string;
  title: string;
  templateId: string;
  templateName: string;
  templateVersion: number;
  status: DocumentStatus;
  createdAt: string;
  updatedAt: string;
  content: string;
  placeholderValues: Readonly<Record<string, string>>;
}>;
