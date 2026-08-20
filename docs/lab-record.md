# WebSocket reference lab record

This record preserves the evidence used to derive the reusable backend-to-frontend guidance. The public
guide should contain the resulting decisions, not this investigation chronology.

## Verification context

| Item | Observed value |
|---|---|
| Creatio | 10.0.0.858 |
| Runtime | .NET 8.0.30 |
| Database | PostgreSQL |
| clio | 8.1.0.107 |
| UI API | `@creatio-devkit/common` `MessageChannelService` |
| Lab package | `WebsocketLab` |
| Page | `UsrWebsocketReference_Page` |

The target was a disposable local Creatio development environment in file-system mode. Environment names,
credentials, cookies, and machine-specific source paths are intentionally omitted.

## Questions and evidence

| Question | Observation | Durable decision | Status |
|---|---|---|---|
| How does backend code address one browser user? | `IMsgChannelManager.FindItemByUId` uses the system-user ID. The local server stores a composite channel per user. | Use `UserConnection.CurrentUser.Id`; do not accept a caller-supplied target in this current-user example. | Verified and executable |
| What routes an event to frontend code? | `SimpleMessage.Header.Sender` is matched exactly by `MessageChannelService.subscribe(sender, callback)`. | Define one stable sender constant and use the identical string on both sides. | Verified and executable |
| What shape reaches the callback? | A string `Body` is JSON-parsed into `event.body`; malformed JSON is dropped by the frontend service. | Serialize a defined JSON DTO into `SimpleMessage.Body`. | Verified in source and executable payload |
| What happens when the browser is offline? | `FindItemByUId` returns `null`. | Return an expected non-delivery result; never dereference the missing channel. | Verified by unit test |
| Is delivery durable? | Channels represent currently connected sessions and no backlog is created by `PostMessage`. | Document the mechanism as transient notification, not a queue. | Verified in platform source and live behavior |
| How is subscription lifetime managed? | Freedom UI exposes paired resume/pause and init/destroy lifecycle requests. | Subscribe once on resume and unsubscribe on pause for a suspendable page. | Verified and executable |
| Does one user with multiple connections receive the message? | The local server exposes a composite per-user channel over the user's physical connections. | Treat targeting as user-scoped, not tab-scoped. | Verified in platform source; not independently exercised with two tabs |
| Does cluster mode preserve the same contract? | The platform cluster implementation uses its messaging service layer while retaining the user-keyed channel manager contract. | Use the same application API; do not implement custom Redis behavior in application code. | Source-verified; live lab used one node |

## Failed experiments that changed the implementation

### Page schema name rejected

- Input: create `WebsocketReference_Page` in a package whose maintainer is `Customer`.
- Failure: Creatio required the `Usr` prefix.
- Correction: create `UsrWebsocketReference_Page`.
- Guidance effect: schema naming remains subject to the target maintainer rules and is not a WebSocket rule.

### Resolving the manager through the legacy factory failed

- Input: when `MsgChannelManager.IsRunning` was true, call
  `ClassFactory.Get<IMsgChannelManager>()` from the standalone package.
- Live failure: `InstanceActivationException` with no Ninject binding for `IMsgChannelManager`; the REST call
  returned HTTP 400 before publication.
- Root cause: the current .NET 8 platform registers the interface in its modern service collection, but the
  legacy `ClassFactory` path used by the package did not expose that binding.
- Correction: use the platform singleton `MsgChannelManager.Instance`, guarded by `IsRunning`, matching core
  platform implementations.
- Guidance effect: obtain the running manager from `MsgChannelManager.Instance`; do not assume the interface
  is resolvable from `ClassFactory`.

## Focused automated acceptance

Command:

```powershell
dotnet test tests/WebsocketLab/WebsocketLab.Tests.csproj -c dev-n8 --no-build
```

Result: 9 passed, 0 failed.

Covered behaviors:

- exact sender and generated event ID;
- JSON message and correlation ID;
- current-user channel selection;
- missing channel result;
- stopped manager result;
- disconnect between channel lookup and message posting;
- request normalization and validation;
- 1,000-character REST payload boundary;
- service mapping of a transient non-delivery result.
- frontend sender parity, modern SDK usage, and paired resume/pause cleanup.

## Live end-to-end acceptance

The browser was opened with a clio-generated authenticated storage state. The test used ordinary input and
button interaction on the real Freedom UI page.

Observed happy path:

```json
{
  "httpStatus": 200,
  "response": {
    "correlationId": "<non-empty-guid>",
    "eventId": "<non-empty-guid>",
    "message": "Message posted to the active user channel.",
    "success": true
  },
  "receivedTextMatched": true
}
```

Independent signals agreed:

1. REST returned 200 and a successful concrete response.
2. The server produced an event ID.
3. The visible page received and displayed the exact message from `event.body.message`.
4. The browser console recorded no new errors during the successful run.

Exploratory checks:

- whitespace-only input displayed client validation and made zero service calls;
- repeated sends replaced the displayed received value without console errors;
- after navigate-away/navigate-back cycles, instrumentation observed one subscription per resume and one
  unsubscribe per pause; the pending-subscription guard prevented concurrent resume requests from leaking a
  second callback;
- 1600×900 and 1024×768 viewports had no horizontal or vertical document overflow;
- the title, input, primary action, acknowledgement, and received value remained visible in both viewports.

Screenshots: [1600×900](images/websocket-live-proof.png) and
[1024×768](images/websocket-live-proof-1024.png).

## Known boundaries

- Verified live on one .NET 8 node and one authenticated user.
- Multiple physical channels for one user and clustered transport were explained by platform source, not
  independently exercised in this lab.
- Broadcast via `PostToAll` was intentionally not implemented because the requested outcome is current-user
  backend-to-frontend messaging and broadcast broadens the data-exposure boundary.
- Offline replay, ordering across reconnects, acknowledgement from the browser, and durable processing are
  unsupported by this mechanism. Persist important state separately.
