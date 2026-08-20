using System;

namespace WebsocketLabApp.Messaging {

	internal sealed class WebSocketPublishResult {

		private WebSocketPublishResult(bool success, Guid? eventId, string message) {
			Success = success;
			EventId = eventId;
			Message = message;
		}

		public Guid? EventId { get; }

		public string Message { get; }

		public bool Success { get; }

		public static WebSocketPublishResult Delivered(Guid eventId) =>
			new WebSocketPublishResult(true, eventId, "Message posted to the active user channel.");

		public static WebSocketPublishResult NotDelivered(string message) =>
			new WebSocketPublishResult(false, null, message);
	}
}
