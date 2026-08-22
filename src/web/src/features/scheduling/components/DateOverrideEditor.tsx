import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { ToggleButton } from "@lily_platform/lily_ui/ui/atoms/ToggleButton";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import type { useDateOverrides } from "../hooks/useDateOverrides";
import { PeriodArrayEditor } from "./PeriodArrayEditor";

interface DateOverrideEditorProps {
  readonly editor: ReturnType<typeof useDateOverrides>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function DateOverrideEditor({ editor, id, t }: DateOverrideEditorProps) {
  return (
    <Stack id={id} spacing={2}>
      <ToggleButton
        id={`${id}.mode`}
        label={t("app:scheduling.overrideMode")}
        exclusive
        value={editor.mode}
        options={[
          { id: "closed", value: "closed", label: t("app:scheduling.closed") },
          { id: "open", value: "open", label: t("app:scheduling.open") },
        ]}
        onValueChange={editor.changeMode}
      />
      <Alert id={`${id}.modeHelp`} severity="info">
        {editor.mode === "closed"
          ? t("app:scheduling.overrideClosedHelp")
          : t("app:scheduling.overrideOpenHelp")}
      </Alert>
      <LilyForm
        key={editor.revision}
        definition={editor.definition}
        instanceId={`${id}.form.${editor.revision}`}
        initialValues={editor.initialValues}
        controller={editor.controller}
        arrayRenderers={{
          periods: ({ item, index, updateItem }) => (
            <PeriodArrayEditor
              id={`${id}.period.${index}`}
              includeDay={false}
              item={item}
              updateItem={updateItem}
              t={t}
            />
          ),
        }}
        onSubmit={editor.submit}
        onSubmitError={({ error }) => editor.handleSubmitError(error)}
      />
      <Stack id={`${id}.actions`} direction="row" spacing={1}>
        <Button
          id={`${id}.save`}
          variant="contained"
          loading={editor.formStatus.isSubmitting}
          onClick={() => void editor.controller.submit()}
        >
          {t("app:common.save")}
        </Button>
        {(editor.editing || editor.isDirty) && (
          <Button id={`${id}.cancel`} onClick={editor.cancelEdit}>
            {t("app:common.close")}
          </Button>
        )}
      </Stack>
    </Stack>
  );
}
