# SwiftDispatch website

Empty Vue 3 project using TypeScript and Vite.

Requires Node.js `^22.18.0 || >=24.12.0` and npm.

Run commands from this directory:

```sh
npm ci
npm run dev
```

`npm run build` checks types and builds production files in `dist/`.
`npm run preview` serves the production build locally.

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
