define("UsrWebsocketReference_Page", /**SCHEMA_DEPS*/["@creatio-devkit/common"]/**SCHEMA_DEPS*/, function/**SCHEMA_ARGS*/(sdk)/**SCHEMA_ARGS*/ {
	const senders = {
		backend: "WebsocketLab.Message",
		ptp: "WebsocketLab.Ptp",
		broadcast: "WebsocketLab.Broadcast"
	};

	const getMessage = async (context) => (await context.UsrWebSocketMessage || "").trim();

	const sendFrontendMessage = async (request, sender, channelType, sentStatus) => {
		const message = await getMessage(request.$context);
		if (!message) {
			await request.$context.set("UsrWebSocketStatus", "Enter a message before sending.");
			return;
		}
		try {
			const messageChannel = new sdk.MessageChannelService();
			await request.$context.set("UsrWebSocketStatus", sentStatus);
			await messageChannel.sendMessage(sender, {
				message,
				sentAtUtc: new Date().toISOString()
			}, channelType);
		} catch (error) {
			await request.$context.set("UsrWebSocketStatus", "The frontend WebSocket send failed.");
		}
	};

	const subscribe = (messageChannel, sender, context, resultAttribute, routeName) =>
		messageChannel.subscribe(sender, async (event) => {
			const body = event.body || {};
			await context.set(
				resultAttribute,
				`${routeName}: ${body.message || "Received a message without a message field."}`
			);
			await context.set(
				"UsrWebSocketStatus",
				`${routeName} received event ${event.id || "unknown"} at ${body.sentAtUtc || "an unknown time"}.`
			);
		});

	const unsubscribeAll = (subscriptions) => (subscriptions || []).forEach(subscription => {
		try {
			subscription?.unsubscribe();
		} catch (error) {
			// Continue releasing the remaining handles during lifecycle cleanup.
		}
	});

	const subscribeAll = async (messageChannel, context) => {
		const results = await Promise.allSettled([
			subscribe(messageChannel, senders.backend, context, "UsrBackendPushResult", "Backend push"),
			subscribe(messageChannel, senders.ptp, context, "UsrPtpResult", "PTP"),
			subscribe(messageChannel, senders.broadcast, context, "UsrBroadcastResult", "Broadcast")
		]);
		const subscriptions = results
			.filter(result => result.status === "fulfilled")
			.map(result => result.value);
		const failure = results.find(result => result.status === "rejected");
		if (failure) {
			unsubscribeAll(subscriptions);
			throw failure.reason;
		}
		return subscriptions;
	};

	return {
		viewConfigDiff: /**SCHEMA_VIEW_CONFIG_DIFF*/[
			{
				"operation": "insert",
				"name": "WebSocketLabContainer",
				"values": {
					"type": "crt.FlexContainer",
					"direction": "column",
					"gap": "medium",
					"padding": "large",
					"alignItems": "stretch",
					"items": [],
					"fitContent": true
				},
				"parentName": "MainContainer",
				"propertyName": "items",
				"index": 0
			},
			{
				"operation": "insert",
				"name": "WebSocketLabTitle",
				"values": {
					"type": "crt.Label",
					"caption": "#ResourceString(WebSocketLabTitle_caption)#",
					"labelType": "headline-1",
					"headingLevel": "h1"
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 0
			},
			{
				"operation": "insert",
				"name": "WebSocketLabDescription",
				"values": {
					"type": "crt.Label",
					"caption": "#ResourceString(WebSocketLabDescription_caption)#",
					"labelType": "body",
					"headingLevel": "label"
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 1
			},
			{
				"operation": "insert",
				"name": "WebSocketMessageInput",
				"values": {
					"type": "crt.Input",
					"label": "#ResourceString(WebSocketMessageInput_label)#",
					"placeholder": "#ResourceString(WebSocketMessageInput_placeholder)#",
					"control": "$UsrWebSocketMessage",
					"readonly": false,
					"multiline": false,
					"labelPosition": "above"
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 2
			},
			{
				"operation": "insert",
				"name": "WebSocketActions",
				"values": {
					"type": "crt.FlexContainer",
					"direction": "row",
					"gap": "small",
					"alignItems": "center",
					"items": [],
					"fitContent": true
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 3
			},
			{
				"operation": "insert",
				"name": "SendBackendPushButton",
				"values": {
					"type": "crt.Button",
					"caption": "#ResourceString(SendBackendPushButton_caption)#",
					"color": "primary",
					"size": "large",
					"clicked": { "request": "usr.SendBackendPushRequest" }
				},
				"parentName": "WebSocketActions",
				"propertyName": "items",
				"index": 0
			},
			{
				"operation": "insert",
				"name": "SendPtpButton",
				"values": {
					"type": "crt.Button",
					"caption": "#ResourceString(SendPtpButton_caption)#",
					"size": "large",
					"clicked": { "request": "usr.SendPtpRequest" }
				},
				"parentName": "WebSocketActions",
				"propertyName": "items",
				"index": 1
			},
			{
				"operation": "insert",
				"name": "SendBroadcastButton",
				"values": {
					"type": "crt.Button",
					"caption": "#ResourceString(SendBroadcastButton_caption)#",
					"size": "large",
					"clicked": { "request": "usr.SendBroadcastRequest" }
				},
				"parentName": "WebSocketActions",
				"propertyName": "items",
				"index": 2
			},
			{
				"operation": "insert",
				"name": "WebSocketStatusLabel",
				"values": {
					"type": "crt.Label",
					"caption": "$UsrWebSocketStatus",
					"labelType": "body",
					"headingLevel": "label"
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 4
			},
			{
				"operation": "insert",
				"name": "WebSocketResultsTitle",
				"values": {
					"type": "crt.Label",
					"caption": "#ResourceString(WebSocketResultsTitle_caption)#",
					"labelType": "headline-3",
					"headingLevel": "h2"
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 5
			},
			{
				"operation": "insert",
				"name": "BackendPushResult",
				"values": { "type": "crt.Label", "caption": "$UsrBackendPushResult", "labelType": "body", "headingLevel": "label" },
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 6
			},
			{
				"operation": "insert",
				"name": "PtpResult",
				"values": { "type": "crt.Label", "caption": "$UsrPtpResult", "labelType": "body", "headingLevel": "label" },
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 7
			},
			{
				"operation": "insert",
				"name": "BroadcastResult",
				"values": { "type": "crt.Label", "caption": "$UsrBroadcastResult", "labelType": "body", "headingLevel": "label" },
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 8
			}
		]/**SCHEMA_VIEW_CONFIG_DIFF*/,
		viewModelConfigDiff: /**SCHEMA_VIEW_MODEL_CONFIG_DIFF*/[
			{
				"operation": "merge",
				"path": [],
				"values": {
					"attributes": {
						"UsrWebSocketMessage": { "value": "Hello through Creatio WebSockets" },
						"UsrWebSocketStatus": { "value": "#ResourceString(WebSocketWaitingStatus_caption)#" },
						"UsrBackendPushResult": { "value": "#ResourceString(BackendPushNoMessage_caption)#" },
						"UsrPtpResult": { "value": "#ResourceString(PtpNoMessage_caption)#" },
						"UsrBroadcastResult": { "value": "#ResourceString(BroadcastNoMessage_caption)#" }
					}
				}
			}
		]/**SCHEMA_VIEW_MODEL_CONFIG_DIFF*/,
		modelConfigDiff: /**SCHEMA_MODEL_CONFIG_DIFF*/[]/**SCHEMA_MODEL_CONFIG_DIFF*/,
		handlers: /**SCHEMA_HANDLERS*/[
			{
				request: "crt.HandleViewModelResumeRequest",
				handler: async (request, next) => {
					await next?.handle(request);
					if (request.$context.UsrWebSocketSubscriptions || request.$context.UsrWebSocketSubscriptionsPending) {
						return;
					}
					const messageChannel = new sdk.MessageChannelService();
					const pendingSubscriptions = subscribeAll(messageChannel, request.$context);
					request.$context.UsrWebSocketSubscriptionsPending = pendingSubscriptions;
					try {
						const subscriptions = await pendingSubscriptions;
						if (request.$context.UsrWebSocketSubscriptionsPending !== pendingSubscriptions) {
							return;
						}
						request.$context.UsrWebSocketSubscriptionsPending = null;
						request.$context.UsrWebSocketSubscriptions = subscriptions;
					} catch (error) {
						if (request.$context.UsrWebSocketSubscriptionsPending === pendingSubscriptions) {
							request.$context.UsrWebSocketSubscriptionsPending = null;
							await request.$context.set("UsrWebSocketStatus", "The WebSocket subscriptions could not be established.");
						}
					}
				}
			},
			{
				request: "crt.HandleViewModelPauseRequest",
				handler: async (request, next) => {
					unsubscribeAll(request.$context.UsrWebSocketSubscriptions);
					request.$context.UsrWebSocketSubscriptions = null;
					const pendingSubscriptions = request.$context.UsrWebSocketSubscriptionsPending;
					if (pendingSubscriptions) {
						request.$context.UsrWebSocketSubscriptionsPending = null;
						try {
							unsubscribeAll(await pendingSubscriptions);
						} catch (error) {
							// subscribeAll already releases every successful partial subscription.
						}
					}
					return next?.handle(request);
				}
			},
			{
				request: "usr.SendBackendPushRequest",
				handler: async (request, next) => {
					const message = await getMessage(request.$context);
					if (!message) {
						await request.$context.set("UsrWebSocketStatus", "Enter a message before sending.");
						return;
					}
					try {
						const httpClient = new sdk.HttpClientService();
						const response = await httpClient.post("/rest/WebSocketReferenceService/SendToCurrentUser", { message });
						const result = response.body || {};
						await request.$context.set("UsrWebSocketStatus", result.message || (response.ok ? "Backend push requested." : "Backend push failed."));
					} catch (error) {
						await request.$context.set("UsrWebSocketStatus", "The backend request failed.");
					}
					return next?.handle(request);
				}
			},
			{
				request: "usr.SendPtpRequest",
				handler: async (request, next) => {
					await sendFrontendMessage(request, senders.ptp, sdk.MessageChannelType.PTP, "PTP sent to every browser connection for the current user.");
					return next?.handle(request);
				}
			},
			{
				request: "usr.SendBroadcastRequest",
				handler: async (request, next) => {
					await sendFrontendMessage(request, senders.broadcast, sdk.MessageChannelType.BROADCAST, "Broadcast sent to all connected users.");
					return next?.handle(request);
				}
			}
		]/**SCHEMA_HANDLERS*/,
		converters: /**SCHEMA_CONVERTERS*/{}/**SCHEMA_CONVERTERS*/,
		validators: /**SCHEMA_VALIDATORS*/{}/**SCHEMA_VALIDATORS*/
	};
});
