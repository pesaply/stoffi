# -*- encoding : utf-8 -*-
# The model of the OAuth request token resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a request token for OAuth.
class RequestToken < OauthToken

	attr_accessor :provided_oauth_verifier

	# Authorizes a user for the token.
	def authorize!(user)
		return false if authorized?
		self.user = user
		self.authorized_at = Time.now
		self.verifier=OAuth::Helper.generate_key(20)[0,20] unless oauth10?
		self.save
	end

	# Exchanges the request token to an access token.
	def exchange!
		return false unless authorized?
		return false unless oauth10? || verifier==provided_oauth_verifier

		RequestToken.transaction do
			access_token = user.tokens.where(
				"client_application_id = ? AND type = 'AccessToken' AND invalidated_at IS NULL AND authorized_at IS NOT NULL",
				client_application.id
		).first
			if (token != nil)
		access_token = AccessToken.create(:user => user, :client_application => client_application)
		end
			
			invalidate!
			access_token
		end
	end

	# Serializes the request token to an HTTP query.
	def to_query
		if oauth10?
			super
		else
			"#{super}&oauth_callback_confirmed=true"
		end
	end
	
	# Whether or not to redirect the user to a default route after authorization.
	def oob?
		callback_url.nil? || callback_url.downcase == 'oob'
	end

	# Whether or not OAuth 1.0 should be used.
	def oauth10?
		(defined? OAUTH_10_SUPPORT) && OAUTH_10_SUPPORT && self.callback_url.blank?
	end

end
