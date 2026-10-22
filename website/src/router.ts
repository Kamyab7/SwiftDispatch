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
