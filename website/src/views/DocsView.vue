<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { documents, renderDocument } from "../docs";
import SiteHeader from "../components/SiteHeader.vue";
import SiteFooter from "../components/SiteFooter.vue";

const route = useRoute();
const page = computed(() =>
  documents.find((doc) => doc.slug === route.params.slug),
);
const pageIndex = computed(() =>
  documents.findIndex((doc) => doc.slug === route.params.slug),
);
const previousPage = computed(() =>
  pageIndex.value > 0 ? documents[pageIndex.value - 1] : undefined,
);
const nextPage = computed(() =>
  pageIndex.value >= 0 ? documents[pageIndex.value + 1] : undefined,
);
const rendered = computed(() =>
  renderDocument(
    page.value?.source ??
      "# Page not found\n\nThis documentation page does not exist. Choose a guide from the navigation.",
    page.value?.slug,
  ),
);
</script>

<template>
  <div class="docs-site">
    <RouterLink
      class="docs-skip"
      :to="{ path: route.path, hash: '#doc-content' }"
      >Skip to content</RouterLink
    >
    <SiteHeader />
    <div class="docs-layout wrap">
      <aside class="docs-sidebar">
        <nav aria-label="Documentation">
          <p class="eyebrow">THE GUIDES</p>
          <RouterLink
            v-for="doc in documents"
            :key="doc.slug"
            :to="`/docs/${doc.slug}`"
            >{{ doc.title }}</RouterLink
          >
        </nav>
        <RouterLink class="docs-home" to="/">← Back to home</RouterLink>
      </aside>
      <main id="doc-content" class="docs-main" tabindex="-1">
        <p class="eyebrow">TRUEMEDIATOR / GUIDES</p>
        <article class="markdown-body" v-html="rendered.html"></article>
        <nav
          v-if="previousPage || nextPage"
          class="docs-pagination"
          aria-label="Guide pagination"
        >
          <RouterLink
            v-if="previousPage"
            class="docs-page-link"
            :to="`/docs/${previousPage.slug}`"
            rel="prev"
          >
            <span>← Previous</span>
            <strong>{{ previousPage.title }}</strong>
          </RouterLink>
          <RouterLink
            v-if="nextPage"
            class="docs-page-link docs-page-next"
            :to="`/docs/${nextPage.slug}`"
            rel="next"
          >
            <span>Next →</span>
            <strong>{{ nextPage.title }}</strong>
          </RouterLink>
        </nav>
        <section
          v-if="page"
          class="docs-next-step"
          aria-label="Put this guide into practice"
        >
          <h2>
            {{
              page.slug === "getting-started"
                ? "Take the pattern into a real application."
                : "Ready to try it in your application?"
            }}
          </h2>
          <p>
            {{
              page.slug === "getting-started"
                ? "Explore the clean architecture sample to see handlers, registration, and behaviors across project boundaries."
                : "Start with one request and its handler, then verify the behavior your project needs."
            }}
          </p>
          <a
            v-if="page.slug === 'getting-started'"
            class="button primary"
            href="https://github.com/Kamyab7/TrueMediator/tree/master/samples/TrueMediator.Sample.CleanArchitecture"
            target="_blank"
            rel="noopener noreferrer"
            >Explore the sample ↗</a
          >
          <RouterLink v-else class="button primary" to="/docs/getting-started"
            >Build your first handler →</RouterLink
          >
        </section>
        <a
          v-if="page"
          class="docs-edit text-link"
          :href="`https://github.com/Kamyab7/TrueMediator/edit/master/website/src/docs/${page.slug}.md`"
          target="_blank"
          rel="noopener noreferrer"
          >Edit this page on GitHub ↗</a
        >
      </main>
      <aside v-if="rendered.headings.length" class="docs-toc">
        <p class="eyebrow">ON THIS PAGE</p>
        <RouterLink
          v-for="heading in rendered.headings"
          :key="heading.id"
          :to="{ path: route.path, hash: `#${heading.id}` }"
          >{{ heading.title }}</RouterLink
        >
      </aside>
    </div>
    <SiteFooter />
  </div>
</template>
