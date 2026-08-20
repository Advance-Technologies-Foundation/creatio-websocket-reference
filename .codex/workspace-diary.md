## 2026-08-20 - Backend-to-frontend WebSocket reference
Context: Build an executable Creatio lab and evidence base for reusable clio guidance.
Decision: Target only the authenticated current user, publish a JSON `SimpleMessage` through the guarded platform singleton, and subscribe with the modern Freedom UI SDK on resume/pause.
Discovery: `ClassFactory.Get<IMsgChannelManager>()` had no live Ninject binding on .NET 8; `MsgChannelManager.Instance` is the supported running singleton used by platform code. Missing user channels are expected transient outcomes.
Files: packages/WebsocketLab/Files/src/cs/Messaging, packages/WebsocketLab/Files/src/cs/EntryPoints/WebServices, packages/WebsocketLab/Schemas/UsrWebsocketReference_Page, tests/WebsocketLab, docs/lab-record.md
Impact: Future agents can reproduce the implementation, unit tests, live acceptance, and known transport boundaries without relying on private source access.

## 2026-08-20 - Independent review and lifecycle race proof
Context: Validate the completed lab with Claude and repeat the acceptance at the real Freedom UI lifecycle boundary.
Decision: Guard both backend posting and frontend subscription creation as transient races; keep one pending subscription promise across concurrent resume requests.
Discovery: Creatio can issue two resume requests before the first subscription resolves. Without a pending guard, both subscribe and only one handle is later released. Instrumented navigate-away/back acceptance now observes one subscribe per resume and one unsubscribe per pause.
Files: packages/WebsocketLab/Files/src/cs/Messaging/WebSocketMessagePublisher.cs, packages/WebsocketLab/Schemas/UsrWebsocketReference_Page/UsrWebsocketReference_Page.js, tests/WebsocketLab, docs/lab-record.md
Impact: The reference teaches lifecycle-safe use of the platform primitive without introducing a custom WebSocket protocol or durable messaging layer.

## 2026-08-20 15:04 – Frontend PTP and BROADCAST proof; SERVER boundary
Context: Extend the lab beyond backend push and validate whether frontend SERVER messages can be handled by ordinary package code.
Decision: Add the supported PTP same-user bridge and BROADCAST announcement examples; reject a SERVER handler that would require reflection or another internal-platform dependency.
Discovery: PTP and BROADCAST both reached two connected tabs live on the approved lab environment. `ClassFactory.Get<IMsgServiceLayer>()` failed with no Ninject binding, while Creatio's own listeners rely on internal core DI. Claude independently found no supported package-accessible receive path in the repository evidence.
Files: packages/WebsocketLab/Schemas/UsrWebsocketReference_Page, packages/WebsocketLab/Files/src/cs/Messaging/WebSocketMessagePublisher.cs, tests/WebsocketLab, README.md, docs/lab-record.md
Impact: Future agents can demonstrate the two frontend-originated routes without overstating SERVER support, and can recognize the missing public extension point as a platform boundary.

## 2026-08-20 15:38 – Review hardening for frontend routes
Context: Claude's requested pre-push review challenged the three-route page, its evidence, and the BROADCAST security wording.
Decision: Make multi-subscription setup all-settled and failure-cleaning, keep designer-managed view config declarative, remove stale screenshot citations, and describe browser BROADCAST as low-trust rather than permission-enforced.
Discovery: A rejected member of `Promise.all` can strand the pending lifecycle guard and leak successful sibling subscriptions; browser-originated BROADCAST has no package-owned backend permission boundary.
Files: packages/WebsocketLab/Schemas/UsrWebsocketReference_Page/UsrWebsocketReference_Page.js, tests/WebsocketLab, README.md, docs/lab-record.md
Impact: The page can recover from partial subscription failure, designer round-trips retain its result labels, and readers are not taught to trust a client-only announcement route.
