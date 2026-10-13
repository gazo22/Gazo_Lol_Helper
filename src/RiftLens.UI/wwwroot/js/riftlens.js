// Raccourcis globaux RiftLens (Ctrl+K / Cmd+K / "/").
export function registerHotkeys(dotnetRef) {
  const handler = (e) => {
    const typing = e.target instanceof HTMLElement && (e.target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(e.target.tagName));
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
      e.preventDefault();
      dotnetRef.invokeMethodAsync("TogglePalette");
    } else if (e.key === "/" && !typing) {
      e.preventDefault();
      dotnetRef.invokeMethodAsync("TogglePalette");
    }
  };
  document.addEventListener("keydown", handler);
  return { dispose: () => document.removeEventListener("keydown", handler) };
}

export function focus(element) {
  element?.focus();
}

export function scrollIntoView(selector) {
  document.querySelector(selector)?.scrollIntoView({ block: "nearest" });
}
