import { isTauri } from "@tauri-apps/api/core";
import { createSignal, onCleanup, onMount } from "solid-js";

export interface DetachedWindowOptions<T = any> {
  /** Unique identifier for the window to prevent collision and scope the sync channel */
  id: string;
  /** Title of the new window */
  title?: string;
  /** Custom path to load (if not provided, uses current URL) */
  path?: string;
  /** Query parameters to append to the URL */
  queryParams?: Record<string, string>;
  /** Window width */
  width?: number;
  /** Window height */
  height?: number;
  /** X position (Left) */
  x?: number;
  /** Y position (Top) */
  y?: number;
  /** Callback for messages received from the detached window or parent */
  onMessage?: (msg: T) => void;
}

export function useDetachedWindow<T = any>(defaultOptions: DetachedWindowOptions<T>) {
  const [isDetached, setIsDetached] = createSignal(false);

  let channel: BroadcastChannel | null = null;
  let unlistenTauri: (() => void) | null = null;

  const syncEventName = `gg_window_sync_${defaultOptions.id}`;

  onMount(async () => {
    if (typeof window === "undefined") return;

    if (isTauri()) {
      try {
        const { listen } = await import("@tauri-apps/api/event");
        const unlisten = await listen<T>(syncEventName, (event) => {
          defaultOptions.onMessage?.(event.payload);
        });
        unlistenTauri = unlisten;
        return;
      } catch (e) {
        console.warn("Failed to hook into Tauri events, falling back to BroadcastChannel.", e);
      }
    }

    channel = new BroadcastChannel(syncEventName);
    channel.onmessage = (e: MessageEvent<T>) => {
      defaultOptions.onMessage?.(e.data);
    };
  });

  onCleanup(() => {
    if (unlistenTauri) {
      unlistenTauri();
    }
    channel?.close();
  });

  const sendMessage = async (msg: T) => {
    if (isTauri()) {
      try {
        const { emit } = await import("@tauri-apps/api/event");
        await emit(syncEventName, msg);
        return;
      } catch (e) {
        console.warn("Failed to emit Tauri event, falling back to BroadcastChannel.", e);
      }
    }

    channel?.postMessage(msg);
  };

  const detach = async (overrideOptions?: Partial<DetachedWindowOptions<T>>) => {
    const opts = { ...defaultOptions, ...overrideOptions };
    setIsDetached(true);

    // Build the target URL
    let targetUrl = opts.path;
    if (!targetUrl) {
      const urlObj = new URL(window.location.href);
      if (opts.queryParams) {
        Object.entries(opts.queryParams).forEach(([key, value]) => {
          urlObj.searchParams.set(key, value);
        });
      }
      targetUrl = urlObj.pathname + urlObj.search + urlObj.hash;
    }

    if (isTauri()) {
      try {
        const { WebviewWindow } = await import("@tauri-apps/api/webviewWindow");

        // Use a unique label so Tauri doesn't crash if an old window is hanging
        const windowLabel = `${opts.id}-window-${Date.now()}`;
        const webview = new WebviewWindow(windowLabel, {
          url: targetUrl,
          title: opts.title || "GeneaGrab",
          width: opts.width ?? 1400,
          height: opts.height ?? 600,
          x: opts.x, // Tauri handles undefined gracefully (centers window)
          y: opts.y,
        });

        webview.once("tauri://error", (e: any) => {
          console.error("Tauri window creation error:", e);
          setIsDetached(false);
        });

        webview.once("tauri://destroyed", () => {
          setIsDetached(false);
        });

        return;
      } catch (e: any) {
        console.warn("Failed to spawn Tauri native window.", e);
        setIsDetached(false);
      }
    }

    // Fallback for standard browsers
    const width = opts.width ?? 1400;
    const height = opts.height ?? 600;
    // Default to centering the popup if no x/y provided
    const left = opts.x ?? window.screen.width / 2 - width / 2;
    const top = opts.y ?? window.screen.height / 2 - height / 2;

    const popup = window.open(targetUrl, `${opts.id}_Window`, `width=${width},height=${height},left=${left},top=${top}`);

    if (popup) {
      const timer = setInterval(() => {
        if (popup.closed) {
          clearInterval(timer);
          setIsDetached(false);
        }
      }, 500);
      onCleanup(() => clearInterval(timer));
    } else {
      setIsDetached(false);
    }
  };

  const closeSelf = async () => {
    if (isTauri()) {
      try {
        const { getCurrentWindow } = await import("@tauri-apps/api/window");
        await getCurrentWindow().close();
        return;
      } catch (e) {
        console.warn("Tauri close API not available, falling back to window.close", e);
      }
    }
    window.close();
  };

  return { isDetached, setIsDetached, detach, closeSelf, sendMessage };
}
