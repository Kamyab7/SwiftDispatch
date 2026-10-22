<script setup lang="ts">
import { computed, watchEffect } from "vue";
import { useRoute } from "vue-router";
import { documents, renderDocument } from "../docs";
import logo from "../assets/logo.png";

const route = useRoute();
const page = computed(() =>
  documents.find((doc) => doc.slug === route.params.slug),
);
const pageIndex = computed(() => documents.findIndex((doc) => doc.slug === route.params.slug));
const previousPage = computed(() => pageIndex.value > 0 ? documents[pageIndex.value - 1] : undefined);
const nextPage = computed(() => pageIndex.value >= 0 ? documents[pageIndex.value + 1] : undefined);
const rendered = computed(() =>
  renderDocument(
    page.value?.source ??
      "# Page not found\n\nThis documentation page does not exist. Choose a guide from the navigation.",
    page.value?.slug,
  ),
);
watchEffect(() => {
  document.title = `${page.value?.title ?? "Page not found"} · SwiftDispatch`;
});
</script>

<template>
  <div class="docs-site">
    <RouterLink
      class="docs-skip"
      :to="{ path: route.path, hash: '#doc-content' }"
      >Skip to content</RouterLink
    >
    <header class="header wrap">
      <RouterLink class="brand" to="/"
        ><img :src="logo" width="38" height="38" alt="" /><span
          >Swift<span class="brand-accent">Dispatch</span></span
        ></RouterLink
      >
      <span class="docs-badge">DOCUMENTATION</span>
      <a
        class="text-link"
        href="https://github.com/Kamyab7/SwiftDispatch"
        target="_blank"
        rel="noopener noreferrer"
        >GitHub ↗</a
      >
    </header>
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
        <p class="eyebrow">SWIFTDISPATCH / GUIDES</p>
        <article class="markdown-body" v-html="rendered.html"></article>
        <nav v-if="previousPage || nextPage" class="docs-pagination" aria-label="Guide pagination">
          <RouterLink v-if="previousPage" class="docs-page-link" :to="`/docs/${previousPage.slug}`" rel="prev">
            <span>← Previous</span>
            <strong>{{ previousPage.title }}</strong>
          </RouterLink>
          <RouterLink v-if="nextPage" class="docs-page-link docs-page-next" :to="`/docs/${nextPage.slug}`" rel="next">
            <span>Next →</span>
            <strong>{{ nextPage.title }}</strong>
          </RouterLink>
        </nav>
        <a
          v-if="page"
          class="docs-edit text-link"
          :href="`https://github.com/Kamyab7/SwiftDispatch/edit/master/website/src/docs/${page.slug}.md`"
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
  </div>
</template>
