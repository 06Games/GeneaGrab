import { createSignal, createEffect, onMount, onCleanup } from "solid-js";
import OpenSeadragon from "openseadragon";
import { IconButton, Divider, Button } from "../../ui/primitives";
import { Icon } from "@iconify-icon/solid";

interface MainViewerProps {
  currentImage: number;
  totalImages: number;
  imageSrc?: string;
  onImageChange: (image: number) => void;
  viewerRef?: (el: HTMLElement) => void;
}

export const MainViewer = (props: MainViewerProps) => {
  let viewerContainerRef!: HTMLDivElement;
  let viewer: OpenSeadragon.Viewer | null = null;

  const [zoomDisplay, setZoomDisplay] = createSignal(100); 
  const [imageInput, setImageInput] = createSignal(String(props.currentImage));

  createEffect(() => {
    setImageInput(String(props.currentImage));
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
    });

    viewer.addHandler("animation", () => {
      if (!viewer) return;
      const currentZoom = viewer.viewport.getZoom(true);
      const homeZoom = viewer.viewport.getHomeZoom();
      if (homeZoom > 0) {
        setZoomDisplay(Math.round((currentZoom / homeZoom) * 100));
      }
    });

    onCleanup(() => {
      viewer?.destroy();
    });
  });

  createEffect(() => {
    if (!viewer) return;
    viewer.open({
      type: 'image',
      url: props.imageSrc
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
        <IconButton title="Image précédente (←)" onClick={() => props.onImageChange(Math.max(1, props.currentImage - 1))}>
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
            aria-label="Numéro d'image"
          />
          <span class="text-[13px] text-dim select-none">
            / {props.totalImages}
          </span>
        </div>

        <IconButton title="Image suivante (→)" onClick={() => props.onImageChange(Math.min(props.totalImages, props.currentImage + 1))}>
          <Icon icon="lucide:chevron-right"></Icon>
        </IconButton>

        <Divider vertical class="mx-2 h-5" />

        <IconButton title="Dézoomer (−)" onClick={handleZoomOut}>
          <Icon icon="lucide:zoom-out"></Icon>
        </IconButton>
        <span class="text-[13px] text-muted tabular-nums w-10 text-center select-none">
          {zoomDisplay()}%
        </span>
        <IconButton title="Zoomer (+)" onClick={handleZoomIn}>
          <Icon icon="lucide:zoom-in"></Icon>
        </IconButton>
        <IconButton title="Ajuster à la fenêtre (F)" onClick={handleFit}>
          <Icon icon="lucide:maximize-2"></Icon>
        </IconButton>
        <IconButton title="Pivoter 90° (R)" onClick={handleRotate}>
          <Icon icon="lucide:rotate-ccw"></Icon>
        </IconButton>

        <Divider vertical class="mx-2 h-5" />

        <IconButton title="Télécharger l'image pleine résolution">
          <Icon icon="lucide:download"></Icon>
        </IconButton>
        <IconButton title="Copier l'URL de l'image">
          <Icon icon="lucide:link"></Icon>
        </IconButton>
      </div>

      <div class="relative flex-1 min-h-0 bg-viewer-dark overflow-hidden">
        <div ref={viewerContainerRef} class="absolute inset-0 w-full h-full" />
        {!props.imageSrc && (
          <div class="pointer-events-none absolute inset-0 flex items-center justify-center bg-viewer-bg z-20">
             <div class="flex flex-col items-center gap-3 select-none">
              <Icon icon="lucide:image" class="text-subtle" width="50" height="50"></Icon>
              <span class="text-[13px] text-dim">vue {props.currentImage} — aucune image</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
