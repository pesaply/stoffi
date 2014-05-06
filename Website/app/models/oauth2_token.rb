# -*- encoding : utf-8 -*-
# The model of the OAuth 2 token resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes an access token for OAuth 2.
class Oauth2Token < AccessToken
	attr_accessor :state
	
	# Serializes the access token to JSON.
	def as_json(options={})
		d = {:access_token=>token, :token_type => 'bearer'}
		d[:expires_in] = expires_in if expires_at
		d
	end

	# Serializes the access token to a HTTP query.
	def to_query
		q = "access_token=#{token}&token_type=bearer"
		q << "&state=#{URI.escape(state)}" if @state
		q << "&expires_in=#{expires_in}" if expires_at
		q << "&scope=#{URI.escape(scope)}" if scope
		q
	end

	# The time left before the token expires (in seconds).
	def expires_in
		expires_at.to_i - Time.now.to_i
	end
end
