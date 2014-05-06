# -*- encoding : utf-8 -*-
# The model of the user resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describes a user account in the database.
class User < ActiveRecord::Base
	include Base
	
	# Include default devise modules. Others available are:
	# :token_authenticatable, :encryptable, :confirmable, :omniauthable, :timeoutable and 
	devise :database_authenticatable, :registerable, 
	       :recoverable, :rememberable, :trackable, :validatable, :lockable
		 
	has_many :links, :dependent => :destroy
	has_many :devices, :dependent => :destroy
	has_many :sources, :dependent => :destroy
	has_many :configurations, :dependent => :destroy
	has_many :playlists, :dependent => :destroy
	has_many :shares, :dependent => :destroy
	has_many :listens, :dependent => :destroy
	has_many :votes, :dependent => :destroy
	has_many :translations
	has_many :donations
	has_many :apps, :class_name => "ClientApplication", :dependent => :destroy
	has_many :tokens, :class_name => "OauthToken", :order => "authorized_at desc", :include => [:client_application], :dependent => :destroy
	has_and_belongs_to_many :songs, :uniq => true
	has_and_belongs_to_many :playlist_subscriptions, :class_name => "Playlist", :join_table => "playlist_subscribers", :uniq => true

	# Setup accessible (or protected) attributes for your model
	attr_accessible :email, :password, :password_confirmation, :remember_me, :unique_token, :id,
		:name_source, :custom_name, :image, :show_ads, :has_password
	
	# The name of the user.
	#
	# Will get the name of the user by either:
	# # Pull the name from a linked account if the user has choosen such a name source.
	# # Return a custom name if the user has provided one.
	# # Look at the email.
	# # Return a default name.
	def name
		s = name_source.to_s
		
		return User.default_name if email.to_s == "" and s == "" and custom_name.to_s == ""
		return email.split('@')[0].titleize if s == "" and custom_name.to_s == ""
		return custom_name if s == ""
		
		p,v = s.split("::",2)
		return s unless ["twitter","facebook","google_oauth2","lastfm","vimeo"].include? p
		l = self.links.find_by_provider(p)
		return User.default_name unless l
		names = l.names
		return User.default_name unless (names.is_a?(Hash) and v and names[v.to_sym])
		names[v.to_sym]
	end
	
	# The default picture of a user.
	def self.default_pic(size)
		size = "_#{size.to_s}" unless size.to_s.empty?
		"/assets/gfx/user#{size}.png"
	end
	
	# The default name of a user.
	def self.default_name
		"Anon"
	end
	
	# The unique hash of a user.
	#
	# This is used to identify a unique communication channel for real time communication.
	def unique_hash
		if self.unique_token.blank?
			update_attribute(:unique_token, Devise.friendly_token[0,50].to_s)
		end
		Digest::SHA2.hexdigest(self.unique_token + id.to_s)
	end
	
	# The amount of fan points the user has gotten.
	def points(artist = nil)
		w = artist ? " AND artist_id = #{artist.id}" : ""
		w = "status != 'returned' AND status != 'failed' AND status != 'revoked'#{w}"
		if artist
			l = artist.listens.where("user_id = ?", id).count
		else
			l = listens.count
		end
		d = donations.where(w).sum(:amount)
		
		return l + (d * 1000)
	end
	
	# The picture of the user.
	#
	# Will get the picture of the user by either:
	# # Pull the picture from a linked account if the user has choosen such a picture source.
	# # Return a default picture.
	def picture(options)
		size = ""
		size = options[:size] if options != nil
		s = image.to_s
		return User.default_pic(size) if s == ""
		if [:gravatar, :identicon, :monsterid, :wavatar, :retro].include? s.to_sym
			s = s == :gravatar ? :mm : s.to_sym
			return gravatar(s)
		else
			l = self.links.find_by_provider(s)
			return User.default_pic unless l
			pic = l.picture
			return User.default_pic unless pic
			return pic
		end
		return User.default_pic(size)
	end
	
	# Returns all apps of a user.
	#
	# <tt>scope</tt> can be:
	# :added:: Apps that the user has allowed access to his/her account.
	# :created:: Apps that was created by the user.
	def get_apps(scope)
		case scope
		when :added
			return ClientApplication.select("client_applications.*").
			joins(:oauth_tokens).
			where("oauth_tokens.invalidated_at is null and oauth_tokens.authorized_at is not null and oauth_tokens.type = 'AccessToken'").
			where("client_applications.id = oauth_tokens.client_application_id and oauth_tokens.user_id = #{id}").
			group("client_applications.id").
			order("oauth_tokens.authorized_at DESC")
		
		when :created
			return apps
		end
	end
	
	# Whether or not the user is an administrator.
	def is_admin?
		self.admin
	end
	
	# The URL to the user's profile.
	def url
		"http://beta.stoffiplayer.com/profile/#{id}"
	end
	
	# The gravatar of the user.
	def gravatar(type)
		gravatar_id = Digest::MD5.hexdigest(email.to_s.downcase)
		force = type == :mm ? "" : "&f=y"
		"https://gravatar.com/avatar/#{gravatar_id}.png?s=48&d=#{type}#{force}"
	end
	
	# Looks for, and creates if necessary, the user based on an authentication with a third party service.
	def self.find_or_create_with_omniauth(auth)
		link = Link.find_by_provider_and_uid(auth['provider'], auth['uid'])
		user = User.find_by_email(auth['info']['email']) if auth['info']['email']
		
		# link found
		if link
			d = auth['credentials']['expires_at']
			d = DateTime.strptime("#{d}",'%s') if d
			link.update_attributes(
				:access_token => auth['credentials']['token'],
				:access_token_secret => auth['credentials']['secret'],
				:refresh_token => auth['credentials']['refresh_token'],
				:token_expires_at => d
			)
			return link.user
		
		# email already registrered, create link for that user
		elsif auth['info'] && auth['info']['email'] && user
			user.create_link(auth)
			return user
		
		# create a new user and a link for that user
		else
			return create_with_omniauth(auth)
		end
	end

	# Creates an account by using an authentication to a third party service.
	def self.create_with_omniauth(auth)
		email = auth['info']['email']
		pass = Devise.friendly_token[0,20]
	
		# create user
		user = User.new(
			:email => email,
			:password => pass,
			:password_confirmation => pass,
			:has_password => false
		)
		user.save(:validate => false)
		
		# create link
		user.create_link(auth)
		
		return user
	end
	
	# Updates the user resource.
	#
	# If a password change is occuring then the current password
	# will be required unless the user has not already set a
	# password (in case the accounts was created using an authentication
	# with a third party service).
	def update_with_password(params={})
		current_password = params.delete(:current_password) if !params[:current_password].blank?
		
		if params[:password].blank?
			params.delete(:password)
			params.delete(:password_confirmation) if params[:password_confirmation].blank?
		end
		
		result = if has_no_password? || valid_password?(current_password)
			r = update_attributes(params)
			update_attribute(:has_password, true) unless params[:password].to_s.blank?
			r
		else
			self.errors.add(:current_password, current_password.blank? ? :blank : :invalid)
			self.attributes = params
			false
		end
		
		clean_up_passwords
		result
	end
	
	# Whether or not the user has no password set.
	#
	# This happens when the account was created using an account at a third party.
	def has_no_password?
		!self.has_password
	end
	
	# The amount of charity that the donations of the user has generated.
	def charity_sum
		donations.sum("amount * (charity_percentage / 100)").to_f.round(2)
	end
	
	# The donations by the user which are either pending or completed.
	def donated
		donations.where("status != 'returned' AND status != 'failed' AND status != 'revoked'")
	end
	
	# The total amount of pending or completed donations.
	def donated_sum
		donated.sum(:amount).to_f.round(2)
	end
	
	# Returns a top list of users.
	#
	# The argument <tt>type</tt> can be:
	#
	# :supporters:: The users whom have donated the most amount.
	def self.top(limit = 5, type = :supporters)
		
		case type
		when :supporters
			self.select("users.id, users.name_source, users.custom_name, users.image, users.email, sum(donations.amount) AS c").
			joins(:donations).
			where("donations.status != 'returned' AND donations.status != 'failed' AND donations.status != 'revoked'").
			group("users.id").
			order("c DESC")
		else
			raise "Unsupported type"
		end
	end
	
	# The string to display to users for representing the resource.
	def display
		name
	end
	
	# Creates a link to a third party service given an omniauth hash.
	def create_link(auth)
		exp = auth['credentials']['expires_at']
		exp = DateTime.strptime("#{exp}",'%s') if exp
		links.create(
			:provider => auth['provider'],
			:uid => auth['uid'],
			:access_token => auth['credentials']['token'],
			:access_token_secret => auth['credentials']['secret'],
			:refresh_token => auth['credentials']['refresh_token'],
			:token_expires_at => exp
		)
	end
	
	# Fetches the encrypted user ID of the user on a third party.
	#
	# This is used by Facebook to provide the ability to specify
	# the link to the user's profile on Facebook in OpenGraph.
	def encrypted_uid(provider)
		links.each do |link|
			return link.encrypted_uid if link.provider == provider
		end
		return nil
	end
	
	# The options to use when the user is serialized.
	def serialize_options
		{
			:except =>
			[
				:has_password, :created_at, :unique_token, :updated_at, :custom_name,
				:admin, :show_ads, :name_source, :image, :email
				
			],
			:methods => [ :kind, :display, :url ]
		}
	end
end
