# Proposal: configured dispatch catalog

Developers following mediator-based code have difficulty locating handlers and understanding the pipeline surrounding a request. SwiftDispatch already validates those relationships at build time, so an opt-in generated catalog can expose them without runtime scanning.

Enable with `EnableDispatchCatalog(true)`. Resolve the singleton `SwiftDispatch.Diagnostics.IDispatchCatalog` to inspect immutable message, response, behavior, handler, and notification-strategy types. Open `SwiftDispatch.DispatchCatalog.g.cs` to inspect readable flows with available source locations. The catalog uses the same validated model as generated dispatch.

The first release covers configured request, void-request, stream, and notification flows. It excludes IDE extensions, call-site analysis, web endpoints, and runtime tracing. Metadata does not promise an actual execution path: short-circuiting, retries, nested dispatch, and DI overrides remain runtime concerns.

Alternatives include colocating requests and handlers, searching for request references, and adding logging behaviors. These help individual navigation or execution diagnosis but do not provide a complete configured pipeline view.

Compatibility: disabled by default; no reflection-based discovery; supported across .NET Standard 2.0 and .NET 8/10/11, including Native AOT. Source paths remain generated comments, not runtime values. Catalog ownership follows existing first-registration dispatch-table semantics.

Validation includes generator diagnostics and compilation, deterministic output, runtime order and immutability, dependency-free catalog lookup, and package/Native AOT smoke checks. This document is a local proposal for review before public submission; no issue or pull request has been published.
