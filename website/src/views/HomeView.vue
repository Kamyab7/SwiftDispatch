<script setup lang="ts">
import { computed, onUnmounted, ref } from "vue";
import { emitConversion } from "../funnel";
import SiteHeader from "../components/SiteHeader.vue";
import SiteFooter from "../components/SiteFooter.vue";
const github = "https://github.com/TrueMediator/TrueMediator";
const command = "dotnet add package TrueMediator";
const copyState = ref("");
let copyTimer: ReturnType<typeof setTimeout> | undefined;
async function copyCommand() {
  try {
    await navigator.clipboard.writeText(command);
    copyState.value = "Copied to clipboard";
    emitConversion("install_copy", "hero");
    clearTimeout(copyTimer);
    copyTimer = setTimeout(() => {
      copyState.value = "";
    }, 2500);
  } catch {
    copyState.value = "Select and copy the command to install.";
  }
}
onUnmounted(() => clearTimeout(copyTimer));
const examples = [
  {
    title: "Requests",
    label: "Hello.cs",
    guide: "getting-started",
    description: "One request. One handler. Business logic with a clear home.",
    code: `// Define a request and its response.
public sealed record Hello(string Name) : IRequest<string>;

public sealed class HelloHandler : IRequestHandler<Hello, string>
{
    public Task<string> Handle(Hello request, CancellationToken ct)
        => Task.FromResult($"Hello, {request.Name}!");
}

// After registering TrueMediator:
var greeting = await mediator.Send(new Hello("Ada"));
// Hello, Ada!`,
  },
  {
    title: "Notifications",
    label: "OrderPlaced.cs",
    guide: "notifications",
    description: "Publish once. Let multiple handlers react independently.",
    code: `public sealed record OrderPlaced(string OrderId) : INotification;

public sealed class OrderAuditHandler
    : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken ct)
    {
        Console.WriteLine($"Order placed: {notification.OrderId}");
        return Task.CompletedTask;
    }
}

await mediator.Publish(new OrderPlaced("order-123"));`,
  },
  {
    title: "Streaming",
    label: "Count.cs",
    guide: "streaming",
    description: "Consume results as they arrive with IAsyncEnumerable<T>.",
    code: `public sealed record Count(int Limit) : IStreamRequest<int>;

// Implement IStreamRequestHandler<Count, int>
// to produce results as an IAsyncEnumerable<int>.

await foreach (int value in mediator.CreateStream(
    new Count(3), cancellationToken))
{
    Console.WriteLine(value);
}

// 1, 2, 3 — streamed from your handler.`,
  },
];
const activeExample = ref(0);
const example = computed(() => examples[activeExample.value]!);
const scenarios = [
  {
    title: "Completed request",
    rows: [
      ["TrueMediator", "25.11 ns", "136 B"],
      ["MediatR", "47.57 ns", "200 B"],
      ["Mediator", "29.20 ns", "88 B"],
    ],
  },
  {
    title: "Notification",
    rows: [
      ["TrueMediator", "54.70 ns", "184 B"],
      ["MediatR", "215.53 ns", "768 B"],
      ["Mediator", "36.72 ns", "56 B"],
    ],
  },
  {
    title: "Yielding async request",
    rows: [
      ["TrueMediator", "859.18 ns", "160 B"],
      ["MediatR", "888.31 ns", "224 B"],
      ["Mediator", "998.18 ns", "192 B"],
    ],
  },
];
const scenarioIndex = ref(0);
const scenario = computed(() => scenarios[scenarioIndex.value]!);
const faqs = [
  {
    q: "Is TrueMediator free for commercial use?",
    a: "Yes. TrueMediator is MIT licensed and free for personal and commercial projects. The source is available on GitHub.",
  },
  {
    q: "Is it a drop-in replacement for MediatR?",
    a: "It uses familiar interfaces, but migration requires namespace and registration changes. Review your integrations, supported features, and pipeline behavior before switching. The migration guide walks through those checks.",
  },
  {
    q: "Which .NET versions does it support?",
    a: "The library targets .NET Standard 2.0, .NET 8, .NET 10, and .NET 11. Native AOT is supported on .NET 8, .NET 10, and .NET 11. Building this repository requires the .NET 11 SDK.",
  },
  {
    q: "Do I install the source generator separately?",
    a: "No. The TrueMediator NuGet package includes the source generator automatically and references TrueMediator.Contracts.",
  },
  {
    q: "Are validation and retries built in?",
    a: "You implement these as pipeline behaviors and register them explicitly. TrueMediator composes the ordered pipeline; your behaviors supply the validation, logging, retry, or exception-handling logic.",
  },
];
</script>

<template>
  <div class="site-shell">
    <RouterLink class="docs-skip" to="/#main-content"
      >Skip to content</RouterLink
    >
    <SiteHeader />
    <main id="main-content">
      <section class="hero wrap">
        <div class="hero-copy">
          <p class="eyebrow">THE SOURCE-GENERATED .NET MEDIATOR</p>
          <h1>A .NET mediator.<br /><span>Ready for AOT.</span></h1>
          <p class="hero-lead">
            A lightweight MediatR alternative.<br />Built for Native AOT. Free
            under MIT.
          </p>
          <p class="hero-description">
            Connect requests to handlers, publish notifications, and compose
            pipelines with registration and dispatch generated at compile time.
          </p>
          <div class="hero-actions">
            <RouterLink class="button primary" to="/docs/getting-started"
              >Build your first handler
              <span aria-hidden="true">→</span></RouterLink
            ><RouterLink class="button secondary" to="/docs/migration"
              >Moving from MediatR?</RouterLink
            >
          </div>
          <div class="install-command">
            <code>{{ command }}</code
            ><button @click="copyCommand" aria-label="Copy install command">
              {{ copyState === "Copied to clipboard" ? "Copied ✓" : "Copy" }}
            </button>
          </div>
          <p class="copy-status" role="status">{{ copyState }}</p>
          <p class="hero-note">Source generator included. No separate setup.</p>
        </div>
        <div class="hero-proof">
          <div class="code-window">
            <div class="code-bar">
              <span class="file-dot"></span><span>Hello.cs</span
              ><span>C# / .NET</span>
            </div>
            <pre><code><span class="comment">// Your request.</span>
<span class="purple">public sealed record</span> <span class="aqua">Hello</span>(<span class="purple">string</span> Name)
    : <span class="aqua">IRequest</span>&lt;<span class="purple">string</span>&gt;;

<span class="comment">// Your handler.</span>
<span class="purple">public sealed class</span> <span class="aqua">HelloHandler</span>
    : <span class="aqua">IRequestHandler</span>&lt;Hello, <span class="purple">string</span>&gt;
{
    <span class="purple">public</span> Task&lt;<span class="purple">string</span>&gt; Handle(
        Hello request, CancellationToken ct)
        =&gt; Task.FromResult(
            <span class="green">$"Hello, {request.Name}!"</span>);
}

<span class="comment">// TrueMediator connects them.</span>
<span class="purple">await</span> mediator.<span class="aqua">Send</span>(<span class="purple">new</span> Hello(<span class="green">"Ada"</span>));</code></pre>
            <div class="code-footer">
              <span class="status-dot"></span> Hello, Ada!
            </div>
          </div>
          <RouterLink class="example-caption" to="/docs/getting-started"
            >See registration and the complete example
            <span>→</span></RouterLink
          >
        </div>
      </section>
      <div class="trust-strip wrap">
        <span>MADE FOR YOUR .NET STACK</span>
        <p>.NET Standard 2.0 <i>/</i> .NET 8 <i>/</i> .NET 10 <i>/</i> .NET 11</p>
        <a
          :href="`${github}/blob/master/LICENSE`"
          target="_blank"
          rel="noopener noreferrer"
          >MIT licensed ↗</a
        >
      </div>

      <section id="why-truemediator" class="wrap section-space">
        <div class="section-heading">
          <div>
            <p class="eyebrow">WHY TRUEMEDIATOR</p>
            <h2>Keep the pattern.<br />Simplify the plumbing.</h2>
          </div>
          <p>
            You want handlers that do the work.<br />And a mediator that fits
            how you deploy.
          </p>
        </div>
        <div class="feature-grid">
          <article class="feature">
            <span class="feature-number">01 / DEPLOY</span>
            <h3>Take your mediator<br />to Native AOT.</h3>
            <p>
              Source-generated registration and dispatch support trimming and
              Native AOT on .NET 8, .NET 10, and .NET 11.
            </p>
            <RouterLink class="text-link" to="/docs/evaluating"
              >Check the fit →</RouterLink
            >
          </article>
          <article class="feature">
            <span class="feature-number">02 / BUILD</span>
            <h3>Give business logic<br />a clear home.</h3>
            <p>
              Use requests and handlers for application logic. Compose logging,
              validation, and retries as ordered behaviors.
            </p>
            <RouterLink class="text-link" to="/docs/pipeline-behaviors"
              >Explore pipelines →</RouterLink
            >
          </article>
          <article class="feature">
            <span class="feature-number">03 / ADOPT</span>
            <h3>Familiar interfaces.<br />An open license.</h3>
            <p>
              Work with familiar mediator patterns under MIT. Start a new
              project or evaluate a migration with the source in reach.
            </p>
            <RouterLink class="text-link" to="/docs/migration"
              >Read the migration guide →</RouterLink
            >
          </article>
        </div>
      </section>

      <section id="examples" class="wrap section-space examples-section">
        <div class="section-heading">
          <div>
            <p class="eyebrow">SMALL API. USEFUL BUILDING BLOCKS.</p>
            <h2>Meet your next handler.</h2>
          </div>
          <p>
            Requests, events, or a stream of results.<br />Choose the
            interaction your feature needs.
          </p>
        </div>
        <div class="example-layout">
          <div class="example-options" role="group" aria-label="Code examples">
            <button
              v-for="(item, index) in examples"
              :key="item.title"
              :aria-pressed="activeExample === index"
              @click="activeExample = index"
            >
              <span class="example-index">0{{ index + 1 }}</span
              ><strong>{{ item.title }}</strong
              ><span>{{ item.description }}</span
              ><b aria-hidden="true">↗</b>
            </button>
          </div>
          <div class="code-window">
            <div class="code-bar">
              <span>{{ example.label }}</span
              ><RouterLink :to="`/docs/${example.guide}`"
                >Full guide →</RouterLink
              >
            </div>
            <pre
              tabindex="0"
              :aria-label="`${example.title} code example`"
            ><code>{{ example.code }}</code></pre>
            <div class="code-footer">
              Examples use TrueMediator.Contracts; register services before
              dispatch.
            </div>
          </div>
        </div>
      </section>

      <section id="benchmarks" class="wrap section-space benchmark-section">
        <div class="section-heading">
          <div>
            <p class="eyebrow">EVIDENCE YOU CAN INSPECT</p>
            <h2>Show the measurements.<br />Keep the context.</h2>
          </div>
          <p>
            Compare execution time and allocations.<br />Then measure your own
            application.
          </p>
        </div>
        <div class="benchmark-panel">
          <div class="benchmark-top">
            <div>
              <span class="feature-number">EXPLORATORY / .NET 10</span>
              <h3>{{ scenario.title }}</h3>
            </div>
            <label
              >Scenario<select v-model="scenarioIndex">
                <option
                  v-for="(item, index) in scenarios"
                  :key="item.title"
                  :value="index"
                >
                  {{ item.title }}
                </option>
              </select></label
            >
          </div>
          <div
            class="table-scroll"
            tabindex="0"
            aria-label="Benchmark comparison"
          >
            <table>
              <caption class="sr-only">
                {{
                  scenario.title
                }}: mean execution time and managed allocation per operation;
                lower is better.
              </caption>
              <thead>
                <tr>
                  <th scope="col">Library</th>
                  <th scope="col">Mean time ↓</th>
                  <th scope="col">Allocated / operation ↓</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="row in scenario.rows"
                  :key="row[0]"
                  :class="{ 'highlight-row': row[0] === 'TrueMediator' }"
                >
                  <th scope="row">{{ row[0] }}</th>
                  <td>{{ row[1] }}</td>
                  <td>{{ row[2] }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <p class="benchmark-note">
            Three measurement iterations on a shared development machine, with
            some build/test overlap. These results are exploratory, not
            publication-quality evidence. Async timing errors overlap; no
            reliable async speed winner is established.
          </p>
          <div class="benchmark-links">
            <a
              class="text-link"
              :href="`${github}/blob/master/benchmarks/results/short-run.md`"
              target="_blank"
              rel="noopener noreferrer"
              >Results, versions &amp; measurement errors ↗</a
            ><a
              class="text-link"
              :href="`${github}/blob/master/benchmarks/README.md`"
              target="_blank"
              rel="noopener noreferrer"
              >Reproduce the benchmarks ↗</a
            >
          </div>
        </div>
      </section>

      <section class="wrap section-space pipeline-section">
        <div class="section-heading">
          <div>
            <p class="eyebrow">CROSS-CUTTING CONCERNS, IN ORDER</p>
            <h2>Let your handler<br />focus on the feature.</h2>
          </div>
          <p>
            Register behaviors in the order you need.<br />Each wraps the next
            step in your pipeline.
          </p>
        </div>
        <div
          class="pipeline-flow"
          aria-label="Example custom pipeline: validation, logging, then handler"
        >
          <div>
            <span>01 / YOUR BEHAVIOR</span>
            <h3>Validation</h3>
            <p>Check the request.</p>
          </div>
          <b aria-hidden="true">→</b>
          <div>
            <span>02 / YOUR BEHAVIOR</span>
            <h3>Logging</h3>
            <p>Record the operation.</p>
          </div>
          <b aria-hidden="true">→</b>
          <div>
            <span>03 / YOUR HANDLER</span>
            <h3>Business logic</h3>
            <p>Do the actual work.</p>
          </div>
        </div>
        <div class="section-tail">
          <p>
            Illustrative behaviors you implement and register. Retries and
            exception handling can live here, too.
          </p>
          <RouterLink class="text-link" to="/docs/pipeline-behaviors"
            >Build your pipeline →</RouterLink
          >
        </div>
      </section>

      <section id="migration" class="wrap section-space">
        <div class="migration-panel">
          <div>
            <p class="eyebrow">ALREADY USING MEDIATR?</p>
            <h2>A familiar starting point.<br />A deliberate migration.</h2>
            <p>
              Keep the request-and-handler pattern. Review compatibility, update
              your configuration, and verify your application’s behavior.
            </p>
            <RouterLink class="button primary" to="/docs/migration"
              >Plan your migration <span aria-hidden="true">→</span></RouterLink
            >
          </div>
          <ol class="migration-steps">
            <li>
              <span>01</span>
              <div>
                <h3>Check your requirements</h3>
                <p>Review supported features and your MediatR integrations.</p>
              </div>
            </li>
            <li>
              <span>02</span>
              <div>
                <h3>Update namespaces and registration</h3>
                <p>Use TrueMediator.Contracts and AddTrueMediator.</p>
              </div>
            </li>
            <li>
              <span>03</span>
              <div>
                <h3>Verify your application</h3>
                <p>
                  Test pipelines, notifications, and cancellation before
                  rollout.
                </p>
              </div>
            </li>
          </ol>
        </div>
      </section>

      <section id="faq" class="wrap section-space faq-section">
        <div>
          <p class="eyebrow">BEFORE YOU COMMIT</p>
          <h2>A few things<br />worth knowing.</h2>
          <p>Evaluate the details before you adopt.</p>
          <RouterLink class="text-link" to="/docs/evaluating"
            >Read the adoption checklist →</RouterLink
          >
        </div>
        <div class="faq-list">
          <details v-for="item in faqs" :key="item.q">
            <summary>{{ item.q }}<span aria-hidden="true">+</span></summary>
            <p>{{ item.a }}</p>
          </details>
        </div>
      </section>

      <section class="cta wrap">
        <p class="eyebrow">FROM EVALUATION TO YOUR FIRST REQUEST</p>
        <h2>Put TrueMediator to work.</h2>
        <p>Start with one handler. See how it fits your application.</p>
        <div class="hero-actions">
          <RouterLink class="button primary" to="/docs/getting-started"
            >Follow the quick start →</RouterLink
          ><a
            class="button secondary"
            :href="`${github}/tree/master/samples/TrueMediator.Sample.CleanArchitecture`"
            target="_blank"
            rel="noopener noreferrer"
            >Explore a real sample ↗</a
          >
        </div>
        <a
          class="text-link cta-source"
          :href="github"
          target="_blank"
          rel="noopener noreferrer"
          >Inspect the source on GitHub ↗</a
        >
      </section>
    </main>
    <SiteFooter />
  </div>
</template>
