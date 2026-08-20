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
