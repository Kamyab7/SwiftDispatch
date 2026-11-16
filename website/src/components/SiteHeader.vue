<script setup lang="ts">
import { ref } from "vue";
import logo from "../assets/logo.png";
const open = ref(false);
</script>
<template>
  <header class="header wrap">
    <RouterLink class="brand" to="/" @click="open = false"
      ><img :src="logo" alt="" width="36" height="36" /><span
        >Swift<span class="brand-accent">Dispatch</span></span
      ></RouterLink
    >
    <button
      class="menu-toggle"
      :aria-expanded="open"
      aria-controls="main-nav"
      @click="open = !open"
    >
      {{ open ? "Close" : "Menu" }}
    </button>
    <nav
      id="main-nav"
      :class="{ 'is-open': open }"
      aria-label="Main navigation"
      @click="open = false"
    >
      <RouterLink to="/#why-swiftdispatch">Why SwiftDispatch</RouterLink>
      <RouterLink to="/#benchmarks">Benchmarks</RouterLink>
      <RouterLink to="/docs/migration">Migration</RouterLink>
      <RouterLink to="/docs/getting-started">Docs</RouterLink>
      <a
        href="https://github.com/Kamyab7/SwiftDispatch"
        target="_blank"
        rel="noopener noreferrer"
        >GitHub ↗</a
      >
    </nav>
    <RouterLink class="button primary header-cta" to="/docs/getting-started"
      >Get started <span aria-hidden="true">→</span></RouterLink
    >
  </header>
</template>
