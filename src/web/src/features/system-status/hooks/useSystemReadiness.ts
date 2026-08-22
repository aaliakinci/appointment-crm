import { useEffect, useState } from "react";

import type { HealthReport } from "../api/healthContract";
import { getReadiness } from "../api/systemStatusApi";

export type ReadinessState =
  | { readonly kind: "loading" }
  | { readonly kind: "ready"; readonly report: HealthReport }
  | { readonly kind: "error" };

export function useSystemReadiness() {
  const [state, setState] = useState<ReadinessState>({ kind: "loading" });

  useEffect(() => {
    const controller = new AbortController();
    void fetchReadinessState(controller.signal).then((nextState) => {
      if (!controller.signal.aborted) {
        setState(nextState);
      }
    });
    return () => controller.abort();
  }, []);

  async function retry() {
    setState({ kind: "loading" });
    setState(await fetchReadinessState());
  }

  return { state, retry };
}

async function fetchReadinessState(signal?: AbortSignal): Promise<ReadinessState> {
  try {
    return { kind: "ready", report: await getReadiness(signal) };
  } catch {
    return signal?.aborted ? { kind: "loading" } : { kind: "error" };
  }
}
