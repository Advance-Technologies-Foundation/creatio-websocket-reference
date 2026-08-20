using System;

namespace WebsocketLabApp.Messaging {

	internal interface IWebSocketMessagePublisher {

		WebSocketPublishResult Publish(Guid userId, WebSocketNotification notification);
	}
}
