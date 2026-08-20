using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Web.SessionState;
using Microsoft.Extensions.DependencyInjection;
using Terrasoft.Web.Common;
using WebsocketLabApp.Messaging;

namespace WebsocketLabApp.EntryPoints.WebServices {

	/// <summary>
	/// Provides a lab endpoint that sends a transient message to the authenticated user's browser.
	/// </summary>
	[ServiceContract]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
	public sealed class WebSocketReferenceService : BaseService, IReadOnlySessionState {

		/// <summary>
		/// Posts a message to the current user's active Creatio message channel.
		/// </summary>
		/// <param name="request">The message request.</param>
		/// <returns>The delivery attempt result.</returns>
		[OperationContract]
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json,
			ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare)]
		public SendWebSocketMessageResponse SendToCurrentUser(SendWebSocketMessageRequest request) {
			string text = request?.Message?.Trim();
			if (string.IsNullOrWhiteSpace(text)) {
				SetStatusCode(400);
				return new SendWebSocketMessageResponse {
					Success = false,
					Message = "The message field is required."
				};
			}
			if (text.Length > 1000) {
				SetStatusCode(400);
				return new SendWebSocketMessageResponse {
					Success = false,
					Message = "The message field cannot exceed 1000 characters."
				};
			}

			Guid correlationId = Guid.NewGuid();
			var notification = new WebSocketNotification {
				CorrelationId = correlationId,
				Message = text,
				SentAtUtc = DateTime.UtcNow
			};
			IWebSocketMessagePublisher publisher =
				WebsocketLabApp.Instance.GetRequiredService<IWebSocketMessagePublisher>();
			ICurrentUserIdProvider currentUser =
				WebsocketLabApp.Instance.GetRequiredService<ICurrentUserIdProvider>();
			WebSocketPublishResult result = publisher.Publish(currentUser.GetCurrentUserId(), notification);
			return new SendWebSocketMessageResponse {
				Success = result.Success,
				CorrelationId = correlationId,
				EventId = result.EventId,
				Message = result.Message
			};
		}

		private void SetStatusCode(int statusCode) {
#if NETSTANDARD2_0
			var httpContext = HttpContextAccessor?.GetInstance();
			if (httpContext != null) {
				httpContext.Response.StatusCode = statusCode;
			}
#else
			WebOperationContext context = WebOperationContext.Current;
			if (context != null) {
				context.OutgoingResponse.StatusCode = (HttpStatusCode)statusCode;
				return;
			}
			var httpContext = HttpContextAccessor?.GetInstance();
			if (httpContext != null) {
				httpContext.Response.StatusCode = statusCode;
			}
#endif
		}
	}
}
