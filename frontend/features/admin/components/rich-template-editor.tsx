"use client";

import { RichHtmlEditor } from "@/components/shared/rich-html-editor";
import type { PlaceholderDto } from "@/types/api";

type RichTemplateEditorProps = Readonly<{
  content: string;
  savedContent: string;
  editable: boolean;
  placeholders: readonly PlaceholderDto[];
  onChange: (content: string) => void;
}>;

export function RichTemplateEditor(props: RichTemplateEditorProps) {
  return (
    <RichHtmlEditor
      {...props}
      ariaLabel={
        props.editable
          ? "Rich template content editor"
          : "Published template content"
      }
      toolbarLabel="Template formatting"
    />
  );
}
