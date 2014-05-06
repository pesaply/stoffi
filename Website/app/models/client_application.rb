# -*- encoding : utf-8 -*-
# The model of the app resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'oauth'
require 'base'

# Describes an app for accessing the API.
class ClientApplication < ActiveRecord::Base
	include Base
	
	# associations
	belongs_to :user
	has_many :tokens, :class_name => "OauthToken"
	has_many :access_tokens
	has_many :oauth2_verifiers
	has_many :oauth_tokens
	
	# validations
	validates_presence_of :name, :website, :key, :secret
	validates_uniqueness_of :key
	before_validation :generate_keys, :on => :create

	validates_format_of :website, :with => /\Ahttp(s?):\/\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/]))?/i
	validates_format_of :support_url, :with => /\Ahttp(s?):\/\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/]))?/i, :allow_blank=>true
	validates_format_of :callback_url, :with => /\Ahttp(s?):\/\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/]))?/i, :allow_blank=>true

	attr_accessor :token_callback_url
	
	# Gets a list of all apps not added by a given user
	def self.not_added_by(user)
		if user == nil
			return self.all
		else
			tokens = "SELECT * from oauth_tokens WHERE oauth_tokens.invalidated_at IS NULL AND oauth_tokens.authorized_at IS NOT NULL AND oauth_tokens.user_id = #{user.id}"
		
			return select("client_applications.*").
			joins("LEFT JOIN (#{tokens}) oauth_tokens ON oauth_tokens.client_application_id = client_applications.id").
			group("client_applications.id").
			having("count(oauth_tokens.id) = 0")
		end
	end

	# Gets an authorized token given a token key.
	def self.find_token(token_key)
		token = OauthToken.find_by_token(token_key, :include => :client_application)
		if token && token.authorized?
			token
		else
			nil
		end
	end

	# Verifies a request by signing it with OAuth.
	def self.verify_request(request, options = {}, &block)
		begin
			signature = OAuth::Signature.build(request, options, &block)
			return false unless OauthNonce.remember(signature.request.nonce, signature.request.timestamp)
			value = signature.verify
			value
		rescue OAuth::Signature::UnknownSignatureMethod => e
			false
		end
	end

	# The URL to the OAuth server.
	def oauth_server
		@oauth_server ||= OAuth::Server.new("http://beta.stoffiplayer.com")
	end

	# The credentials of the OAuth consumer.
	def credentials
		@oauth_client ||= OAuth::Consumer.new(key, secret)
	end

	# Creates a token for requesting access to the OAuth API.
	#
	# Note: If our application requires passing in extra parameters handle it here
	def create_request_token(params={})
		RequestToken.create :client_application => self, :callback_url=>self.token_callback_url
	end
	
	# The large icon of the app (64x64).
	def large_icon
		return icon_64 unless icon_64.to_s.empty?
		"/assets/gfx/app_default_icon_64.png"
	end
	
	# The small icon of the app (16x16).
	def small_icon
		return icon_16 unless icon_16.to_s.empty?
		"/assets/gfx/app_default_icon_16.png"
	end
	
	# The type of the resource.
	def kind
		"app"
	end
	
	# The string to display to users for representing the resource.
	def display
		name
	end
	
	# The options to use when the app is serialized.
	def serialize_options
		{
			:except => :secret,
			:methods => [ :kind, :display, :url ]
		}
	end

protected

	# Generate the public and secret API keys for the app.
	def generate_keys
		self.key = OAuth::Helper.generate_key(40)[0,40]
		self.secret = OAuth::Helper.generate_key(40)[0,40]
	end
end
