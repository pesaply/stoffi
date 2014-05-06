# -*- encoding : utf-8 -*-
# The model of the link resource describing links to third parties.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'
require 'json'
require 'links/facebook'
require 'links/twitter'
require 'links/lastfm'

# Describes a link between a user's Stoffi account and a third party account.
class Link < ActiveRecord::Base
	include Base

	belongs_to :user
	has_many :backlogs, :dependent => :destroy, :class_name => "LinkBacklog"
	
	validates :provider, presence: true
	
	# Updates the access token by using the refresh token.
	def update_credentials(auth)
		exp = auth['credentials']['expires_at']
		exp = DateTime.strptime("#{exp}",'%s') if exp
		self.update_attributes(
			:access_token => auth['credentials']['token'],
			:access_token_secret => auth['credentials']['secret'],
			:refresh_token => auth['credentials']['refresh_token'],
			:token_expires_at => exp
		)
		
		# retry pending submissions
		retried = []
		self.backlogs.each do |backlog|
			next unless backlog.resource
			
			# only resubmit each resource once
			next if retried.include? backlog.resource
			retried << backlog.resource
			
			share(backlog.resource)           if backlog.resource.is_a? Share
			update_listen(backlog.resource)   if backlog.resource.is_a? Listen
			update_playlist(backlog.resource) if backlog.resource.is_a? Playlist
			donate(backlog.resource)          if backlog.resource.is_a? Donation
		end
		
		# clear pending submissions
		self.backlogs.destroy_all
	end
	
	# The list of services currently supported.
	def self.available
		[
			{ :name => "Twitter" },
			{ :name => "Facebook" },
			{ :name => "Google", :link_name => "google_oauth2" },
			{ :name => "Vimeo" },
			{ :name => "SoundCloud" },
			{ :name => "Last.fm", :link_name => "lastfm" },
			{ :name => "MySpace" },
			{ :name => "Yahoo" },
			{ :name => "Weibo" },
			{ :name => "vKontakte" },
			{ :name => "LinkedIn" },
			{ :name => "Windows Live", :link_name => "windowslive" }
		]
	end
	
	# Whether or not a user profile picture can be extracted from the third party account.
	def picture?
		["twitter", "facebook", "google_oauth2", "lastfm", "vimeo"].include?(provider)
	end
	
	# Whether or not the third party has a social button.
	def can_button?
		["twitter", "google_oauth2"].include?(provider)
	end
	
	# Whether or not to show a social button.
	def button?
		can_button? and show_button
	end
	
	# Whether or not stuff can be shared with the third party account.
	def can_share?
		["facebook", "twitter", "lastfm"].include?(provider)
	end

	# Whether or not to share stuff with the third party account.
	def share?
		do_share && can_share?
	end
	
	# Whether or not listens can be shared with the third party account.
	def can_listen?
		["facebook", "lastfm"].include?(provider)
	end
	
	# Whether or not to share listens with the third party account.
	def listen?
		do_listen && can_listen?
	end
	
	# Whether or not donations can be shared with the third party account.
	def can_donate?
		["facebook", "twitter"].include?(provider)
	end
	
	# Whether or not to share donations with the third party account.
	def donate?
		do_donate && can_donate?
	end
	
	# Whether or not playlists can be kept and synchronized with the third party account.
	def can_create_playlist?
		["facebook", "lastfm"].include?(provider)
	end
	
	# Whether or not to keep and synchronize playlists with the third party account.
	def create_playlist?
		do_create_playlist && can_create_playlist?
	end
	
	def error
		return backlogs.order(:created_at).last.error if backlogs.count > 0
		return nil
	end
	
	alias_method "can_button", "can_button?"
	alias_method "can_share", "can_share?"
	alias_method "can_listen", "can_listen?"
	alias_method "can_donate", "can_donate?"
	alias_method "can_create_playlist", "can_create_playlist?"
	
	# The user's profile picture on the third party account.
	def picture
		begin
			case provider
			when "facebook"
				response = get("/me?fields=picture")
				return response['picture']['data']['url']
				
			when "twitter"
				response = get("/1.1/users/show.json?user_id=#{uid}")
				return response['profile_image_url_https']
				
			when "google_oauth2"
				response = get("/oauth2/v1/userinfo")
				return response['picture'] if response['picture']
				
			when "myspace"
				response = get("/v1.0/people/@me")
				return response['thumbnailUrl'] if response['thumbnailUrl']
				
			when "vimeo"
				response = get("/api/v2/#{uid}/info.json")
				return response['portrait_medium']
				
			when "yahoo"
				response = get("/v1/user/#{uid}/profile/tinyusercard")
				return response['profile']['image']['imageUrl']
				
			when "vkontakte"
				response = get("api.php?method=getProfiles?uids=#{uid}&fields=photo_medium")
				logger.debug response.inspect
				return response['Response'][0]['Photo']
				
			when "lastfm"
				response = get("/2.0/?method=user.getinfo&format=json&user=#{uid}&api_key=#{creds[:id]}")
				return response['user']['image'][1]['#text']
				
			when "linkedin"
				response = get("/v1/people/~")
				logger.debug response.inspect
				#return ???
				
			when "windowslive"
				response = get("/v5.0/me/picture?access_token=#{access_token}")
				logger.debug response.inspect
				return response['person']['picture_url']
				
			end
		rescue Exception => e
			logger.debug "error fetching pictures from #{provider}"
			logger.debug e.to_yaml
		end
		return nil
	end
	
	# The user's names on the third party account.
	#
	# Returns a hash of <tt>fullname</tt> and/or <tt>username</tt>.
	def names
		begin
			case provider
			when "facebook"
				response = get("/me?fields=name,username")
				r = { :fullname => response['name'] }
				r[:username] = response['username'] if response['username'] != nil
				return r
				
			when "twitter"
				response = get("/1.1/users/show.json?user_id=#{uid}")
				return {
					:username => response['screen_name'],
					:fullname => response['name']
				}
				
			when "google_oauth2"
				response = get("/oauth2/v1/userinfo")
				return {
					:fullname => response['name'],
					:username => response['email'].split('@',2)[0]
				}
				
			when "vimeo"
				response = get("/api/v2/#{uid}/info.json")
				return { :fullname => response['display_name'] }
				
			when "lastfm"
				response = get("/2.0/?method=user.getinfo&format=json&user=#{uid}&api_key=#{creds[:id]}")
				return {
					:username => response['user']['name'],
					:fullname => response['user']['realname']
				}
				
			end
		rescue Exception => e
			logger.debug "error fetching names from #{provider}"
			logger.debug e.to_yaml
		end
		return {}
	end
	
	# The user's name on the third party account.
	def name
		n = names
		return n[:fullname] if n[:fullname]
		return n[:username] if n[:username]
		return I18n.translate("user.name.unknown")
	end
	
	# Share a resource on the service.
	def share(s)
		return unless share?
		
		if s.resource.is_a?(Song)
			# fix message to either
			#  - the user's message
			#  - title by artist
			#  - title
			msg = s.message
			if msg.to_s == ""
				msg = s.resource.title
				a = s.resource.artist.name
				msg += " by #{a}" unless a.to_s == ""
			end
			logger.debug "share message: #{msg}"
			
		elsif s.resource.is_a?(Playlist)
			# fix message to either
			#  - the user's message
			#  - playlist by user
			msg = s.message
			if msg.to_s == ""
				msg = "#{s.resource.name} by #{s.resource.user.name}"
			end
			logger.debug "share message: #{msg}"
		end
		
		begin
			case [provider, s.resource_type]
			when ["facebook", "Song"]
				share_song_on_facebook(s, msg)
			
			when ["facebook", "Playlist"]
				share_playlist_on_facebook(s, msg)
				
			when ["twitter", "Song"]
				share_song_on_twitter(s, msg)
			
			when ["twitter", "Playlist"]
				share_playlist_on_twitter(s, msg)		
			end
		rescue Exception => e
			logger.debug "error sharing #{s.resource.display} on #{provider}"
			catch_error(s, e)
		end
	end
	
	# Let the service know that the user has started to play a song.
	def start_listen(l)
		return unless listen?
		
		begin
			case provider
			when "facebook"
				start_listen_on_facebook(l)
			when "lastfm"
				start_listen_on_lastfm(l)
			end
		rescue Exception => e
			logger.debug "error starting listen on service: #{provider}"
			catch_error(l, e)
		end
	end
	
	# Let the service know that the user has paused, resumed, or otherwise updated a currently active song.
	def update_listen(l)
		return unless listen?
		
		begin
			case provider
			when "facebook"
				update_listen_on_facebook(l)
			end
		rescue Exception => e
			logger.debug "error updating listen on service: #{provider}"
			catch_error(l, e)
		end
	end
	
	# Let the service know that the user has stopped listen to a song.
	def end_listen(l)
		return unless listen?
		
		begin
			case provider
			when "lastfm"
				end_listen_on_lastfm(l)
			when "facebook"
				end_listen_on_facebook(l)
			end
		rescue Exception => e
			logger.debug "error ending listen on service: #{provider}"
			catch_error(l, e)
		end
	end
	
	# Remove that a song was listened to on the service.
	#
	# Used when the song was skipped or not played for long enough.
	def delete_listen(l)
		return unless listen?
		
		begin
			case provider
			when "facebook"
				delete_listen_on_facebook(l)
			end
		rescue Exception => e
			logger.debug "error deleting listen from service: #{provider}"
			logger.debug e.inspect
		end
	end
	
	# Let the service know that the user made a donation.
	def donate(d)
		return unless donate?
		
		begin
			case provider
			when "facebook"
				show_donation_on_facebook(d)
				
			when "twitter"
				show_donation_on_twitter(d)
			end
		rescue Exception => e
			logger.debug "error sharing donation on service: #{provider}"
			catch_error(d, e)
		end
	end
	
	# Let the service know that the user just created a playlist.
	def create_playlist(p)
		return unless create_playlist?
		
		begin
			case provider
			when "facebook"
				create_playlist_on_facebook(p)
			end
		rescue Exception => e
			logger.debug "error creating playlist on service: #{provider}"
			catch_error(p, e)
		end
	end
	
	# Let the service know that the user just updated a playlist.
	def update_playlist(p)
		return unless create_playlist?
		
		begin
			case provider
			when "facebook"
				update_playlist_on_facebook(p)
			end
		rescue Exception => e
			logger.debug "error updating playlist on service: #{provider}"
			catch_error(p, e)
		end
	end
	
	# Let the service know that the user just removed a playlist.
	def delete_playlist(p)
		return unless create_playlist?
		
		begin
			case provider
			when "facebook"
				delete_playlist_on_facebook(p)
			end
		rescue Exception => e
			logger.debug "error deleting playlist on service: #{provider}"
			logger.debug e.inspect
		end
	end
	
	# Get an encrypted ID of the user at the third party.
	#
	# Currently used by Facebook to allow link to the user profile in open graph tags.
	def fetch_encrypted_uid
		begin
			case provider
			when "facebook"
				fetch_encrypted_facebook_uid
			end
		rescue Exception => e
			logger.debug "error fetching encrypted uid from service: #{provider}"
			logger.debug e.inspect
		end
	end
	
	# The string to display to users for representing the resource.
	def display
		Link.pretty_name(provider)
	end
	
	# The URL where the user should browse to initiate a new link to this service.
	def connectURL
		"http://beta.stoffiplayer.com/auth/#{provider}"
	end
	
	# The display name of a given provider.
	def self.pretty_name(provider)
		case provider
		when "google_oauth2" then "Google"
		when "linked_in" then "LinkedIn"
		when "soundcloud" then "SoundCloud"
		when "lastfm" then "Last.fm"
		when "myspace" then "MySpace"
		when "linkedin" then "LinkedIn"
		when "vkontakte" then "vKontakte"
		when "windowslive" then "Live"
		else provider.titleize
		end
	end
	
	# The options to use when the link is serialized.
	def serialize_options
		{
			:except =>
			[
				:access_token_secret, :access_token, :uid, :created_at,
				:refresh_token, :token_expires_at, :user_id, :updated_at
			],
			:methods =>
			[
				:kind, :display, :url, :connectURL,
				:can_button, :can_share, :can_listen, :can_donate, :can_create_playlist,
				:name, :error
			]
		}
	end
	
	private

	include Links::Facebook
	include Links::Twitter
	include Links::Lastfm
	
	# Send an authorized GET request to the service.
	def get(path, params = {})
		request(path, :get, params)
	end
	
	# Send an authorized POST request to the service.
	def post(path, params = {})
		request(path, :post, params)
	end
	
	# Send an authorized DELETE request to the service.
	def delete(path)
		request(path, :delete)
	end
	
	# Send an authorized request to the service.
	def request(path, method = :get, params = {})
	
		# google uses a refresh_token
		# we need to request a new access_token using the
		# refresh_token if it has expired
		if provider == "google_oauth2" and refresh_token and token_expires_at < DateTime.now
			http = Net::HTTP.new("accounts.google.com", 443)
			http.use_ssl = true
			res, data = http.post("/o/oauth2/token", 
			{
				:refresh_token => refresh_token,
				:client_id => creds[:id],
				:client_secret => creds[:key],
				:grant_type => 'refresh_token'
			}.map { |k,v| "#{k}=#{v}" }.join('&'))
			response = JSON.parse(data)
			exp = response['expires_in'].seconds.from_now
			update_attributes({ :token_expires_at => exp, :access_token => response['access_token']})
		end
		
		if provider == "twitter"
			client = OAuth::Consumer.new(creds[:id], creds[:key],
				{
					:site => creds[:url],
					:ssl => {:ca_path => "/etc/ssl/certs"},
					:scheme => :header
				})
			token_hash = {
				:oauth_token => access_token,
				:oauth_token_secret => access_token_secret
			}
			token = OAuth::AccessToken.from_hash(client, token_hash)
			logger.debug params.inspect
			resp = token.request(method, creds[:url] + path, params[:params])
			#raise "fooo: " + resp.body.to_s
			return JSON.parse(resp.body)
		else
			client = OAuth2::Client.new(creds[:id], creds[:key], :site => creds[:url], :ssl => {:ca_path => "/etc/ssl/certs"})
			token = OAuth2::AccessToken.new(client, access_token, :header_format => "OAuth %s")
	
			case method
			when :get
				return token.get(path).parsed
			
			when :post
				return token.post(path, params).parsed
		
			when :delete
				return token.delete(path).parsed
			end
		end
	end
	
	def catch_error(resource, exception)
		begin
			splitted = exception.message.split("\n")
			json = splitted[1]
			error = JSON.parse json
			message = error['error']['message']
			logger.debug "catching error: #{message}"
			logger.debug message
			if provider == "facebook"
				if message.start_with? "Session has expired at unix time" or
				   message.start_with? "Error validating access token:" or
				   message == "The session has been invalidated because the user has changed the password."
				   bl = self.backlogs.new
				   bl.resource = resource
				   bl.error = message
				   bl.save
				end
			end
		rescue
			logger.debug "failed to catch error: #{exception.message}"
		end
	end
	
	# The API credentials for Stoffi to authenticate with the service.
	def creds
		Stoffi::Application::OA_CRED[provider.to_sym]
	end
end
