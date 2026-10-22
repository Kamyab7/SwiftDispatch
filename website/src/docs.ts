import MarkdownIt from "markdown-it";

const markdown = new MarkdownIt({ html: false, linkify: true });
const sources = import.meta.glob<string>("./docs/**/*.md", {
  query: "?raw",
  import: "default",
  eager: true,
});

export const documents = Object.entries(sources)
  .map(([file, source]) => ({
    slug: file.replace("./docs/", "").replace(/\.md$/, ""),
    title: source.match(/^#\s+(.+)$/m)?.[1] ?? file,
    source,
  }))
  .sort((a, b) => {
    if (a.slug === "getting-started") return -1;
    if (b.slug === "getting-started") return 1;
    return a.title.localeCompare(b.title);
  });

export function renderDocument(source: string, slug = "getting-started") {
  const headings: { id: string; title: string }[] = [];
  const ids = new Map<string, number>();
  const tokens = markdown.parse(source, {});
  tokens.forEach((token, index) => {
    token.children?.forEach((child) => {
      const href = child.attrGet("href");
      if (child.type !== "link_open" || typeof href !== "string" || !href) return;
      if (href.startsWith("#")) {
        child.attrSet("href", `#/docs/${slug}${href}`);
      } else if (/^[\w./-]+\.md(?:#.*)?$/.test(href)) {
        const parent = slug.split("/").slice(0, -1).join("/");
        const url = new URL(
          href,
          `https://docs.local/${parent ? parent + "/" : ""}`,
        );
        child.attrSet(
          "href",
          `#/docs/${url.pathname.slice(1).replace(/\.md$/, "")}${url.hash}`,
        );
      }
    });
    if (token.type === "heading_open") {
      const title = tokens[index + 1]?.content ?? "";
      const base =
        title
          .toLowerCase()
          .replace(/[^\p{L}\p{N}\s-]/gu, "")
          .trim()
          .replace(/\s+/g, "-") || "section";
      const count = ids.get(base) ?? 0;
      ids.set(base, count + 1);
      const id = count ? `${base}-${count}` : base;
      token.attrSet("id", id);
      if (token.tag === "h2") headings.push({ id, title });
    }
  });
  return {
    html: markdown.renderer.render(tokens, markdown.options, {}),
    headings,
  };
}
