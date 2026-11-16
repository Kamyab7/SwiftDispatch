<script setup lang="ts">
import logo from "../assets/logo.png";
</script>
<template>
  <footer class="footer wrap">
    <div>
      <RouterLink class="brand" to="/"
        ><img :src="logo" alt="" width="30" height="30" /><span
          >Swift<span class="brand-accent">Dispatch</span></span
        ></RouterLink
      >
      <p>A lightweight mediator for .NET. MIT licensed.</p>
    </div>
    <nav aria-label="Footer navigation">
      <RouterLink to="/docs/getting-started">Quick start</RouterLink
      ><RouterLink to="/docs/migration">Migration guide</RouterLink
      ><a
        href="https://www.nuget.org/packages/SwiftDispatch"
        target="_blank"
        rel="noopener noreferrer"
        >NuGet ↗</a
      ><a
        href="https://github.com/Kamyab7/SwiftDispatch/blob/master/LICENSE"
        target="_blank"
        rel="noopener noreferrer"
        >MIT license ↗</a
      >
    </nav>
  </footer>
</template>
