define("UsrWebsocketReference_Page", /**SCHEMA_DEPS*/["@creatio-devkit/common"]/**SCHEMA_DEPS*/, function/**SCHEMA_ARGS*/(sdk)/**SCHEMA_ARGS*/ {
	const senderName = "WebsocketLab.Message";
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
				"name": "SendWebSocketMessageButton",
				"values": {
					"type": "crt.Button",
					"caption": "#ResourceString(SendWebSocketMessageButton_caption)#",
					"color": "primary",
					"size": "large",
					"clicked": {
						"request": "usr.SendWebSocketMessageRequest"
					}
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 3
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
				"name": "WebSocketReceivedLabel",
				"values": {
					"type": "crt.Label",
					"caption": "$UsrWebSocketReceivedMessage",
					"labelType": "headline-3",
					"headingLevel": "h2"
				},
				"parentName": "WebSocketLabContainer",
				"propertyName": "items",
				"index": 5
			}
		]/**SCHEMA_VIEW_CONFIG_DIFF*/,
		viewModelConfigDiff: /**SCHEMA_VIEW_MODEL_CONFIG_DIFF*/[
			{
				"operation": "merge",
				"path": [],
				"values": {
					"attributes": {
						"UsrWebSocketMessage": {
							"value": "Hello from the Creatio backend"
						},
						"UsrWebSocketStatus": {
							"value": "#ResourceString(WebSocketWaitingStatus_caption)#"
						},
						"UsrWebSocketReceivedMessage": {
							"value": "#ResourceString(WebSocketNoMessage_caption)#"
						}
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
					if (request.$context.UsrWebSocketSubscription ||
						request.$context.UsrWebSocketSubscriptionPending) {
						return;
					}
					const messageChannel = new sdk.MessageChannelService();
					const pendingSubscription = messageChannel.subscribe(
						senderName,
						async (event) => {
							const body = event.body || {};
							await request.$context.set(
								"UsrWebSocketReceivedMessage",
								body.message || "Received a message without a message field."
							);
							await request.$context.set(
								"UsrWebSocketStatus",
								`Received ${event.id} at ${body.sentAtUtc || "an unknown time"}.`
							);
						}
					);
					request.$context.UsrWebSocketSubscriptionPending = pendingSubscription;
					const subscription = await pendingSubscription;
					if (request.$context.UsrWebSocketSubscriptionPending !== pendingSubscription) {
						return;
					}
					request.$context.UsrWebSocketSubscriptionPending = null;
					request.$context.UsrWebSocketSubscription = subscription;
				}
			},
			{
				request: "crt.HandleViewModelPauseRequest",
				handler: async (request, next) => {
					if (request.$context.UsrWebSocketSubscription) {
						request.$context.UsrWebSocketSubscription.unsubscribe();
						request.$context.UsrWebSocketSubscription = null;
					}
					const pendingSubscription = request.$context.UsrWebSocketSubscriptionPending;
					if (pendingSubscription) {
						request.$context.UsrWebSocketSubscriptionPending = null;
						const subscription = await pendingSubscription;
						subscription.unsubscribe();
					}
					return next?.handle(request);
				}
			},
			{
				request: "usr.SendWebSocketMessageRequest",
				handler: async (request, next) => {
					const message = (await request.$context.UsrWebSocketMessage || "").trim();
					if (!message) {
						await request.$context.set("UsrWebSocketStatus", "Enter a message before sending.");
						return;
					}
					try {
						const httpClient = new sdk.HttpClientService();
						const response = await httpClient.post(
							"/rest/WebSocketReferenceService/SendToCurrentUser",
							{ message }
						);
						const result = response.body || {};
						await request.$context.set(
							"UsrWebSocketStatus",
							result.message || (response.ok ? "Request completed." : "Request failed.")
						);
					} catch (error) {
						await request.$context.set("UsrWebSocketStatus", "The backend request failed.");
					}
					return next?.handle(request);
				}
			}
		]/**SCHEMA_HANDLERS*/,
		converters: /**SCHEMA_CONVERTERS*/{}/**SCHEMA_CONVERTERS*/,
		validators: /**SCHEMA_VALIDATORS*/{}/**SCHEMA_VALIDATORS*/
	};
});
