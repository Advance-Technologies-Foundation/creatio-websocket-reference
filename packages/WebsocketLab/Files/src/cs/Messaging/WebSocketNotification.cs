using System;
using System.Runtime.Serialization;

namespace WebsocketLabApp.Messaging {

	[DataContract]
	internal sealed class WebSocketNotification {

		[DataMember(Name = "correlationId")]
		public Guid CorrelationId { get; set; }

		[DataMember(Name = "message")]
		public string Message { get; set; }

		[DataMember(Name = "sentAtUtc")]
		public DateTime SentAtUtc { get; set; }
	}
}
