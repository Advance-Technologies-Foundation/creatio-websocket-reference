using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Terrasoft.Core.ServiceModelContract;
using Terrasoft.Web.Http.Abstractions;
using WebsocketLabApp.EntryPoints.WebServices;
using WebsocketLabApp.Messaging;
using App = WebsocketLabApp.WebsocketLabApp;

namespace WebsocketLab.Tests.EntryPoints.WebServices {

	[TestFixture(Category = "UnitTests")]
	internal sealed class WebSocketReferenceServiceTests {

		private ICurrentUserIdProvider _currentUser;
		private IWebSocketMessagePublisher _publisher;
		private HttpResponse _httpResponse;
		private WebSocketReferenceService _sut;

		[SetUp]
		public void SetUpService() {
			_currentUser = Substitute.For<ICurrentUserIdProvider>();
			_publisher = Substitute.For<IWebSocketMessagePublisher>();
			App.InjectedServices = new List<Func<IServiceCollection, IServiceCollection>> {
				services => services.AddSingleton(_publisher).AddSingleton(_currentUser)
			};
			App.Instance.Reset();

			HttpContext context = Substitute.For<HttpContext>();
			_httpResponse = Substitute.For<HttpResponse>();
			context.Response.Returns(_httpResponse);
			IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
			accessor.GetInstance().Returns(context);
			_sut = new WebSocketReferenceService { HttpContextAccessor = accessor };
		}

		[TearDown]
		public void TearDownService() {
			App.InjectedServices = null;
			App.Instance.Reset();
		}

		[Test]
		[Description("Uses the authenticated user and maps a successful publish result")]
		public void SendToCurrentUser_WhenRequestIsValid_PublishesForAuthenticatedUser() {
			// Arrange
			Guid currentUserId = Guid.NewGuid();
			Guid eventId = Guid.NewGuid();
			_currentUser.GetCurrentUserId().Returns(currentUserId);
			WebSocketNotification capturedNotification = null;
			_publisher.Publish(currentUserId, Arg.Do<WebSocketNotification>(item =>
				capturedNotification = item)).Returns(WebSocketPublishResult.Delivered(eventId));

			// Act
			SendWebSocketMessageResponse response = _sut.SendToCurrentUser(
				new SendWebSocketMessageRequest { Message = "  Hello current user  " });

			// Assert
			response.Success.Should().BeTrue(
				because: "the publisher posted to the authenticated user's active channel");
			response.EventId.Should().Be(eventId,
				because: "the HTTP caller needs the posted event identifier");
			capturedNotification.Message.Should().Be("Hello current user",
				because: "the service normalizes surrounding whitespace before publishing");
			capturedNotification.CorrelationId.Should().Be(response.CorrelationId,
				because: "the same correlation identifier must cross HTTP and WebSocket boundaries");
			_publisher.Received(1).Publish(currentUserId, capturedNotification);
		}

		[Test]
		[Description("Rejects an empty message before invoking the WebSocket publisher")]
		public void SendToCurrentUser_WhenMessageIsEmpty_ReturnsBadRequest() {
			// Arrange
			var request = new SendWebSocketMessageRequest { Message = "   " };

			// Act
			SendWebSocketMessageResponse response = _sut.SendToCurrentUser(request);

			// Assert
			response.Success.Should().BeFalse(
				because: "an empty payload is not a useful browser notification");
			response.Message.Should().Be("The message field is required.",
				because: "the caller needs the exact invalid field");
			_httpResponse.StatusCode.Should().Be(400,
				because: "missing required input is an HTTP client error");
			_publisher.DidNotReceiveWithAnyArgs().Publish(default, default);
		}

		[Test]
		[Description("Rejects messages longer than the lab payload limit before invoking the publisher")]
		public void SendToCurrentUser_WhenMessageExceedsLimit_ReturnsBadRequest() {
			// Arrange
			var request = new SendWebSocketMessageRequest { Message = new string('a', 1001) };

			// Act
			SendWebSocketMessageResponse response = _sut.SendToCurrentUser(request);

			// Assert
			response.Success.Should().BeFalse(
				because: "the lab deliberately keeps transient WebSocket payloads small");
			response.Message.Should().Be("The message field cannot exceed 1000 characters.",
				because: "the caller needs the exact payload constraint");
			_httpResponse.StatusCode.Should().Be(400,
				because: "an oversized message is an HTTP client error");
			_publisher.DidNotReceiveWithAnyArgs().Publish(default, default);
		}

		[Test]
		[Description("Reports a missing active browser channel as an expected delivery outcome")]
		public void SendToCurrentUser_WhenBrowserIsOffline_ReturnsFailureValue() {
			// Arrange
			Guid currentUserId = Guid.NewGuid();
			_currentUser.GetCurrentUserId().Returns(currentUserId);
			_publisher.Publish(currentUserId, Arg.Any<WebSocketNotification>())
				.Returns(WebSocketPublishResult.NotDelivered("The current user has no active browser channel."));

			// Act
			SendWebSocketMessageResponse response = _sut.SendToCurrentUser(
				new SendWebSocketMessageRequest { Message = "Hello" });

			// Assert
			response.Success.Should().BeFalse(
				because: "transient delivery cannot succeed without an active browser channel");
			response.Message.Should().Be("The current user has no active browser channel.",
				because: "the service must preserve the publisher's actionable outcome");
			response.EventId.Should().BeNull(
				because: "no SimpleMessage was posted");
		}
	}
}
