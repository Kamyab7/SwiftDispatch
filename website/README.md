# SwiftDispatch website

SwiftDispatch marketing site and documentation, built with Vue 3, TypeScript, and Vite.

Requires Node.js `^22.18.0 || >=24.12.0` and npm.

Run commands from this directory:

```sh
npm ci
npm run dev
```

`npm run build` checks types and builds production files in `dist/`.
`npm run preview` serves the production build locally.

## Adoption funnel

The home page moves visitors through the product promise, code examples, benefits, benchmark evidence, migration, adoption questions, and quick start. New projects go to the runnable console example; existing MediatR projects go to the migration guide. The evaluation guide supports compatibility decisions. Documentation points visitors onward to implementation and the clean architecture sample.

Primary conversion intent: quick-start visits and successful install-command copies. Secondary intent: migration and evaluation visits, sample exploration, GitHub, and NuGet clicks. A click or clipboard copy does not prove a package install or successful adoption.

`src/funnel.ts` emits local `swiftdispatch:conversion` CustomEvents with `name`, `placement`, and optional `destination`. Events are `quick_start_click`, `migration_click`, `evaluation_click`, `github_click`, `nuget_click`, and `install_copy`. No analytics provider, cookies, persistent identifiers, or network collection is configured. Connect your chosen analytics provider to this event before relying on conversion reports; handle consent as appropriate for that provider. Destination values omit query strings and are limited to recognized links.

For example, a provider adapter can subscribe with:

```js
window.addEventListener("swiftdispatch:conversion", ({ detail }) => {
  // Forward detail to your configured analytics provider.
});
```

The existing hash router remains compatible with static hosting. It updates titles and descriptions during client navigation, but documentation does not have separate server-rendered metadata. For an organic-search acquisition campaign, prerendered pages with path-based URLs and matching hosting rewrites are a separate deployment requirement. The root HTML includes the landing page's title, description, and social metadata.

Keep benchmark data synchronized with the linked repository reports and retain their methodology caveats. Do not present exploratory measurements as production guarantees.

## Documentation

Documentation is rendered from Markdown files in `src/docs/`. Add a `.md` file with a level-one heading (`# Page title`) to create a page and automatically include it in the sidebar. Getting started appears first; other pages are sorted by title.

Link between guides using relative Markdown links, for example `[Pipeline behaviors](pipeline-behaviors.md)`. Level-two headings populate the page's table of contents. Raw HTML is disabled in Markdown.

Open `/#/docs/getting-started` in the local dev server. Hash-based routing supports static hosts without server-side route rewrites. Changes to Markdown update through Vite; production updates require a new build.

## GitHub wiki

The `Sync documentation to wiki` workflow publishes `src/docs/*.md` to the repository wiki when these files change on `master`. It can also be run manually from Actions. `getting-started.md` becomes `Home.md`; local page links are adjusted and a sidebar is generated.

One-time setup:

1. Enable **Wikis** in the repository's **Settings → General → Features**, then create the first wiki page on GitHub.
2. Add an Actions repository secret named `WIKI_TOKEN` containing a token with permission to push to the wiki. For a public repository, a classic personal access token with `public_repo` scope can be used; it must belong to an account with repository write access.
3. Run **Sync documentation to wiki** from the Actions tab.

Edit documentation here and submit changes through pull requests. The wiki's root Markdown pages are generated: manual edits are overwritten, and pages removed from this directory are removed from the wiki. Git history is retained.
