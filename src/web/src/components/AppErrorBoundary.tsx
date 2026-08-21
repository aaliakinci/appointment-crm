import { Component, type ErrorInfo, type PropsWithChildren } from "react";

interface AppErrorBoundaryState {
  readonly failed: boolean;
}

export class AppErrorBoundary extends Component<PropsWithChildren, AppErrorBoundaryState> {
  public state: AppErrorBoundaryState = { failed: false };

  public static getDerivedStateFromError(): AppErrorBoundaryState {
    return { failed: true };
  }

  public componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error("Application render failed", {
      name: error.name,
      componentStack: info.componentStack,
    });
  }

  public render() {
    if (!this.state.failed) {
      return this.props.children;
    }

    return (
      <main id="application-error" role="alert">
        <h1>Uygulama görüntülenemedi</h1>
        <p>Sayfayı yenileyin. Sorun devam ederse API trace bilgisini kullanın.</p>
        <button type="button" onClick={() => globalThis.location.reload()}>
          Yenile
        </button>
      </main>
    );
  }
}
