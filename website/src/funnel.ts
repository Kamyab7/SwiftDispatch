// Local integration hook. No network requests, cookies, or persistent identifiers.
export function emitConversion(
  name: string,
  placement: string,
  destination?: string,
) {
  window.dispatchEvent(
    new CustomEvent("swiftdispatch:conversion", {
      detail: { name, placement, ...(destination ? { destination } : {}) },
    }),
  );
}

export function installFunnelHooks() {
  document.addEventListener("click", (event) => {
    if (!(event.target instanceof Element)) return;
    const link = event.target.closest<HTMLAnchorElement>("a[href]");
    if (!link) return;
    const url = new URL(link.href);
    const placement = link.closest("header")
      ? "header"
      : link.closest("footer")
        ? "footer"
        : link.closest("section")?.id ||
          (link.closest(".hero") ? "hero" : "content");
    if (url.origin === location.origin) {
      const destination = url.hash.slice(1).split("#")[0];
      if (destination === "/docs/getting-started")
        emitConversion("quick_start_click", placement, destination);
      if (destination === "/docs/migration")
        emitConversion("migration_click", placement, destination);
      if (destination === "/docs/evaluating")
        emitConversion("evaluation_click", placement, destination);
    } else if (
      url.hostname === "github.com" ||
      url.hostname === "www.nuget.org"
    ) {
      emitConversion(
        url.hostname === "github.com" ? "github_click" : "nuget_click",
        placement,
        url.origin + url.pathname,
      );
    }
  });
}
