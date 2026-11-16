<script setup lang="ts">
import SiteHeader from "../components/SiteHeader.vue";
import SiteFooter from "../components/SiteFooter.vue";
</script>

<template>
  <div class="docs-site">
    <RouterLink class="docs-skip" to="/privacy#privacy-content"
      >Skip to content</RouterLink
    >
    <SiteHeader />
    <main
      id="privacy-content"
      class="privacy-content wrap markdown-body"
      tabindex="-1"
    >
      <p class="eyebrow">SWIFTDISPATCH / PRIVACY</p>
      <h1>Privacy policy</h1>
      <p>Last updated: <time datetime="2026-11-16">16 November 2026</time></p>
      <p>
        This policy describes the SwiftDispatch website maintained by the
        project's
        <a
          href="https://github.com/Kamyab7"
          target="_blank"
          rel="noopener noreferrer"
          >Kamyab7 maintainer</a
        >. It covers the current website, rather than applications that use the
        SwiftDispatch library.
      </p>

      <h2>Browsing this website</h2>
      <p>
        You can read the site without creating an account or submitting personal
        information. The website has no contact forms, newsletter signup,
        advertising pixels, or connected analytics service. Fonts, images, and
        application assets are served with the website; it does not load fonts
        from a third-party font service.
      </p>

      <h2>Hosting and security logs</h2>
      <p>
        The repository is configured to publish this site through GitHub Pages.
        GitHub states that it logs and stores visitors' IP addresses for
        security purposes, including visits made without signing in. See
        <a
          href="https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages"
          target="_blank"
          rel="noopener noreferrer"
          >GitHub's Pages documentation</a
        >
        and
        <a
          href="https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement"
          target="_blank"
          rel="noopener noreferrer"
          >GitHub's privacy statement</a
        >
        for its data handling, retention, international processing, and privacy
        contact options. This website does not configure a separate retention
        period for GitHub's logs.
      </p>
      <p>
        If you are viewing a local development preview, requests go to that
        development server instead of GitHub Pages.
      </p>

      <h2>Cookies, storage, and local interaction events</h2>
      <p>
        The website's application code does not set cookies or store visitor
        identifiers in local storage or session storage.
      </p>
      <p>
        Some links and the install-command copy button trigger an event inside
        your browser. These events describe the action, where it occurred on the
        page, and, for recognized links, the destination without query
        parameters. No analytics collector is connected: the application does
        not send these events to a server or persist them across visits.
      </p>
      <p>
        The copy button writes the displayed installation command to your
        clipboard when you click it. It does not read your clipboard.
      </p>

      <h2>External links and contacting the maintainer</h2>
      <p>
        Links to GitHub and NuGet take you to separate services with their own
        privacy policies. Information you submit there, such as a GitHub issue,
        is handled by those services and may be public. Avoid posting private
        information in public issues.
      </p>
      <p>
        For privacy questions, contact the maintainer at
        <a
          href="mailto:valipourkamyab.official@gmail.com?subject=SwiftDispatch%20privacy"
          >valipourkamyab.official@gmail.com</a
        >, the project's published contact address. Emailing the maintainer
        shares your address and message through the email services involved.
        Correspondence is used to handle your request; this site has no
        mailing-list integration or automated marketing-email collection.
      </p>

      <h2>Your privacy rights</h2>
      <p>
        Where applicable data-protection law provides them, you may request
        access, correction, deletion, restriction, or portability of your
        personal data, or object to processing. You may also complain to your
        data-protection authority. Contact the maintainer about data they
        handle, and GitHub about data GitHub handles. The local interaction
        events described above do not create a stored visitor profile to
        retrieve or delete.
      </p>

      <h2>Changes to this policy</h2>
      <p>
        This policy will need updating if the website adds analytics collection,
        forms, email subscriptions, or other data processing. The date above
        identifies this version.
      </p>
      <RouterLink class="text-link" to="/">← Back to SwiftDispatch</RouterLink>
    </main>
    <SiteFooter />
  </div>
</template>
