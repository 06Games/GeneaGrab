import { createSignal, createEffect, onMount, onCleanup, Show, Switch, Match, createMemo } from "solid-js";
import OpenSeadragon from "openseadragon";
import { IconButton, Divider } from "../../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { getBackendService } from "../../services/apiFactory";
import { ImageMeta } from "../../types/image";

interface MainViewerProps {
  currentImage: number;
  imageMeta: ImageMeta | undefined;
  totalImages: number;
  registryId: number;
  onImageChange: (image: number) => void;
  viewerRef?: (el: HTMLElement) => void;
}

enum ImageStatus {
  Loading,
  Error,
  Loaded,
}
type ViewerState =
  | { type: ImageStatus.Loading }
  | { type: ImageStatus.Loaded }
  | { type: ImageStatus.Error; message: string };

export const MainViewer = (props: MainViewerProps) => {
  const { t } = useI18n();
  const api = getBackendService();
  let viewerContainerRef!: HTMLDivElement;
  let viewer: OpenSeadragon.Viewer | null = null;

  const [zoomDisplay, setZoomDisplay] = createSignal(100);
  const [imageInput, setImageInput] = createSignal(String(props.currentImage));
  const [imageStatus, setImageStatus] = createSignal<ViewerState>({ type: ImageStatus.Loading });
  const [downloadProgress, setDownloadProgress] = createSignal<{ current: number, total: number } | null>(null);

  createEffect(() => {
    const isImageNumValid = props.currentImage >= 1 && props.currentImage <= props.totalImages;
    setImageInput(String(props.currentImage));
    setImageStatus(isImageNumValid ? { type: ImageStatus.Loading } : { type: ImageStatus.Error, message: t("mainViewer.invalidImage", { n: props.currentImage }) });
  });

  const commitImageInput = () => {
    const n = parseInt(imageInput(), 10);
    if (!isNaN(n) && n >= 1 && n <= props.totalImages) {
      props.onImageChange(n);
    } else {
      setImageInput(String(props.currentImage));
    }
  };

  onMount(() => {
    if (props.viewerRef) props.viewerRef(viewerContainerRef);

    viewer = OpenSeadragon({
      element: viewerContainerRef,
      showNavigationControl: false,
      showSequenceControl: false,
      gestureSettingsTouch: { pinchToZoom: true },
      gestureSettingsMouse: { clickToZoom: false, scrollToZoom: true },
      imageLoaderLimit: 5,
      animationTime: 0.3,
      zoomPerScroll: 1.5,
      springStiffness: 10,
    });

    const resizeObserver = new ResizeObserver(() => {
      if (viewer && viewer.viewport) viewer.forceRedraw();
    });
    resizeObserver.observe(viewerContainerRef);

    viewer.world.addHandler("add-item", (event) => {
      const item = event.item;

      const wakeUpAndDraw = () => {
        if (!viewer) return;

        // Force a coordinate change to wake up the rendering
        viewer.viewport.zoomBy(1.00001);
        viewer.forceRedraw();

        if (imageStatus().type == ImageStatus.Loading) setImageStatus({ type: ImageStatus.Loaded });
      };

      if (item.getFullyLoaded())
        wakeUpAndDraw();
      else item.addHandler("fully-loaded-change", (e) => {
        if (e.fullyLoaded) wakeUpAndDraw();
      });
    });
    viewer.addHandler("tile-load-failed", (e) => {
      if (imageStatus().type == ImageStatus.Loading)
        setImageStatus({ type: ImageStatus.Error, message: t("mainViewer.imageError", { n: props.currentImage, e: e.message }) });
      console.error("Tile Load Failed:", e);
    });
    viewer.addHandler("open-failed", (e) => {
      setImageStatus({ type: ImageStatus.Error, message: t("mainViewer.imageError", { n: props.currentImage, e: e.message }) });
      console.error("Open Failed:", e);
    });
    viewer.addHandler("animation", () => {
      if (!viewer) return;
      const currentZoom = viewer.viewport.getZoom(true);
      const homeZoom = viewer.viewport.getHomeZoom();
      if (homeZoom > 0) setZoomDisplay(Math.round((currentZoom / homeZoom) * 100));
    });

    createEffect(() => {
      if (props.currentImage < 1 || props.currentImage > props.totalImages)
        return;

      const tileSize = props.imageMeta?.tile_size ?? 256;
      const zoomOffset = Math.log2(tileSize);

      viewer!.open({
        type: 'custom',
        width: props.imageMeta?.width,
        height: props.imageMeta?.height,
        tileSize: tileSize,
        minLevel: zoomOffset,
        getTileUrl: (level: number, x: number, y: number) => api.getTileUrl(props.registryId, props.currentImage, level - zoomOffset, x, y)
      } as any);
    });


    viewer.addHandler("canvas-key", (e) => {
      // Prevent default OpenSeadragon keyboard shortcuts
      e.preventDefaultAction = true;
    });

    const handleContextMenu = (e: MouseEvent) => {
      // Reset viewer on right-click
      e.preventDefault();
      viewer?.viewport.goHome();
    };
    viewerContainerRef.addEventListener("contextmenu", handleContextMenu);

    onCleanup(() => {
      resizeObserver.disconnect();
      if (viewerContainerRef) {
        viewerContainerRef.removeEventListener("contextmenu", handleContextMenu);
      }
      viewer?.destroy();
    });
  });

  const handleZoomIn = () => viewer?.viewport.zoomBy(1.3);
  const handleZoomOut = () => viewer?.viewport.zoomBy(0.77);
  const handleFit = () => viewer?.viewport.goHome();
  const handleRotate = () => {
    if (!viewer) return;
    const currentRot = viewer.viewport.getRotation();
    viewer.viewport.setRotation((currentRot + 90) % 360);
  };

  const handleDownloadImage = async () => {
    if (downloadProgress()) return;

    let unlisten: (() => void) | undefined;

    try {
      setDownloadProgress({ current: 0, total: 100 });
      unlisten = await api.onDownloadProgress(props.registryId, props.currentImage, (current, total) => {
        setDownloadProgress({ current, total });
      });

      const url = api.getImageUrl(props.registryId, props.currentImage);
      if (!url) throw new Error("No image URL");

      const image = await fetch(url);
      if (!image.ok) throw new Error("Fetch failed");

      const blob = await image.blob();
      const link = document.createElement("a");
      link.href = URL.createObjectURL(blob);
      link.download = `image-${props.registryId}-${props.currentImage}.jpg`;
      link.click();
    } catch (e) {
      console.error("Could not download image", e);
    } finally {
      if (unlisten) unlisten();
      setDownloadProgress(null);
    }
  }

  const handleCopyUrl = () => {
    const url = props.imageMeta?.ark_url;
    if (url) navigator.clipboard.writeText(url);
  }

  return (
    <div class="relative flex-1 flex flex-col overflow-hidden bg-viewer-bg min-h-0">
      <div class="flex-shrink-0 flex items-center gap-1 px-3 py-2 bg-panel border-b border-subtle shadow-sm z-10">
        <IconButton title={t("mainViewer.prev", { key: "←" })} onClick={() => props.onImageChange(Math.max(1, props.currentImage - 1))}>
          <Icon icon="lucide:chevron-left"></Icon>
        </IconButton>

        <div class="flex items-center gap-1.5 mx-1">
          <input
            type="text"
            value={imageInput()}
            onInput={(e) => setImageInput(e.currentTarget.value)}
            onBlur={commitImageInput}
            onKeyDown={(e) => e.key === "Enter" && commitImageInput()}
            class={[
              "w-12 h-8 text-center rounded-md border border-subtle",
              "text-[14px] text-main bg-tinted",
              "focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20",
              "transition-all tabular-nums",
            ].join(" ")}
            aria-label={t("mainViewer.ariaImageNumber")}
          />
          <span class="text-[13px] text-dim select-none">
            / {props.totalImages}
          </span>
        </div>

        <IconButton title={t("mainViewer.next", { key: "→" })} onClick={() => props.onImageChange(Math.min(props.totalImages, props.currentImage + 1))}>
          <Icon icon="lucide:chevron-right"></Icon>
        </IconButton>

        <Divider vertical class="mx-2 h-5" />

        <IconButton title={t("mainViewer.zoomOut", { key: "-" })} onClick={handleZoomOut}>
          <Icon icon="lucide:zoom-out"></Icon>
        </IconButton>
        <span class="text-[13px] text-muted tabular-nums w-10 text-center select-none">
          {zoomDisplay()}%
        </span>
        <IconButton title={t("mainViewer.zoomIn", { key: "+" })} onClick={handleZoomIn}>
          <Icon icon="lucide:zoom-in"></Icon>
        </IconButton>
        <IconButton title={t("mainViewer.fit", { key: "F" })} onClick={handleFit}>
          <Icon icon="lucide:maximize-2"></Icon>
        </IconButton>
        <IconButton title={t("mainViewer.rotate", { key: "R" })} onClick={handleRotate}>
          <Icon icon="lucide:rotate-ccw"></Icon>
        </IconButton>

        <Divider vertical class="mx-2 h-5" />

        <Show when={downloadProgress()} fallback={
          <IconButton title={t("mainViewer.download")} onClick={handleDownloadImage}>
            <Icon icon="lucide:download"></Icon>
          </IconButton>
        }>
          {(progress) => (
            <div class="relative inline-grid place-items-center w-8 h-8 rounded-md cursor-wait" title={`${progress().current} / ${progress().total}`}>
              <Icon icon="lucide:download" class="col-start-1 row-start-1 opacity-30"></Icon>

              <Icon
                icon="lucide:download"
                class="col-start-1 row-start-1 text-accent transition-all duration-300"
                style={{ "clip-path": `inset(0 0 ${100 - (progress().current / Math.max(1, progress().total)) * 100}% 0)` }}
              ></Icon>
            </div>
          )}
        </Show>

        <IconButton title={t("mainViewer.copyUrl")} onClick={handleCopyUrl} disabled={!props.imageMeta?.ark_url}>
          <Icon icon="lucide:link"></Icon>
        </IconButton>
      </div>

      <div class="relative flex-1 min-h-0 bg-viewer-dark overflow-hidden">
        <div ref={viewerContainerRef} class="absolute inset-0 w-full h-full" />

        <Show when={imageStatus().type !== ImageStatus.Loaded && imageStatus()}>
          {(state) => (
            <div class="pointer-events-none absolute inset-0 flex items-center justify-center bg-viewer-bg z-20">
              <div class="flex flex-col items-center gap-3 select-none">
                <Switch>
                  <Match when={state().type === ImageStatus.Error}>
                    <Icon icon="lucide:image-off" class="text-subtle" width="50" height="50" />
                    <span class="text-[13px] text-dim text-center whitespace-pre-line">{(state() as any).message}</span>
                  </Match>
                  <Match when={state().type === ImageStatus.Loading}>
                    <Icon icon="lucide:loader-2" class="animate-spin text-subtle" width="25" height="25" />
                  </Match>
                </Switch>
              </div>
            </div>
          )}
        </Show>
      </div>
    </div>
  );
};
