using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace WebsocketLab.Tests.FreedomUi {

	[TestFixture(Category = "UnitTests")]
	internal sealed class FreedomUiWebSocketContractTests {

		[Test]
		[Description("Keeps the Freedom UI page on the modern sender-matched subscription with paired cleanup")]
		public void PageSchema_ShouldPreserveModernWebSocketContract() {
			// Arrange
			string repositoryRoot = FindRepositoryRoot();
			string pageBody = File.ReadAllText(Path.Combine(repositoryRoot, "packages", "WebsocketLab", "Schemas",
				"UsrWebsocketReference_Page", "UsrWebsocketReference_Page.js"));

			// Act
			string[] requiredFragments = {
				"const senderName = \"WebsocketLab.Message\"",
				"new sdk.MessageChannelService()",
				"crt.HandleViewModelResumeRequest",
				"crt.HandleViewModelPauseRequest",
				"UsrWebSocketSubscriptionPending",
				"UsrWebSocketSubscription.unsubscribe()"
			};

			// Assert
			pageBody.Should().ContainAll(requiredFragments,
				because: "the page must subscribe with the backend sender and release the runtime handle");
			pageBody.Should().NotContain("Terrasoft.ServerChannel",
				because: "new Freedom UI code uses the public MessageChannelService API");
		}

		private static string FindRepositoryRoot() {
			DirectoryInfo current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (current != null && !File.Exists(Path.Combine(current.FullName, "MainSolution.slnx"))) {
				current = current.Parent;
			}
			return current?.FullName
				?? throw new DirectoryNotFoundException("Could not locate the WebSocket reference repository root.");
		}
	}
}
