﻿using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
	public class OAuthUserSession : IOAuthSession
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(OAuthUserSession));

		public OAuthUserSession()
		{
			this.ProviderOAuthAccess = new List<IOAuthTokens>();
			this.AuthHttpGateway = new OAuthHttpGateway();
		}

		public IOAuthHttpGateway AuthHttpGateway { get; set; }

		public string ReferrerUrl { get; set; }

		public string Id { get; set; }

		public string UserAuthId { get; set; }

		public string TwitterUserId { get; set; }

		public string TwitterScreenName { get; set; }

		public string FacebookUserId { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string DisplayName { get; set; }

		public string Email { get; set; }

		public string RequestTokenSecret { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime LastModified { get; set; }

		public List<IOAuthTokens> ProviderOAuthAccess { get; set; }

		public virtual bool IsAnyAuthorized()
		{
			return ProviderOAuthAccess
				.Any(x => !string.IsNullOrEmpty(x.AccessTokenSecret) 
					&& !string.IsNullOrEmpty(x.AccessToken));
		}

		public virtual bool IsAuthorized(string provider)
		{
			return ProviderOAuthAccess
				.Any(x => x.Provider == provider
					&& !string.IsNullOrEmpty(x.AccessTokenSecret)
					&& !string.IsNullOrEmpty(x.AccessToken));
		}

		private void SaveUserAuth(IUserAuthRepository provider, IOAuthTokens tokens)
		{
			if (provider == null) return;
			this.UserAuthId = provider.CreateOrMergeAuthSession(this, tokens);
		}

		public virtual void OnAuthenticated(IServiceBase oAuthService, IOAuthTokens tokens, Dictionary<string, string> authInfo)
		{
			var provider = tokens.Provider;
			var authProvider = oAuthService.TryResolve<IUserAuthRepository>();
			if (authProvider != null)
				authProvider.LoadUserAuth(this, tokens);

			if (provider == TwitterOAuthConfig.Name)
			{
				if (authInfo.ContainsKey("user_id"))
					tokens.UserId = this.TwitterUserId = authInfo.GetValueOrDefault("user_id");

				if (authInfo.ContainsKey("screen_name"))
					this.TwitterScreenName = authInfo.GetValueOrDefault("screen_name");

				try
				{
					var json = AuthHttpGateway.DownloadTwitterUserInfo(this.TwitterUserId);
					var obj = JsonObject.Parse(json);
					tokens.DisplayName = obj.Get("name");
					this.DisplayName = tokens.DisplayName ?? this.DisplayName;
				}
				catch (Exception ex)
				{
					Log.Error("Could not retrieve twitter user info for '{0}'".Fmt(TwitterUserId), ex);
				}
			}
			else if (provider == FacebookOAuthConfig.Name)
			{
				try
				{
					var json = AuthHttpGateway.DownloadFacebookUserInfo(tokens.AccessTokenSecret);
					var obj = JsonObject.Parse(json);
					tokens.UserId = obj.Get("id");
					tokens.DisplayName = obj.Get("name");
					tokens.FirstName = obj.Get("first_name");
					tokens.LastName = obj.Get("last_name");
					tokens.Email = obj.Get("email");

					this.FacebookUserId = tokens.UserId ?? this.FacebookUserId;
					this.DisplayName = tokens.DisplayName ?? this.DisplayName;
					this.FirstName = tokens.FirstName ?? this.FirstName;
					this.LastName = tokens.LastName ?? this.LastName;
					this.Email = tokens.Email ?? this.Email;
				}
				catch (Exception ex)
				{
					Log.Error("Could not retrieve facebook user info for '{0}'".Fmt(tokens.DisplayName), ex);
				}
			}

			authInfo.ForEach((x, y) => tokens.Items[x] = y);

			SaveUserAuth(oAuthService.TryResolve<IUserAuthRepository>(), tokens);
		}
	}

}