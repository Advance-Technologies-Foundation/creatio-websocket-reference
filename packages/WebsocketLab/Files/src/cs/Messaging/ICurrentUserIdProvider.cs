using System;
using Terrasoft.Core;

namespace WebsocketLabApp.Messaging {

	internal interface ICurrentUserIdProvider {

		Guid GetCurrentUserId();
	}

	internal sealed class CurrentUserIdProvider : ICurrentUserIdProvider {

		private readonly Func<UserConnection> _userConnectionAccessor;

		public CurrentUserIdProvider(Func<UserConnection> userConnectionAccessor) {
			_userConnectionAccessor = userConnectionAccessor;
		}

		public Guid GetCurrentUserId() => _userConnectionAccessor().CurrentUser.Id;
	}
}
