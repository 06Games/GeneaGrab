import { createSignal, createEffect, onMount, onCleanup } from "solid-js";
import OpenSeadragon from "openseadragon";
import { IconButton, Divider } from "../../ui/primitives";
import { Icon } from "@iconify-icon/solid";
import { useI18n } from "../../ui/i18n";
import { getBackendService } from "../../services/apiFactory";

interface MainViewerProps {
  currentImage: number;
  totalImages: number;
  registryId: string;
  onImageChange: (image: number) => void;
  viewerRef?: (el: HTMLElement) => void;
}

export const MainViewer = (props: MainViewerProps) => {
  const { t } = useI18n();
  const api = getBackendService();
  let viewerContainerRef!: HTMLDivElement;
  let viewer: OpenSeadragon.Viewer | null = null;

  const [zoomDisplay, setZoomDisplay] = createSignal(100);
  const [imageInput, setImageInput] = createSignal(String(props.currentImage));
  const [imageSrc, setImageSrc] = createSignal<string | null>(null);
  const [imageError, setImageError] = createSignal(false);

  createEffect(() => {
    setImageInput(String(props.currentImage));
    setImageError(false);
    setImageSrc(api.getImageUrl(props.registryId, props.currentImage, false));
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
      };

      if (item.getFullyLoaded())
        wakeUpAndDraw();
      else item.addHandler("fully-loaded-change", (e) => {
        if (e.fullyLoaded) wakeUpAndDraw();
      });
    });

    viewer.addHandler("open-failed", () => setImageError(true));

    viewer.addHandler("animation", () => {
      if (!viewer) return;
      const currentZoom = viewer.viewport.getZoom(true);
      const homeZoom = viewer.viewport.getHomeZoom();
      if (homeZoom > 0) setZoomDisplay(Math.round((currentZoom / homeZoom) * 100));
    });

    createEffect(() => {
      const url = imageSrc();
      if (!url) return;

      viewer!.open({
        type: 'image',
        url: url
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

        <IconButton title={t("mainViewer.zoomOut", { key: "−" })} onClick={handleZoomOut}>
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

        <IconButton title={t("mainViewer.download")}>
          <Icon icon="lucide:download"></Icon>
        </IconButton>
        <IconButton title={t("mainViewer.copyUrl")}>
          <Icon icon="lucide:link"></Icon>
        </IconButton>
      </div>

      <div class="relative flex-1 min-h-0 bg-viewer-dark overflow-hidden">
        <div ref={viewerContainerRef} class="absolute inset-0 w-full h-full" />
        {(!imageSrc() || imageError()) && (
          <div class="pointer-events-none absolute inset-0 flex items-center justify-center bg-viewer-bg z-20">
            <div class="flex flex-col items-center gap-3 select-none">
              <Icon icon={imageError() ? "lucide:image-off" : "lucide:image"} class="text-subtle" width="50" height="50"></Icon>
              <span class="text-[13px] text-dim">{t("mainViewer.noImage", { n: props.currentImage })}</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
