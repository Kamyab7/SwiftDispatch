import { documents } from "./docs";
import { createRouter, createWebHashHistory } from "vue-router";

export const router = createRouter({
  history: createWebHashHistory(import.meta.env.BASE_URL),
  routes: [
    { path: "/", component: () => import("./views/HomeView.vue") },
    { path: "/privacy", component: () => import("./views/PrivacyView.vue") },
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

const guideDescriptions: Record<string, string> = {
  "getting-started": "Install SwiftDispatch and build your first .NET request handler with source-generated registration, dependency injection, and Native AOT support.",
  "evaluating": "Evaluate SwiftDispatch for your .NET project: compare mediator features, supported frameworks, Native AOT support, and migration requirements.",
  "migration": "Migrate from MediatR to SwiftDispatch. Learn how to update request handlers, dependency injection, pipeline behaviors, and notifications.",
  "pipeline-behaviors": "Build ordered SwiftDispatch pipeline behaviors for .NET request handlers, including validation, logging, retries, and short-circuiting.",
  "notifications": "Publish SwiftDispatch notifications to multiple .NET handlers with sequential, parallel, or custom execution strategies and cancellation support.",
  "streaming": "Stream .NET request results with SwiftDispatch using IAsyncEnumerable, stream request handlers, cancellation, and stream pipeline behaviors.",
  "fluentvalidation": "Add asynchronous FluentValidation to SwiftDispatch request pipelines. Register validators, aggregate failures, and configure validation behavior ordering.",
  "entity-framework-core": "Use opt-in Entity Framework Core transactions with SwiftDispatch requests. Configure transaction attributes, database contexts, commit, and rollback."
};

router.afterEach((to) => {
  const page = documents.find((doc) => doc.slug === to.params.slug);
  const title =
    to.path === "/"
      ? "SwiftDispatch — MediatR alternative for .NET with Native AOT"
      : to.path === "/privacy"
        ? "Privacy policy · SwiftDispatch"
        : `${page?.title ?? "Page not found"} · SwiftDispatch`;
  const description =
    to.path === "/"
      ? "A free, MIT-licensed MediatR alternative for .NET with source-generated dispatch, Native AOT support, CQRS, ordered pipelines, notifications, and streaming."
      : to.path === "/privacy"
        ? "How the SwiftDispatch website handles hosting logs, local interaction events, cookies, and privacy questions."
        : (page && guideDescriptions[page.slug]) ||
          "Explore SwiftDispatch documentation and examples for .NET requests, pipeline behaviors, notifications, and streaming.";
  document
    .querySelector('meta[name="twitter:title"]')
    ?.setAttribute("content", title);
  document
    .querySelector('meta[name="twitter:description"]')
    ?.setAttribute("content", description);
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
