using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
			string resourceBody = File.ReadAllText(Path.Combine(repositoryRoot, "packages", "WebsocketLab", "Resources",
				"UsrWebsocketReference_Page.ClientUnit", "resource.en-US.xml"));
			string metadataBody = File.ReadAllText(Path.Combine(repositoryRoot, "packages", "WebsocketLab", "Schemas",
				"UsrWebsocketReference_Page", "metadata.json"));

			// Act
			string[] requiredFragments = {
				"backend: \"WebsocketLab.Message\"",
				"ptp: \"WebsocketLab.Ptp\"",
				"broadcast: \"WebsocketLab.Broadcast\"",
				"new sdk.MessageChannelService()",
				"sdk.MessageChannelType.PTP",
				"sdk.MessageChannelType.BROADCAST",
				"Promise.allSettled",
				"usr.SendBackendPushRequest",
				"usr.SendPtpRequest",
				"usr.SendBroadcastRequest",
				"$UsrBackendPushResult",
				"$UsrPtpResult",
				"$UsrBroadcastResult",
				"crt.HandleViewModelResumeRequest",
				"crt.HandleViewModelPauseRequest",
				"UsrWebSocketSubscriptionsPending",
				"unsubscribeAll(request.$context.UsrWebSocketSubscriptions)"
			};

			// Assert
			pageBody.Should().ContainAll(requiredFragments,
				because: "the page must subscribe with the backend sender and release the runtime handle");
			pageBody.Should().NotContain("Terrasoft.ServerChannel",
				because: "new Freedom UI code uses the public MessageChannelService API");
			string viewConfig = pageBody.Split("/**SCHEMA_VIEW_CONFIG_DIFF*/")[1];
			viewConfig.Should().NotContain("...",
				because: "the designer-managed view configuration must remain declarative JSON-like metadata");
			viewConfig.Should().NotContain(".map(",
				because: "generated JavaScript entries cannot be round-tripped by the page designer");
			string[] resourceKeys = Regex.Matches(pageBody, @"#ResourceString\(([^)]+)\)#")
				.Select(match => match.Groups[1].Value)
				.Distinct()
				.ToArray();
			foreach (string resourceKey in resourceKeys) {
				resourceBody.Should().Contain($"LocalizableStrings.{resourceKey}.Value",
					because: $"the en-US resource must define the page key {resourceKey}");
				metadataBody.Should().Contain($"\"{resourceKey}\"",
					because: $"the client-unit metadata must register the page key {resourceKey}");
			}
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
