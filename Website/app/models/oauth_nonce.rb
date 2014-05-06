# -*- encoding : utf-8 -*-
# The model of the OAuth nonce resource.
#
# Simple store of nonces. The OAuth Spec requires that any given pair of nonce and timestamps are unique.
# Thus you can use the same nonce with a different timestamp and viceversa.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describe a nonce for use with OAuth requests.
class OauthNonce < ActiveRecord::Base
	validates_presence_of :nonce, :timestamp
	validates_uniqueness_of :nonce, :scope => :timestamp

	# Remembers a nonce and it's associated timestamp. It returns false if it has already been used
	def self.remember(nonce, timestamp)
		oauth_nonce = OauthNonce.create(:nonce => nonce, :timestamp => timestamp)
		return false if oauth_nonce.new_record?
		oauth_nonce
	end
end
