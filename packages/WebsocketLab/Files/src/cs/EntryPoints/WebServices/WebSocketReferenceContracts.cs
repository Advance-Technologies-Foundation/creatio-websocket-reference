using System;
using System.Runtime.Serialization;

namespace WebsocketLabApp.EntryPoints.WebServices {

	/// <summary>
	/// Defines the request accepted by <see cref="WebSocketReferenceService"/>.
	/// </summary>
	[DataContract]
	public sealed class SendWebSocketMessageRequest {

		/// <summary>
		/// Gets or sets the text delivered to the current user's browser.
		/// </summary>
		[DataMember(Name = "message")]
		public string Message { get; set; }
	}

	/// <summary>
	/// Describes whether Creatio posted the message to an active browser channel.
	/// </summary>
	[DataContract]
	public sealed class SendWebSocketMessageResponse {

		/// <summary>
		/// Gets or sets the correlation identifier included in the browser payload.
		/// </summary>
		[DataMember(Name = "correlationId")]
		public Guid CorrelationId { get; set; }

		/// <summary>
		/// Gets or sets the WebSocket event identifier.
		/// </summary>
		[DataMember(Name = "eventId")]
		public Guid? EventId { get; set; }

		/// <summary>
		/// Gets or sets a human-readable outcome.
		/// </summary>
		[DataMember(Name = "message")]
		public string Message { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the message was posted.
		/// </summary>
		[DataMember(Name = "success")]
		public bool Success { get; set; }
	}
}
