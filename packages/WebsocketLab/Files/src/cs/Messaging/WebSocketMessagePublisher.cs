using System;
using Common.Logging;
using Newtonsoft.Json;
using Terrasoft.Messaging.Common;

namespace WebsocketLabApp.Messaging {

	internal sealed class WebSocketMessagePublisher : IWebSocketMessagePublisher {

		private readonly Func<IMsgChannelManager> _channelManagerAccessor;
		private readonly ILog _logger;

		public WebSocketMessagePublisher(Func<IMsgChannelManager> channelManagerAccessor, ILog logger) {
			_channelManagerAccessor = channelManagerAccessor;
			_logger = logger;
		}

		public WebSocketPublishResult Publish(Guid userId, WebSocketNotification notification) {
			IMsgChannel channel;
			try {
				IMsgChannelManager channelManager = _channelManagerAccessor();
				if (channelManager == null) {
					return WebSocketPublishResult.NotDelivered("The Creatio message channel is not running.");
				}
				channel = channelManager.FindItemByUId(userId);
				if (channel == null) {
					return WebSocketPublishResult.NotDelivered("The current user has no active browser channel.");
				}
			} catch (Exception exception) {
				_logger.Warn($"The WebSocket channel for user {userId} could not be resolved.", exception);
				return WebSocketPublishResult.NotDelivered("The Creatio message channel is unavailable.");
			}

			Guid eventId = Guid.NewGuid();
			IMsg message = new SimpleMessage {
				Id = eventId,
				Body = JsonConvert.SerializeObject(notification)
			};
			message.Header.Sender = Constants.WebSocketSenderName;
			try {
				channel.PostMessage(message);
			} catch (Exception exception) {
				_logger.Warn(
					$"WebSocket event {eventId} for user {userId} and sender {Constants.WebSocketSenderName} was not posted because the channel closed.",
					exception);
				return WebSocketPublishResult.NotDelivered(
					"The message could not be posted to the active user channel.");
			}
			return WebSocketPublishResult.Delivered(eventId);
		}
	}
}
