import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";

import { LocalizedLilyDateForm } from "@/shared/forms/LocalizedLilyDateForm";

import type { useTimeOff } from "../hooks/useTimeOff";

interface TimeOffEditorProps {
  readonly editor: ReturnType<typeof useTimeOff>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function TimeOffEditor({ editor, id, t }: TimeOffEditorProps) {
  return (
    <>
      <LocalizedLilyDateForm
        key={editor.revision}
        definition={editor.definition}
        instanceId={`${id}.form.${editor.revision}`}
        bindings={editor.bindings}
        controller={editor.controller}
        onSubmit={editor.submit}
        onSubmitError={({ error }) => editor.handleSubmitError(error)}
      />
      <Button
        id={`${id}.save`}
        variant="contained"
        loading={editor.formStatus.isSubmitting}
        sx={{ alignSelf: "flex-start" }}
        onClick={() => void editor.controller.submit()}
      >
        {t("app:scheduling.addTimeOff")}
      </Button>
    </>
  );
}
