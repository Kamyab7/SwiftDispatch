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
