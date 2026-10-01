"use client";

import Image from "@tiptap/extension-image";
import TextAlign from "@tiptap/extension-text-align";
import { TableKit } from "@tiptap/extension-table";
import { EditorContent, useEditor, useEditorState } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import {
  AlignCenter,
  AlignJustify,
  AlignLeft,
  AlignRight,
  Bold,
  Braces,
  ImagePlus,
  Italic,
  List,
  ListOrdered,
  Redo2,
  Table2,
  Underline,
  Undo2,
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import type { PlaceholderDto } from "@/types/api";

type RichTemplateEditorProps = Readonly<{
  content: string;
  savedContent: string;
  editable: boolean;
  placeholders: readonly PlaceholderDto[];
  onChange: (content: string) => void;
}>;

export function RichTemplateEditor({
  content,
  savedContent,
  editable,
  placeholders,
  onChange,
}: RichTemplateEditorProps) {
  const [imageOpen, setImageOpen] = useState(false);
  const [imageUrl, setImageUrl] = useState("");
  const [imageAlt, setImageAlt] = useState("");
  const [imageError, setImageError] = useState<string | null>(null);
  const savedContentRef = useRef(savedContent);
  const normalizedSavedContentRef = useRef(content);
  const lastEmittedContentRef = useRef<string | null>(null);
  const editorReadyRef = useRef(false);
  const editor = useEditor({
    immediatelyRender: false,
    editable,
    content,
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3] },
      }),
      TextAlign.configure({
        types: ["heading", "paragraph"],
      }),
      TableKit.configure({
        table: {
          resizable: true,
          HTMLAttributes: { class: "template-table" },
        },
      }),
      Image.configure({
        allowBase64: false,
        HTMLAttributes: { class: "template-image" },
      }),
    ],
    onCreate: ({ editor: currentEditor }) => {
      normalizedSavedContentRef.current = currentEditor.getHTML();
      editorReadyRef.current = true;
    },
    onUpdate: ({ editor: currentEditor }) => {
      if (!editorReadyRef.current) return;
      const editorContent = currentEditor.getHTML();
      const nextContent =
        editorContent === normalizedSavedContentRef.current
          ? savedContentRef.current
          : editorContent;
      lastEmittedContentRef.current = nextContent;
      onChange(nextContent);
    },
    editorProps: {
      attributes: {
        "aria-label": editable
          ? "Rich template content editor"
          : "Published template content",
      },
    },
  });

  useEffect(() => {
    editor?.setEditable(editable);
  }, [editable, editor]);

  useEffect(() => {
    if (!editor || lastEmittedContentRef.current === content) {
      lastEmittedContentRef.current = null;
      return;
    }
    if (editor.isFocused || editor.getHTML() === content) return;
    editor.commands.setContent(content, { emitUpdate: false });
  }, [content, editor]);

  useEffect(() => {
    savedContentRef.current = savedContent;
    if (editor && content === savedContent) {
      normalizedSavedContentRef.current = editor.getHTML();
    }
  }, [content, editor, savedContent]);

  const editorState = useEditorState({
    editor,
    selector: ({ editor: currentEditor }) => {
      if (!currentEditor) return null;
      const headingLevel = ([1, 2, 3] as const).find((level) =>
        currentEditor.isActive("heading", { level }),
      );
      return {
        block: headingLevel ? `heading-${headingLevel}` : "paragraph",
        bold: currentEditor.isActive("bold"),
        italic: currentEditor.isActive("italic"),
        underline: currentEditor.isActive("underline"),
        bulletList: currentEditor.isActive("bulletList"),
        orderedList: currentEditor.isActive("orderedList"),
        alignLeft: currentEditor.isActive({ textAlign: "left" }),
        alignCenter: currentEditor.isActive({ textAlign: "center" }),
        alignRight: currentEditor.isActive({ textAlign: "right" }),
        alignJustify: currentEditor.isActive({ textAlign: "justify" }),
        inTable: currentEditor.isActive("table"),
        canUndo: currentEditor.can().undo(),
        canRedo: currentEditor.can().redo(),
      };
    },
  });

  function setBlock(value: string) {
    if (!editor) return;
    if (value === "paragraph") {
      editor.chain().focus().setParagraph().run();
      return;
    }

    const level = Number(value.replace("heading-", "")) as 1 | 2 | 3;
    editor.chain().focus().setHeading({ level }).run();
  }

  function insertPlaceholder(key: string) {
    if (!editor || !key) return;
    editor.chain().focus().insertContent(`{{${key}}}`).run();
  }

  function runTableAction(action: string) {
    if (!editor || !action) return;
    const chain = editor.chain().focus();
    if (action === "insert")
      chain.insertTable({ rows: 3, cols: 3, withHeaderRow: true });
    if (action === "row-before") chain.addRowBefore();
    if (action === "row-after") chain.addRowAfter();
    if (action === "column-before") chain.addColumnBefore();
    if (action === "column-after") chain.addColumnAfter();
    if (action === "delete-row") chain.deleteRow();
    if (action === "delete-column") chain.deleteColumn();
    if (action === "delete-table") chain.deleteTable();
    chain.run();
  }

  function addImage() {
    if (!editor) return;

    let parsedUrl: URL;
    try {
      parsedUrl = new URL(imageUrl);
    } catch {
      setImageError("Enter a complete image URL.");
      return;
    }

    if (!["http:", "https:"].includes(parsedUrl.protocol)) {
      setImageError("Image URLs must use HTTP or HTTPS.");
      return;
    }

    editor
      .chain()
      .focus()
      .setImage({
        src: parsedUrl.toString(),
        alt: imageAlt.trim() || undefined,
      })
      .run();
    setImageOpen(false);
    setImageUrl("");
    setImageAlt("");
    setImageError(null);
  }

  return (
    <div className="overflow-hidden border bg-white">
      {editable ? (
        <div
          role="toolbar"
          aria-label="Template formatting"
          className="flex flex-wrap items-center gap-1 border-b bg-muted/40 p-2"
        >
          <select
            value={editorState?.block ?? "paragraph"}
            onChange={(event) => setBlock(event.target.value)}
            className="h-8 rounded-lg border border-input bg-background px-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/40"
            aria-label="Text style"
            disabled={!editor}
          >
            <option value="paragraph">Paragraph</option>
            <option value="heading-1">Heading 1</option>
            <option value="heading-2">Heading 2</option>
            <option value="heading-3">Heading 3</option>
          </select>

          <ToolbarDivider />
          <ToolbarButton
            label="Bold"
            active={editorState?.bold}
            disabled={!editor}
            onClick={() => editor?.chain().focus().toggleBold().run()}
          >
            <Bold />
          </ToolbarButton>
          <ToolbarButton
            label="Italic"
            active={editorState?.italic}
            disabled={!editor}
            onClick={() => editor?.chain().focus().toggleItalic().run()}
          >
            <Italic />
          </ToolbarButton>
          <ToolbarButton
            label="Underline"
            active={editorState?.underline}
            disabled={!editor}
            onClick={() => editor?.chain().focus().toggleUnderline().run()}
          >
            <Underline />
          </ToolbarButton>

          <ToolbarDivider />
          <ToolbarButton
            label="Bullet list"
            active={editorState?.bulletList}
            disabled={!editor}
            onClick={() => editor?.chain().focus().toggleBulletList().run()}
          >
            <List />
          </ToolbarButton>
          <ToolbarButton
            label="Numbered list"
            active={editorState?.orderedList}
            disabled={!editor}
            onClick={() => editor?.chain().focus().toggleOrderedList().run()}
          >
            <ListOrdered />
          </ToolbarButton>

          <ToolbarDivider />
          {(
            [
              ["left", "Align left", AlignLeft, editorState?.alignLeft],
              ["center", "Align center", AlignCenter, editorState?.alignCenter],
              ["right", "Align right", AlignRight, editorState?.alignRight],
              ["justify", "Justify", AlignJustify, editorState?.alignJustify],
            ] as const
          ).map(([alignment, label, Icon, active]) => (
            <ToolbarButton
              key={alignment}
              label={label}
              active={active}
              disabled={!editor}
              onClick={() =>
                editor?.chain().focus().setTextAlign(alignment).run()
              }
            >
              <Icon />
            </ToolbarButton>
          ))}

          <ToolbarDivider />
          <label className="relative flex h-8 items-center gap-1.5 rounded-lg border border-border bg-background px-2 text-xs font-medium hover:bg-muted focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/40">
            <Braces aria-hidden="true" className="size-3.5" />
            <span>Placeholder</span>
            <select
              value=""
              onChange={(event) => insertPlaceholder(event.target.value)}
              className="absolute inset-0 cursor-pointer opacity-0"
              aria-label="Insert placeholder"
              disabled={placeholders.length === 0}
            >
              <option value="">Insert placeholder</option>
              {placeholders.map((placeholder) => (
                <option key={placeholder.id} value={placeholder.key}>
                  {placeholder.label} ({placeholder.key})
                </option>
              ))}
            </select>
          </label>

          <label className="relative flex h-8 items-center gap-1.5 rounded-lg border border-border bg-background px-2 text-xs font-medium hover:bg-muted focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/40">
            <Table2 aria-hidden="true" className="size-3.5" />
            <span>Table</span>
            <select
              value=""
              onChange={(event) => runTableAction(event.target.value)}
              className="absolute inset-0 cursor-pointer opacity-0"
              aria-label="Table actions"
            >
              <option value="">Table actions</option>
              <option value="insert">Insert 3 × 3 table</option>
              <option value="row-before" disabled={!editorState?.inTable}>
                Add row before
              </option>
              <option value="row-after" disabled={!editorState?.inTable}>
                Add row after
              </option>
              <option value="column-before" disabled={!editorState?.inTable}>
                Add column before
              </option>
              <option value="column-after" disabled={!editorState?.inTable}>
                Add column after
              </option>
              <option value="delete-row" disabled={!editorState?.inTable}>
                Delete row
              </option>
              <option value="delete-column" disabled={!editorState?.inTable}>
                Delete column
              </option>
              <option value="delete-table" disabled={!editorState?.inTable}>
                Delete table
              </option>
            </select>
          </label>

          <ToolbarButton
            label="Insert image"
            disabled={!editor}
            onClick={() => setImageOpen(true)}
          >
            <ImagePlus />
          </ToolbarButton>

          <ToolbarDivider />
          <ToolbarButton
            label="Undo"
            disabled={!editorState?.canUndo}
            onClick={() => editor?.chain().focus().undo().run()}
          >
            <Undo2 />
          </ToolbarButton>
          <ToolbarButton
            label="Redo"
            disabled={!editorState?.canRedo}
            onClick={() => editor?.chain().focus().redo().run()}
          >
            <Redo2 />
          </ToolbarButton>
        </div>
      ) : null}

      <div className="bg-slate-100 p-3 sm:p-5">
        <EditorContent
          editor={editor}
          className={cn(
            "template-editor mx-auto max-w-[816px]",
            !editable && "template-editor-readonly",
          )}
        />
      </div>

      <Dialog open={imageOpen} onOpenChange={setImageOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Insert image</DialogTitle>
            <DialogDescription>
              Add an image hosted at a public HTTP or HTTPS address.
            </DialogDescription>
          </DialogHeader>
          {imageError ? (
            <Alert variant="destructive" role="alert">
              <AlertTitle>Image could not be inserted</AlertTitle>
              <AlertDescription>{imageError}</AlertDescription>
            </Alert>
          ) : null}
          <div className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="template-image-url">Image URL</Label>
              <Input
                id="template-image-url"
                type="url"
                autoFocus
                value={imageUrl}
                placeholder="https://example.com/image.png"
                onChange={(event) => {
                  setImageUrl(event.target.value);
                  setImageError(null);
                }}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="template-image-alt">Alternative text</Label>
              <Input
                id="template-image-alt"
                value={imageAlt}
                onChange={(event) => setImageAlt(event.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setImageOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="button"
              onClick={addImage}
              disabled={!imageUrl.trim()}
            >
              Insert image
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function ToolbarButton({
  label,
  active = false,
  disabled,
  onClick,
  children,
}: {
  label: string;
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      size="icon-sm"
      className={cn(active && "bg-muted text-foreground")}
      aria-label={label}
      aria-pressed={active || undefined}
      title={label}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </Button>
  );
}

function ToolbarDivider() {
  return <span aria-hidden="true" className="mx-0.5 h-5 w-px bg-border" />;
}
