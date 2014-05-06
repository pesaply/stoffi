# -*- encoding : utf-8 -*-
# The model of the OAuth 2 verifier resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a verifier token for OAuth 2.
class Oauth2Verifier < OauthToken
	validates_presence_of :user
	attr_accessor :state

	# Exchanges the token by invalidating the current token and creating a new one.
	def exchange!(params={})
		OauthToken.transaction do
			token = Oauth2Token.create! :user=>user,:client_application=>client_application, :scope => scope
			invalidate!
			token
		end
	end

	# The token of the verifier.
	def code
		token
	end

	# The URL to redirect to when verification is complete.
	def redirect_url
		callback_url
	end

	# Serializes the verifier token to a HTTP query.
	def to_query
		q = "code=#{token}"
		q << "&state=#{URI.escape(state)}" if @state
		q
	end

	protected

	# Generates the keys and timestamps of the verifier token.
	def generate_keys
		self.token = OAuth::Helper.generate_key(20)[0,20]
		self.expires_at = 10.minutes.from_now
		self.authorized_at = Time.now
	end

end
