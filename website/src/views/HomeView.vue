<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import logo from "../assets/logo.png";

const github = "https://github.com/Kamyab7/SwiftDispatch";
const reducedMotion = ref(false);
const pipelineTick = ref(0);
const pipelineStages = [
  {
    title: "Validation",
    detail: "Checking request data",
    done: "Request validated",
    icon: "✓",
  },
  {
    title: "Authentication",
    detail: "Checking identity",
    done: "Identity authenticated",
    icon: "◇",
  },
  {
    title: "Logging",
    detail: "Recording request",
    done: "Request logged",
    icon: "≡",
  },
  {
    title: "Handler",
    detail: "Executing handler",
    done: "Response returned",
    icon: "↗",
  },
];
const demoRequests = ["CreateOrder", "GetCustomer"];
const activeStage = computed(() =>
  reducedMotion.value ? 4 : pipelineTick.value % 5,
);
const activeRequest = computed(
  () => demoRequests[Math.floor(pipelineTick.value / 5) % demoRequests.length],
);
let pipelineTimer: ReturnType<typeof setInterval> | undefined;
let motionPreference: MediaQueryList | undefined;
function updateMotionPreference() {
  reducedMotion.value = motionPreference?.matches ?? false;
}
onMounted(() => {
  document.title = "SwiftDispatch — A clearer path for your requests";
  motionPreference = window.matchMedia("(prefers-reduced-motion: reduce)");
  updateMotionPreference();
  motionPreference.addEventListener("change", updateMotionPreference);
  pipelineTimer = window.setInterval(() => {
    if (!reducedMotion.value) pipelineTick.value += 1;
  }, 1200);
});
onUnmounted(() => {
  window.clearInterval(pipelineTimer);
  motionPreference?.removeEventListener("change", updateMotionPreference);
});

const copied = ref(false);
const copyError = ref(false);
const command = "dotnet add package SwiftDispatch";
async function copyCommand() {
  try {
    await navigator.clipboard.writeText(command);
    copied.value = true;
    window.setTimeout(() => {
      copied.value = false;
    }, 2000);
  } catch {
    copyError.value = true;
  }
}
const features = [
  {
    number: "01",
    icon: "⌘",
    title: "Generated. Not reflected.",
    text: "Registration and dispatch are generated at compile time. Keep your runtime focused on the work that matters.",
    tag: "SOURCE GENERATION",
  },
  {
    number: "02",
    icon: "↗",
    title: "Native AOT, naturally.",
    text: "Built with trimming and Native AOT in mind. Take your mediator from source to a native executable.",
    tag: "AOT COMPATIBLE",
  },
  {
    number: "03",
    icon: "≋",
    title: "Your pipeline. Your rules.",
    text: "Compose ordered behaviors for validation, logging, retries, and more. Keep cross-cutting concerns out of your handlers.",
    tag: "ORDERED BEHAVIORS",
  },
];
const faqs = [
  {
    q: "What is SwiftDispatch?",
    a: "SwiftDispatch is a free, lightweight mediator library targeting .NET Standard 2.0, .NET 8, and .NET 10. It connects requests to handlers, publishes notifications, and supports ordered pipeline behaviors using source-generated registration and dispatch.",
  },
  {
    q: "How is it different from MediatR?",
    a: "SwiftDispatch uses familiar mediator interfaces while generating registration and dispatch at compile time instead of relying on runtime reflection. It is designed for Native AOT and trimming, and is available under the MIT license.",
  },
  {
    q: "Can I migrate an existing application?",
    a: "For supported features, update your namespaces to SwiftDispatch.Contracts and register your handlers with AddSwiftDispatch and GenerateSwiftDispatch. Check the repository README for setup details and supported behavior before migrating.",
  },
  {
    q: "Which .NET versions are supported?",
    a: "SwiftDispatch targets .NET Standard 2.0, .NET 8, and .NET 10. The source generator is included automatically with the SwiftDispatch package.",
  },
  {
    q: "Is it free for commercial projects?",
    a: "Yes. SwiftDispatch is licensed under MIT and is free for personal and commercial use.",
  },
];
</script>

<template>
  <div class="site-shell">
    <header class="header wrap">
      <a class="brand" href="#/" aria-label="SwiftDispatch home"
        ><img :src="logo" alt="" width="38" height="38" /><span
          >Swift<span class="brand-accent">Dispatch</span></span
        ></a
      >
      <nav aria-label="Main navigation">
        <a href="#/#features">Features</a
        ><a href="#/#faq">FAQ</a>
        <RouterLink to="/docs/getting-started">Documentation</RouterLink>
      </nav>
      <a
        class="github-nav"
        aria-label="SwiftDispatch on GitHub"
        :href="github"
        target="_blank"
        rel="noopener noreferrer"
        ><svg viewBox="0 0 24 24" aria-hidden="true">
          <path
            fill="currentColor"
            d="M12 .8a11.3 11.3 0 0 0-3.6 22c.6.1.8-.2.8-.5v-2c-3.3.7-4-1.4-4-1.4-.5-1.3-1.3-1.7-1.3-1.7-1.1-.7.1-.7.1-.7 1.2.1 1.8 1.2 1.8 1.2 1.1 1.8 2.8 1.3 3.4 1 .1-.8.4-1.3.7-1.6-2.6-.3-5.4-1.3-5.4-5.7 0-1.3.5-2.3 1.2-3.1-.1-.3-.5-1.5.1-3.1 0 0 1-.3 3.1 1.2a10.8 10.8 0 0 1 5.7 0c2.2-1.5 3.1-1.2 3.1-1.2.6 1.6.2 2.8.1 3.1.8.8 1.2 1.8 1.2 3.1 0 4.4-2.8 5.4-5.4 5.7.4.4.8 1.1.8 2.2v3c0 .3.2.6.8.5A11.3 11.3 0 0 0 12 .8Z"
          /></svg
      ></a>
    </header>

    <main>
      <section class="hero wrap">
        <h1>Your requests.<br /><span>A clearer path.</span></h1>
        <p class="hero-description">
          A free, lightweight MediatR alternative with Native AOT support.
        </p>
        <button class="install-command" @click="copyCommand">
          <span aria-hidden="true">$</span><code>{{ command }}</code>
          <span class="copy-label" aria-live="polite">
            {{ copied ? "Copied" : "Copy" }}
            <svg class="copy-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path v-if="copied" d="m5 12 4 4L19 6" />
              <template v-else>
                <rect x="8" y="8" width="12" height="12" rx="2" />
                <path d="M16 8V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2" />
              </template>
            </svg>
          </span>
        </button>
        <p v-if="copyError" role="status">
          Select and copy the command above to install.
        </p>
        <div class="hero-actions">
          <a
            class="button primary"
            href="#/docs/getting-started"
            target="_blank"
            rel="noopener noreferrer"
            >Get started <span aria-hidden="true">↗</span></a
          ><a class="button secondary" href="#/docs/getting-started"
            >Documentation <span aria-hidden="true">→</span></a
          >
        </div>

        <div id="how-it-works" class="simulation">
          <div class="simulation-bar">
            <span><i class="status-dot"></i> DISPATCH IN MOTION</span>
          </div>
          <div
            class="routing"
            role="img"
            aria-label="Illustrative routing: requests pass through SwiftDispatch and pipeline behaviors to their corresponding handlers."
          >
            <svg
              class="routes"
              viewBox="0 0 1000 310"
              fill="none"
              aria-hidden="true"
            >
              <defs>
                <linearGradient
                  id="route-color"
                  gradientUnits="userSpaceOnUse"
                  x1="170"
                  y1="155"
                  x2="830"
                  y2="155"
                >
                  <stop stop-color="#00deeb" />
                  <stop offset="1" stop-color="#3265ff" />
                </linearGradient>
              </defs>
              <g class="route-lines">
                <path
                  d="M170 65 H260 Q290 65 290 95 V125 Q290 155 330 155 H425"
                />
                <path d="M170 155 H425" />
                <path
                  d="M170 245 H260 Q290 245 290 215 V185 Q290 155 330 155 H425"
                />
                <path
                  d="M575 155 H660 Q700 155 700 125 V95 Q700 65 735 65 H830"
                />
                <path d="M575 155 H830" />
                <path
                  d="M575 155 H660 Q700 155 700 185 V215 Q700 245 735 245 H830"
                />
              </g>
              <g class="packets">
                <circle r="4" class="packet p1" />
                <circle r="4" class="packet p2" />
                <circle r="4" class="packet p3" />
                <circle r="4" class="packet p4" />
                <circle r="4" class="packet p5" />
                <circle r="4" class="packet p6" />
              </g>
            </svg>
            <div class="route-column requests">
              <span class="route-caption">YOUR REQUESTS</span>
              <div class="route-node">
                <span class="node-symbol cyan">↗</span><code>CreateOrder</code
                ><span class="node-light"></span>
              </div>
              <div class="route-node">
                <span class="node-symbol blue">↗</span><code>GetCustomer</code
                ><span class="node-light"></span>
              </div>
              <div class="route-node">
                <span class="node-symbol violet">↗</span><code>OrderPlaced</code
                ><span class="node-light"></span>
              </div>
            </div>
            <div class="dispatch-core">
              <div class="core-halo"></div>
              <div class="core-icon">
                <img :src="logo" width="74" height="74" alt="" />
              </div>
              <strong>SwiftDispatch</strong><span>COMPILE-TIME ROUTING</span>
            </div>
            <div class="route-column handlers">
              <span class="route-caption">THE RIGHT HANDLER</span>
              <div class="route-node">
                <span class="handler-mark">⌘</span><code>OrderHandler</code
                ><span class="node-check">✓</span>
              </div>
              <div class="route-node">
                <span class="handler-mark">⌘</span><code>CustomerHandler</code
                ><span class="node-check">✓</span>
              </div>
              <div class="route-node">
                <span class="handler-mark">⌘</span
                ><code>NotificationHandlers</code
                ><span class="node-check">✓</span>
              </div>
            </div>
          </div>
          <div
            class="pipeline-demo"
            aria-label="Illustrative custom request pipeline"
          >
            <div class="pipeline-heading">
              <span class="pipeline-label">INSIDE THE REQUEST PIPELINE</span>
              <span class="simulation-note">Illustrative custom behaviors</span>
            </div>
            <div class="pipeline-request">
              <code>{{ activeRequest }}</code>
              <span>{{
                activeStage === 4 ? "✓ Response returned" : "Processing request"
              }}</span>
            </div>
            <ol class="pipeline-stages">
              <li
                v-for="(stage, index) in pipelineStages"
                :key="stage.title"
                class="pipeline-stage"
                :class="{
                  'is-active': activeStage === index,
                  'is-complete': activeStage > index,
                }"
                :aria-current="activeStage === index ? 'step' : undefined"
              >
                <div class="stage-top">
                  <span class="stage-icon" aria-hidden="true">{{
                    activeStage > index ? "✓" : stage.icon
                  }}</span
                  ><span class="stage-number">0{{ index + 1 }}</span>
                </div>
                <strong>{{ stage.title }}</strong>
                <span class="stage-detail">{{
                  activeStage > index
                    ? stage.done
                    : activeStage === index
                      ? stage.detail
                      : "Waiting for request"
                }}</span>
                <span class="stage-progress" aria-hidden="true"><i></i></span>
              </li>
            </ol>
            <p class="pipeline-explainer">
              Example behaviors you compose: validate the payload, authenticate
              the caller, log the request, then execute the handler.
            </p>
          </div>
        </div>
      </section>

      <section id="features" class="features wrap section-space">
        <div class="section-heading">
          <div>
            <p class="eyebrow">BUILT FOR THE WAY YOU BUILD</p>
            <h2>Less plumbing.<br />More possibility.</h2>
          </div>
          <p>
            Keep your application decoupled.<br />Keep your code easy to follow.
          </p>
        </div>
        <div class="feature-grid">
          <article
            v-for="feature in features"
            :key="feature.number"
            class="feature"
          >
            <div class="feature-top">
              <span class="feature-icon">{{ feature.icon }}</span
              ><span>{{ feature.number }}</span>
            </div>
            <h3>{{ feature.title }}</h3>
            <p>{{ feature.text }}</p>
            <span class="feature-tag">{{ feature.tag }}</span>
          </article>
        </div>
      </section>

      <section class="code-section wrap section-space">
        <div class="code-copy">
          <p class="eyebrow">FAMILIAR BY DESIGN</p>
          <h2>One request.<br />One clear intention.</h2>
          <p>
            Send a request. Let the generated dispatcher find its handler. Keep
            the business logic where it belongs.
          </p>
          <a
            class="text-link"
            :href="`${github}/tree/master/samples/SwiftDispatch.Sample.Console`"
            target="_blank"
            rel="noopener noreferrer"
            >Explore the sample <span>↗</span></a
          >
        </div>
        <div class="code-window">
          <div class="code-bar">
            <span><i></i><i></i><i></i></span><span>Program.cs</span
            ><span>C#</span>
          </div>
          <pre><code><span class="comment">// Define the intent.</span>
<span class="purple">public sealed record</span> <span class="aqua">Hello</span>(<span class="purple">string</span> Name)
    : <span class="aqua">IRequest</span>&lt;<span class="purple">string</span>&gt;;

<span class="comment">// Send it to the right handler.</span>
<span class="purple">var</span> greeting = <span class="purple">await</span> mediator.<span class="aqua">Send</span>(
    <span class="purple">new</span> <span class="aqua">Hello</span>(<span class="green">"World"</span>));

<span class="comment">// Simple on purpose.</span></code></pre>
          <div class="code-footer">
            <span class="status-dot"></span> Less runtime magic. More
            compile-time clarity.
          </div>
        </div>
      </section>

      <section id="faq" class="faq-section wrap section-space">
        <div>
          <p class="eyebrow">A LITTLE MORE CONTEXT</p>
          <h2>Good questions.<br />Clear answers.</h2>
          <p>
            Something else on your mind?<br /><a
              class="text-link"
              :href="`${github}/issues`"
              target="_blank"
              rel="noopener noreferrer"
              >Start a conversation on GitHub ↗</a
            >
          </p>
        </div>
        <div class="faq-list">
          <details v-for="item in faqs" :key="item.q">
            <summary>{{ item.q }}<span aria-hidden="true">+</span></summary>
            <p>{{ item.a }}</p>
          </details>
        </div>
      </section>

      <section id="documentation" class="cta wrap">
        <span class="eyebrow">BUILD SOMETHING THAT FLOWS</span>
        <h2>Make room for your next idea.</h2>
        <p>Explore the guides and build your first request handler.</p>
        <button class="install-command" @click="copyCommand">
          <span aria-hidden="true">$</span><code>{{ command }}</code
          ><span class="copy-label" aria-live="polite">
            {{ copied ? "Copied" : "Copy" }}
            <svg class="copy-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path v-if="copied" d="m5 12 4 4L19 6" />
              <template v-else>
                <rect x="8" y="8" width="12" height="12" rx="2" />
                <path d="M16 8V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h2" />
              </template>
            </svg>
          </span>
        </button>
        <p v-if="copyError" class="copy-error" role="status">
          Select and copy the command above to install.
        </p>
        <span class="sr-only" role="status">{{
          copied ? "Install command copied to clipboard" : ""
        }}</span
        ><a
          class="text-link"
          href="#/docs/getting-started"
          target="_blank"
          rel="noopener noreferrer"
          >Read the quick start →</a
        >
      </section>
    </main>
    <footer class="footer wrap">
      <a class="brand" href="#/"
        ><img :src="logo" alt="" width="29" height="29" /><span
          >Swift<span class="brand-accent">Dispatch</span></span
        ></a
      ><span>Open source. Forward thinking.</span>
      <div>
        <a :href="github" target="_blank" rel="noopener noreferrer">GitHub ↗</a
        ><a href="#/docs/getting-started">Documentation</a
        ><a
          :href="`${github}/blob/master/LICENSE`"
          target="_blank"
          rel="noopener noreferrer"
          >MIT License</a
        >
      </div>
    </footer>
  </div>
</template>
