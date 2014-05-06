# -*- encoding : utf-8 -*-
# The model of the OAuth 1 token resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describe a token for use with OAuth requests.
class OauthToken < ActiveRecord::Base
	belongs_to :client_application
	belongs_to :user
	validates_uniqueness_of :token
	validates_presence_of :client_application, :token
	before_validation :generate_keys, :on => :create

	# Whether or not the token has been invalidated.
	def invalidated?
		invalidated_at != nil
	end

	# Invalidates the token.
	def invalidate!
		update_attribute(:invalidated_at, Time.now)
	end

	# Whether or not the token has been authorized.
	def authorized?
		authorized_at != nil && !invalidated?
	end

	# Gets the HTTP query for sending to a requesting client app.
	def to_query
		"oauth_token=#{token}&oauth_token_secret=#{secret}"
	end
	
	#def valid?
	#	# todo: check valid_to field
	#	not invalidated?
	#end

	protected

	# Generates the token's keys.
	def generate_keys
		self.token = OAuth::Helper.generate_key(40)[0,40]
		self.secret = OAuth::Helper.generate_key(40)[0,40]
	end
end
