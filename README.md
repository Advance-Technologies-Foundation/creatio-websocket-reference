# Creatio WebSocket reference

This executable Creatio lab demonstrates the supported application-level WebSocket routes available to a
Freedom UI page:

- backend push to the authenticated user's connected browser;
- frontend `PTP` to every connected browser for the same user;
- frontend `BROADCAST` to every connected user.

It uses Creatio's existing message channel and does not introduce a custom WebSocket server or protocol.

## What the lab teaches

| Action | Flow | Purpose |
|---|---|---|
| **Backend push** | Frontend REST request → C# publisher → current user's channel → frontend subscription | Send transient backend notifications to one authenticated user. |
| **PTP to my user** | Frontend → `MessageChannelType.PTP` → same user's connections | Bridge tabs or browser sessions owned by the same user. |
| **Broadcast to all** | Frontend → `MessageChannelType.BROADCAST` → every active user channel | Demonstrate application-wide fan-out for low-trust messages. |

The lab also records an important negative result: although the frontend SDK exposes
`MessageChannelType.SERVER`, no supported public hook for receiving that route from an ordinary standalone
package was found on the tested Creatio 10.0.0.858 .NET 8 runtime. See
[Why there is no SERVER button](#why-there-is-no-server-button).

All three demonstrated routes are transient. They work only while the receiving browser is connected and
must not replace persisted state, durable queues, or background-job result storage.

## Repository map

| Path | Purpose |
|---|---|
| `packages/WebsocketLab/Files/src/cs/Messaging` | Backend publisher, JSON payload, and explicit delivery result. |
| `packages/WebsocketLab/Files/src/cs/EntryPoints/WebServices` | Thin current-user REST entry point. |
| `packages/WebsocketLab/Schemas/UsrWebsocketReference_Page` | Freedom UI page using the public message-channel API. |
| `tests/WebsocketLab` | NUnit, FluentAssertions, and NSubstitute unit tests. |
| `docs/lab-record.md` | Source evidence, failed experiments, and repeatable acceptance results. |

## Backend push pattern

The backend targets the authenticated system user, guards platform availability, and posts a JSON body with
a stable sender name:

```csharp
if (!MsgChannelManager.IsRunning) {
	return WebSocketPublishResult.NotDelivered("The Creatio message channel is not running.");
}

IMsgChannel channel;
try {
	channel = MsgChannelManager.Instance.FindItemByUId(userId);
	if (channel == null) {
		return WebSocketPublishResult.NotDelivered("The current user has no active browser channel.");
	}
} catch (Exception exception) {
	logger.Warn($"The WebSocket channel for user {userId} could not be resolved.", exception);
	return WebSocketPublishResult.NotDelivered("The Creatio message channel is unavailable.");
}

Guid eventId = Guid.NewGuid();
IMsg message = new SimpleMessage {
	Id = eventId,
	Body = JsonConvert.SerializeObject(payload)
};
message.Header.Sender = "WebsocketLab.Message";
try {
	channel.PostMessage(message);
} catch (Exception exception) {
	logger.Warn(
		$"WebSocket event {eventId} for user {userId} and sender WebsocketLab.Message was not posted because the channel closed.",
		exception);
	return WebSocketPublishResult.NotDelivered(
		"The message could not be posted to the active user channel.");
}
```

The page subscribes with the identical sender string:

```javascript
const channel = new sdk.MessageChannelService();
const subscription = await channel.subscribe("WebsocketLab.Message", async (event) => {
	await request.$context.set("UsrBackendPushResult", `Backend push: ${event.body.message}`);
});
```

`FindItemByUId` returns `null` when the user has no active browser channel. Treat that as expected
non-delivery. The body must be valid JSON because the modern frontend service parses string bodies before
invoking subscribers. The implementation logs event ID, user ID, sender, and exception for a disconnect
race, but never logs the payload.

## Frontend PTP and BROADCAST pattern

`MessageChannelService.sendMessage` accepts a logical sender, a JSON-compatible body, and a route:

```javascript
const channel = new sdk.MessageChannelService();

await channel.sendMessage(
	"WebsocketLab.Ptp",
	{ message, sentAtUtc: new Date().toISOString() },
	sdk.MessageChannelType.PTP
);

await channel.sendMessage(
	"WebsocketLab.Broadcast",
	{ message, sentAtUtc: new Date().toISOString() },
	sdk.MessageChannelType.BROADCAST
);
```

Subscribe separately to `WebsocketLab.Ptp` and `WebsocketLab.Broadcast`. PTP is user-scoped, not tab-scoped:
every connected browser channel for that user can receive it. BROADCAST is application-wide and can expose
the body to every connected user. The browser route has no package-owned server permission check, so use it
only when every authenticated user is allowed to send the message and the payload is safe for that audience.
For a trusted system announcement, call a backend endpoint that checks an operation permission and then
publishes through a backend broadcast primitive.

The page creates all subscriptions on `crt.HandleViewModelResumeRequest`, stores the pending subscription
promise to prevent duplicate callbacks during concurrent resume requests, and unsubscribes every handle on
`crt.HandleViewModelPauseRequest`. Do not use the legacy `Terrasoft.ServerChannel` API for new Freedom UI
pages.

## Why there is no SERVER button

Creatio's frontend SDK can send:

```javascript
await channel.sendMessage(sender, body, sdk.MessageChannelType.SERVER);
```

Platform source exposes inbound events through `IMsgServiceLayer.OnMsgChannelConnected` and
`IWebSocketServer.OnChannelMessage`, and Creatio's own internal features use those events. The missing piece
for application packages is a supported way to obtain either service:

- `ClassFactory.Get<IMsgServiceLayer>()` failed live on .NET 8 with no Ninject binding;
- `ClassFactory.Get<IMsgChannelManager>()` has the same limitation;
- `CoreApiContainer.Resolve<T>()` is an internal platform API and is not package-accessible;
- reflection or private-field access would be version-fragile and is intentionally excluded from this
  reference.

Therefore, this repository does not claim that ordinary application packages can handle frontend
`SERVER` messages. No supported acquisition path was found within the tested runtime and package boundary;
modern core-DI acquisition from a package remains unverified. A genuinely supported bidirectional package
flow remains REST-in plus WebSocket-out, which is what **Backend push** demonstrates. Add a SERVER example
only when Creatio publishes a public package extension point or a supported core binding for the receive
service.

## Build and unit-test

Prerequisites:

- a Creatio environment registered in clio;
- .NET 8 SDK;
- build references restored into the ignored `.application` directory.

```powershell
clio restorew -e <environment-name>
dotnet build MainSolution.slnx -c dev-n8
dotnet test tests/WebsocketLab/WebsocketLab.Tests.csproj -c dev-n8 --no-build
```

The focused suite verifies the backend sender, JSON correlation, authenticated-user targeting, validation,
offline/stopped-channel results, disconnect logging without payloads, and frontend PTP/BROADCAST sender,
route, subscription, and cleanup contracts.

## Load and run the lab

Follow the live clio guidance for the target environment's deployment mode. In file-system mode, direct
schema metadata changes must first be loaded into the database; then rebuild and restart:

```powershell
clio pkg-to-db -e <environment-name>
dotnet build MainSolution.slnx -c dev-n8
clio restart-web-app -e <environment-name> --wait-ready
```

The page is standalone and is not registered as a workplace section. Append the matching path to the
Creatio base URL:

| Creatio runtime | Page path |
|---|---|
| .NET Framework | `/0/Shell/#Section/UsrWebsocketReference_Page` |
| .NET 8 or .NET 10 | `/Shell/#Section/UsrWebsocketReference_Page` |

For example:

```text
https://example.creatio.com/Shell/#Section/UsrWebsocketReference_Page
```

To verify the routes:

1. Open the page in two tabs as the same user.
2. Choose **PTP to my user** and confirm both tabs update the PTP result.
3. Choose **Broadcast to all** and confirm every connected test session updates the broadcast result. Use a
   second user when proving the all-user boundary.
4. Choose **Backend push** and confirm the REST result and WebSocket result both succeed.

## Security and operational boundaries

- Never accept an arbitrary target user ID in a current-user endpoint; derive it from the authenticated
  `UserConnection`.
- Treat every payload as user-visible and avoid secrets or unrestricted business records.
- Frontend BROADCAST has no package-owned server permission check. Use it only for low-trust messages that
  every authenticated user may send; use a permission-checked backend endpoint for trusted announcements.
- Prefer HTTPS outside an isolated development environment.
- Keep payloads small and persist important results separately.
- Never commit credentials, browser sessions, `.application`, build output, or `TASK.md`.

## Relationship to Clio Knowledge

The reusable agent workflow is published by
[`clio-knowledge`](https://github.com/Advance-Technologies-Foundation/clio-knowledge). This repository is
the executable evidence behind that guidance. The guide owns reusable decisions and safety rules; this lab
preserves implementation, unit tests, live acceptance, and rejected approaches.

## License

The example source is licensed under the [MIT License](LICENSE). Creatio platform binaries and packages
remain subject to their own license and are not distributed by this repository.
