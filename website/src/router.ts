import { documents } from "./docs";
import { createRouter, createWebHashHistory } from "vue-router";

export const router = createRouter({
  history: createWebHashHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", component: () => import("./views/HomeView.vue") },
    { path: "/docs", redirect: "/docs/getting-started" },
    {
      path: "/docs/:slug(.*)",
      component: () => import("./views/DocsView.vue"),
    },
    { path: "/:pathMatch(.*)*", redirect: "/docs/not-found" },
  ],
  scrollBehavior(to, _from, saved) {
    if (saved) return saved;
    if (to.hash) return { el: to.hash, top: 24 };
    return { top: 0 };
  },
});

router.afterEach((to) => {
  const page = documents.find((doc) => doc.slug === to.params.slug);
  const title =
    to.path === "/"
      ? "SwiftDispatch — Lightweight .NET mediator with Native AOT"
      : `${page?.title ?? "Page not found"} · SwiftDispatch`;
  const description =
    to.path === "/"
      ? "A lightweight, MIT-licensed MediatR alternative for .NET. Source-generated registration and dispatch, Native AOT support, requests, notifications, and streaming."
      : (page?.source
          .split("\n")
          .find((line) => line.length > 60 && !line.startsWith("#"))
          ?.replace(/\*\*/g, "") ??
        "Explore SwiftDispatch documentation and examples.");
  document.title = title;
  document
    .querySelector('meta[name="description"]')
    ?.setAttribute("content", description);
  document
    .querySelector('meta[property="og:title"]')
    ?.setAttribute("content", title);
  document
    .querySelector('meta[property="og:description"]')
    ?.setAttribute("content", description);
});
