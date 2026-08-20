using System;
using System.Text.RegularExpressions;
using Common.Logging;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Terrasoft.Messaging.Common;
using WebsocketLabApp.Messaging;

namespace WebsocketLab.Tests.Messaging {

	[TestFixture(Category = "UnitTests")]
	internal sealed class WebSocketMessagePublisherTests {
		private static WebSocketMessagePublisher CreateSut(Func<IMsgChannelManager> accessor) =>
			new WebSocketMessagePublisher(accessor, Substitute.For<ILog>());

		[Test]
		[Description("Posts a JSON SimpleMessage with the stable sender to the requested active user channel")]
		public void Publish_WhenUserChannelIsActive_PostsExpectedMessage() {
			// Arrange
			Guid userId = Guid.NewGuid();
			Guid correlationId = Guid.NewGuid();
			IMsgChannelManager manager = Substitute.For<IMsgChannelManager>();
			IMsgChannel channel = Substitute.For<IMsgChannel>();
			manager.FindItemByUId(userId).Returns(channel);
			IMsg postedMessage = null;
			channel.When(item => item.PostMessage(Arg.Any<IMsg>()))
				.Do(call => postedMessage = call.Arg<IMsg>());
			WebSocketMessagePublisher sut = CreateSut(() => manager);
			var notification = new WebSocketNotification {
				CorrelationId = correlationId,
				Message = "Hello from the backend",
				SentAtUtc = new DateTime(2026, 8, 20, 10, 30, 0, DateTimeKind.Utc)
			};

			// Act
			WebSocketPublishResult result = sut.Publish(userId, notification);

			// Assert
			result.Success.Should().BeTrue(
				because: "an active channel accepted the transient message");
			result.EventId.Should().NotBeNull(
				because: "each posted SimpleMessage needs an event identifier");
			postedMessage.Should().NotBeNull(
				because: "the publisher must post to the resolved user channel");
			postedMessage.Header.Sender.Should().Be("WebsocketLab.Message",
				because: "the frontend subscribes using this exact routing key");
			postedMessage.Id.Should().Be(result.EventId.Value,
				because: "the response must identify the event that was posted");
			JObject body = JObject.Parse((string)postedMessage.Body);
			body.Value<string>("message").Should().Be("Hello from the backend",
				because: "the message body is the frontend payload");
			Guid.Parse(body.Value<string>("correlationId")).Should().Be(correlationId,
				because: "the browser must be able to correlate the HTTP request and WebSocket event");
		}

		[Test]
		[Description("Returns an explicit non-delivery result when the user has no active browser channel")]
		public void Publish_WhenUserChannelIsMissing_ReturnsNotDelivered() {
			// Arrange
			Guid userId = Guid.NewGuid();
			IMsgChannelManager manager = Substitute.For<IMsgChannelManager>();
			manager.FindItemByUId(userId).Returns((IMsgChannel)null);
			WebSocketMessagePublisher sut = CreateSut(() => manager);
			var notification = new WebSocketNotification();

			// Act
			WebSocketPublishResult result = sut.Publish(userId, notification);

			// Assert
			result.Success.Should().BeFalse(
				because: "WebSocket delivery is available only while a browser channel is connected");
			result.Message.Should().Be("The current user has no active browser channel.",
				because: "callers need an actionable transient-delivery failure");
			manager.Received(1).FindItemByUId(userId);
		}

		[Test]
		[Description("Returns an explicit non-delivery result when Creatio messaging is not running")]
		public void Publish_WhenManagerIsUnavailable_ReturnsNotDelivered() {
			// Arrange
			WebSocketMessagePublisher sut = CreateSut(() => null);

			// Act
			WebSocketPublishResult result = sut.Publish(Guid.NewGuid(), new WebSocketNotification());

			// Assert
			result.Success.Should().BeFalse(
				because: "a stopped channel manager cannot deliver a WebSocket event");
			result.Message.Should().Be("The Creatio message channel is not running.",
				because: "the failure should distinguish platform availability from an offline user");
		}

		[Test]
		[Description("Returns an explicit non-delivery result when the browser disconnects during publication")]
		public void Publish_WhenChannelClosesBeforePost_ReturnsNotDelivered() {
			// Arrange
			Guid userId = Guid.NewGuid();
			IMsgChannelManager manager = Substitute.For<IMsgChannelManager>();
			IMsgChannel channel = Substitute.For<IMsgChannel>();
			manager.FindItemByUId(userId).Returns(channel);
			channel.When(item => item.PostMessage(Arg.Any<IMsg>()))
				.Do(_ => throw new InvalidOperationException("Channel closed"));
			ILog logger = Substitute.For<ILog>();
			WebSocketMessagePublisher sut = new WebSocketMessagePublisher(() => manager, logger);

			// Act
			WebSocketPublishResult result = sut.Publish(userId, new WebSocketNotification {
				Message = "DoNotLogThisPayload"
			});

			// Assert
			result.Success.Should().BeFalse(
				because: "a disconnect between channel lookup and posting is an expected transient outcome");
			result.EventId.Should().BeNull(
				because: "the event was not accepted by the channel");
			result.Message.Should().Be("The message could not be posted to the active user channel.",
				because: "the caller needs a stable non-delivery explanation instead of an HTTP 500 response");
			logger.Received(1).Warn(
				Arg.Is<string>(value => Regex.IsMatch(value,
					@"WebSocket event [0-9a-fA-F-]{36} for user " + userId +
					@" and sender WebsocketLab\.Message") &&
					!value.Contains("DoNotLogThisPayload")),
				Arg.Any<InvalidOperationException>());
		}

		[Test]
		[Description("Returns non-delivery and logs identifiers when resolving the user channel throws")]
		public void Publish_WhenChannelResolutionThrows_ReturnsNotDelivered() {
			// Arrange
			Guid userId = Guid.NewGuid();
			ILog logger = Substitute.For<ILog>();
			WebSocketMessagePublisher sut = new WebSocketMessagePublisher(
				() => throw new InvalidOperationException("Manager stopped"), logger);

			// Act
			WebSocketPublishResult result = sut.Publish(userId, new WebSocketNotification {
				Message = "DoNotLogThisPayload"
			});

			// Assert
			result.Success.Should().BeFalse(
				because: "a manager race is a transient delivery failure rather than an HTTP 500");
			result.Message.Should().Be("The Creatio message channel is unavailable.",
				because: "the caller needs a non-committal channel failure explanation");
			logger.Received(1).Warn(
				Arg.Is<string>(value => value.Contains(userId.ToString()) &&
					!value.Contains("DoNotLogThisPayload")),
				Arg.Any<InvalidOperationException>());
		}
	}
}
