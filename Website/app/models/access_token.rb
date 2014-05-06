# -*- encoding : utf-8 -*-
# The model of the OAuth access token resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a token for accessing the Stoffi API via OAuth.
class AccessToken < OauthToken
	validates_presence_of :user, :secret
	before_create :set_authorized_at

	# Implement this to return a hash or array of the capabilities the access token has
	# This is particularly useful if you have implemented user defined permissions.
	# def capabilities
	#   {:invalidate=>"/oauth/invalidate",:capabilities=>"/oauth/capabilities"}
	# end

	protected

	# Sets when a token was authorized.
	def set_authorized_at
		self.authorized_at = Time.now
	end
end
