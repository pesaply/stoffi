# -*- encoding : utf-8 -*-
# The business logic for the web app.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class ApplicationController < ActionController::Base
	# we need number_to_currency for our title formatting
	include ActionView::Helpers::NumberHelper
	
	#require 'juggernaut' # do we need this after rails 3 upgrade?
	require 'geoip'
	
	# prevent csrf
	#TODO: the API stops working with this :(
	#protect_from_forgery
	
	before_filter :auth_with_params,
	              :check_device,
	              :ensure_proper_oauth,
	              :restrict_admin,
	              :set_tab,
	              :classify_device,
	              :prepare_for_mobile,
	              :prepare_for_embedded,
	              :reload_locales,
	              :set_locale,
	              :check_tracking,
	              :set_config,
	              :check_old_browsers

	# sets the configuration for the website
	def set_config
		@site_config = Admin::Config.first
		
	end
	
	# Reload the cached translations if we are running beta/alpha
	def reload_locales
		I18n.reload! if Rails.env.development?
	end
	
	def owns(resource, id)
		o = resource.find_by_id(id)
		return false if current_user == nil or o == nil
		return false unless o.user == current_user
		return true
	end
	
	def not_found(resource)
		@resource = resource
		render file: "#{Rails.root}/public/404", status: :not_found, :formats => [:html, :mobile]
	end
	
	# authenticate a user if the correct parameters are given
	# we need this to let applications login using their saved
	# tokens.
	#
	# We should require HTTPS for this one (in case we turn it
	# off on the whole site.
	def auth_with_params
		if params[:oauth_token] && params[:oauth_secret_token]
			t = params[:oauth_token]
			s = params[:oauth_secret_token]
			token = OauthToken.find_by_token_and_secret(t, s)
			if token
				sign_in(token.user)
			else
				redirect_to login_path
			end
		end
	end
	
	# check if the device ID header or param is present and
	# set @current_device accordingly.
	def check_device
		[
			params[:device_id],
			request.env['HTTP_X_DEVICE_ID']
		].each do |f|
			logger.info "checking for device id"
			if not f.to_s.empty?
				logger.info "found device id: " + f.to_s
				begin
					@current_device = Device.find(f)
					@current_device.poke(current_client_application, request.ip)
					logger.debug "ip: #{request.ip}"
					logger.info "@current_device set"
					return
				rescue Exception => e
					logger.warn "could not load device with ID #{f}: " + e.message
				end
			end
		end
	end
	
	# we require each oauth request to carry a device_id
	# parameter so we can keep track of devices.
	def ensure_proper_oauth
		if oauth?.is_a?(AccessToken)
				
			# /devices and /me OK
			if controller_name != "devices" &&
				!(
					controller_name == "registrations" &&
					action_name == "show" &&
					!(params[:id] && params[:id] != "me")
				) &&
				!(
					controller_name == "playlists" &&
					(action_name == "index" || action_name == "show" || action_name == "by")
				)
				if params[:device_id]
					begin
						@current_device = Device.find(params[:device_id])
						@current_device.poke(current_client_application, request.ip)
					rescue
						error = { :message => "Invalid device ID. It either doesn't exist or is not owned by current user.", :code => 2 }
					end
				else
					error = { :message => "Missing device ID. Every request must have a device_id parameter.", :code => 1 }
				end
			end
			if error
				logger.debug "returning error: #{error[:message]}"
				respond_to do |format|
					format.xml  { render :xml => error, :status => :unprocessable_entity }
					format.json { render :json => error, :status => :unprocessable_entity }
					format.yaml { render :text => error.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
				end
				return
			end
		end
	end
	
	# Decode a string for presentation.
	def self.d(str)
		return str unless str.is_a? String
		
		next_str = HTMLEntities.new.decode(str)
		while (next_str != str)
			str = next_str
			next_str = HTMLEntities.new.decode(str)
		end
		return next_str
	end
	def d(str)
		self.class.d(str)
	end
	helper_method :d
	
	# Encodes a string so it can be stored and transmitted.
	#
	# The string is first decoded until it doesn't change
	# anything and then a single encoding is performed.
	def self.e(str)
		return unless str
		
		if str.is_a? String
			str = HTMLEntities.new.encode(d(str), :decimal)
			str.gsub!(/[']/, "&#39;")
			str.gsub!(/["]/, "&#34;")
			str.gsub!(/[\\]/, "&#92;")
			return str
		end
		
		return str.map { |s| e(s) } if str.is_a?(Array)
		return str.each { |a,b| str[a] = e(b) } if str.is_a?(Hash)
		return str
	end
	def e(str)
		self.class.e(str)
	end
	helper_method :e
	
	def user_type
		return "Admin" if admin?
		return "Member" if user_signed_in?
		return "Visitor"
	end
	helper_method :user_type
	
	helper_method :admin?
	def admin?
		user_signed_in? && current_user.is_admin?
	end
	
	# make sure that non-admins cannot access the admin namespace
	def restrict_admin
		klass = self.class.name
		unless klass.index("::").nil?
			ns = klass.split("::").first.downcase
			ensure_admin if ns == "admin"
		end
	end
	
	def ensure_admin
		access_denied unless current_user && current_user.is_admin?
	end
	
	def access_denied
		if user_signed_in?
			redirect_to dashboard_url
			
		else
			if [:json, :xml].include? request.format.to_sym
				format = request.format.to_sym.to_s
				error = { :message => "authentication error", :code => 401 }
				self.status = 401
				self.content_type = request.format
				self.response_body = eval("error.to_#{format}")
			
			else
				session["user_return_to"] = request.url
				redirect_to login_url
			end
		end
	end
	
	def authenticate_user!(opts={})
		access_denied unless user_signed_in?
	end
	
	# map stuff between what oauth-plugin expects and what Devise gives:
	alias :logged_in? :user_signed_in?
	alias :login_required :authenticate_user!
	
	def current_user=(user)
		sign_in(:user, user)
	end
	
	def current_user
		user = super
		return current_token.user unless user or not current_token
		user
	end
	
	def process_me(id)
		if (!id || id == "me")
			redirect_to login_path and return unless current_user || current_token
			logger.debug "processing special user 'me'"
			user = current_user || current_token.user
			return user.id if user
		end
		return id
	end
		
	def default_url_options(options={})
		{ :l => base_path }
	end
	
	def verify_authenticity_token
		v = verified_request?
		o = oauth?
		logger.debug "verify_authenticity_token is passed: verified=#{v} || oauth=#{o}\n"
		v || o || raise(ActionController::InvalidAuthenticityToken)
	end
	
	private
	
	def after_sign_in_path_for(resource)
		set_locale
		saved = stored_location_for(resource)
		return saved if saved
		return dashboard_path(:format => params[:format], :l => base_path)
	end
	
	def after_sign_out_path_for(resource_or_scope)
		set_locale
		request.referer || login_path(:format => params[:format], :l => base_path)
	end
	
	def base_path
		return nil unless @parsed_locale
		return nil if ['us', '--'].include?(@parsed_locale.to_s)
		@parsed_locale.to_s
	end
	
	def set_locale
		@parsed_locale =
			extract_locale_from_param ||
			extract_locale_from_tld || 
			extract_locale_from_subdomain ||
			extract_locale_from_cookie ||
			extract_locale_from_accept_language_header ||
			extract_locale_from_ip

		@parsed_locale = @parsed_locale.to_sym if @parsed_locale
		@parsed_locale = :us if @parsed_locale == :en # no stupid :en! either :us or :uk, thanks.
		@parsed_locale = nil if @parsed_locale and not I18n.available_locales.include?(@parsed_locale)
		I18n.locale = @parsed_locale || I18n.default_locale
	end
	
	# Get locale code from parameter.
	# Sets a session cookie if parameter found.
	def extract_locale_from_param
		parsed_locale = params[:l] || params[:locale] || params[:i18n_locale]
		if parsed_locale && 
			cookies[:locale] = parsed_locale
			parsed_locale
		else
			nil
		end
	end
	
	# Get locale code from cookie.
	def extract_locale_from_cookie
		parsed_locale = cookies[:locale]
		if parsed_locale && I18n.available_locales.include?(parsed_locale.to_sym)
			parsed_locale
		else
			nil
		end
	end
	
	# Get locale from top-level domain or return nil if such locale is not available
	# You have to put something like:
	#   127.0.0.1 application.com
	#   127.0.0.1 application.it
	#   127.0.0.1 application.pl
	# in your /etc/hosts file to try this out locally
	def extract_locale_from_tld
		tld = request.host.split('.').last
		parsed_locale = case tld
			when 'hk' then 'cn'
			else tld
		end
		I18n.available_locales.include?(parsed_locale.to_sym) ? parsed_locale  : nil
	end
	
	# Get locale code from request subdomain (like http://it.application.local:3000)
	# You have to put something like:
	#   127.0.0.1 gr.application.local
	# in your /etc/hosts file to try this out locally
	def extract_locale_from_subdomain
		parsed_locale = request.subdomains.first
		return nil if parsed_locale == nil
		I18n.available_locales.include?(parsed_locale.to_sym) ? parsed_locale : nil
	end
	
	# Get locale code from reading the "Accept-Language" header
	def extract_locale_from_accept_language_header
		begin
			l = request.env['HTTP_ACCEPT_LANGUAGE']
			if l
				parsed_locale = l.scan(/^[a-z]{2}[-_]([a-zA-Z]{2})/).first.first.to_s.downcase
				if parsed_locale && I18n.available_locales.include?(parsed_locale.to_sym)
					return parsed_locale
				else
					return nil
				end
			end
		rescue
			return nil
		end
	end
	
	# Get locale code from looking up location of the IP
	def extract_locale_from_ip
		o = origin_country(request.remote_ip)
		if o
			parsed_locale = o.country_code2.downcase
			if parsed_locale && I18n.available_locales.include?(parsed_locale.to_sym)
				parsed_locale
			else
				nil
			end
		end
	end
	
	def check_tracking
		dnt = request.env['HTTP_DNT']
		@track = true
		@track = false if dnt == "1" && @browser != "ie"
	end
	
	# Gets the origin country of an IP
	def origin_country(ip)
		db = File.join(Rails.root, "lib", "assets", "GeoIP.dat")
		if File.exists? db
			GeoIP.new(db).country(ip)
		else
			nil
		end
	end
	helper_method :origin_country
	
	# Gets the origin city (and country) from an IP
	def origin_city(ip)
		db = File.join(Rails.root, "lib", "assets", "GeoLiteCity.dat")
		if File.exists? db
			GeoIP.new(db).city(ip)
		else
			nil
		end
	end
	helper_method :origin_city
	
	# Gets the origin network from an IP
	def origin_network(ip)
		db = File.join(Rails.root, "lib", "assets", "GeoIPASNum.dat")
		if File.exists? db
			GeoIP.new(db).asn(ip)
		else
			nil
		end
	end
	helper_method :origin_network
  
	def set_tab(controller = controller_name, action = action_name)
		@tab = ""
		if controller == "pages" and action == "download"
			@tab = "get"
		elsif controller == "pages"
			@tab = action
		elsif controller == "oauth_clients"
			@tab = "apps"
		elsif controller == "devices"
			@tab = "devices"
		elsif controller == "donations"
			@tab = "donations"
		elsif controller == "registrations" and action == "new"
			@tab = "join"
		elsif controller == "registrations" and (action == "edit" or action == "settings")
			@tab = "settings"
		elsif controller == "registrations"
			@tab = "dashboard"
		elsif controller == "contribute"
			@tab = "contribute"
		elsif controller == "sessions"
			@tab = "login"
		end
		@c = controller
		@a = action
	end
	
	def classify_device
		@ua = request.user_agent.to_s.downcase
		embedder = request.env['HTTP_X_EMBEDDER']
		begin
			embedder = embedder.split('/')
			@browser = embedder[0].downcase
			@embedder_version = embedder[1]
		rescue
			embedder = nil
		end
			
		if embedder.to_s == ""
			case @ua
			when /facebook/i
				@browser = "facebook"
			when /googlebot/i
				@browser = "google"
			when /chrome/i
				@browser = "chrome"
			when /opera/i
				@browser = "opera"
			when /firefox/i
				@browser = "firefox"
			when /safari/i
				@browser = "safari"
			when /msie/i
				@browser = "ie"
			else
				@browser = "unknown"
			end
		end

		case @ua
		when /windows nt 6.2/i
			@os = "windows 8"
		when /windows nt 6.1/i
			@os = "windows 7"
		when /windows phone/i
			@os = "windows phone"
		when /windows/i
			@os = "windows old"
		when /iphone/i
			@os = "iphone"
		when /android/i
			@os = "android"
		when /linux/i
			@os = "linux"
		when /mac/i
			@os = "mac"
		else
			@os = "unknown"
		end
	end
	
	def check_old_browsers
		return if cookies[:skip_old]
		return if mobile_device? or embedded_device?
		return if ["facebook","google"].include? @browser
		return if controller_name == "pages" and action_name == "old"
		return if @ua.include?("capybara-webkit")
		
		logger.info "ua: #{@ua}"
		logger.info "browser: #{@browser}"
		logger.info "os: #{@os}"
		logger.info "embedded_device? #{embedded_device?}"
		logger.info "mobile_device? #{mobile_device?}"
		
		if params[:dangerous]
			cookies[:skip_old] = "1"
		else
			begin
				old = case @browser
				when "ie"
					v = @ua.match(/ msie (\d+\.\d+)/)[1].to_i
					v < 9
					
				when "firefox"
					v = @ua.match(/ firefox\/(\d[\d\.]*\d)/)[1].to_i
					v < 10
					
				when "opera"
					ua = @ua.split
					ua.pop if ua[-1].match(/\[\w\w\]/)
					if ua[-1].start_with? "version/"
						v = ua[-1].split('/')[1].to_i
					elsif ua[-2] == "opera"
						v = ua[-1].to_i
					elsif ua[0].start_with? "opera/"
						v = ua[0].split('/')[1].to_i
					else
						v = 0
					end
					v < 10
					
				when "chrome"
					v = @ua.match(/ chrome\/(\d[\d\.]*\d) /)[1].to_i
					v < 10
					
				when "safari"
					m = @ua.match(/ version\/(\d[\d\.]*\d) /)
					if m
						v = m[1].to_i
					else
						v = 0
					end
					v < 5
					
				else
					false
				end
				
				if old
					logger.info "render warning of old browser instead of requested page"
					render("pages/old", :l => I18n.locale, :layout => false) and return
				end
			rescue
			end
		end
	end
	
	def mobile_device?
		if cookies[:mobile_param]
			cookies[:mobile_param] == "1"
		else
			request.user_agent =~ /Mobile|webOS/
		end
	end
	helper_method :mobile_device?
	
	def embedded_device?
		if cookies[:embedded_param]
			cookies[:embedded_param] == "1"
		else
			request.env['HTTP_X_EMBEDDER'] != nil or @ua.match(/ msie 7\.0;.* \.net4.0/)
		end
	end
	helper_method :embedded_device?
	
	def prepare_for_mobile
		cookies[:mobile_param] = params[:mobile] if params[:mobile]
		request.format = :mobile if mobile_device? && adaptable_format?
	end
	
	def prepare_for_embedded
		cookies[:embedded_param] = params[:embedded] if params[:embedded]
		request.format = :embedded if embedded_device? && adaptable_format?
	end
	
	def adaptable_format?
		request.format == :html || request.format.to_s == "*/*"
	end
	
	def pagination_params
		max_l = 50
		min_l = 1
		default_l = 25
		
		min_o = 0
		default_o = 0
		
		l = params[:limit] || default_l
		o = params[:offset] || default_o
		
		l = l.to_i
		o = o.to_i
		
		l = max_l if l > max_l
		l = min_l if l < min_l
		o = min_o if o < min_o
		
		return l, o
	end
	
	def https_get(url)
		json = {}
		uri = URI(url)
		Net::HTTP.start(uri.host, uri.port, :use_ssl => uri.scheme == 'https') do |http|
			request = Net::HTTP::Get.new uri.to_s
			res = http.request request
			data = res.body
			
			json = JSON.parse(data)
		end
		return json
	end
end

class String
	def possessive
		l = ""
		l = I18n.locale.to_s if I18n
		case l
		
		when 'se'
			self + case self[-1,1]
			when 's' then ""
			else "s"
			end
			
		else
			self + case self[-1,1]
			when 's' then "'"
			else "'s"
			end
		end
	end
	
	def downcase
		self.tr 'A-ZÅÄÖƐƆŊ', 'a-zåäöɛɔŋ'
	end
end

module ActionView
	module Helpers
		class FormBuilder
			
			def d(str)
				return str unless str.is_a? String
				
				next_str = HTMLEntities.new.decode(str)
				while (next_str != str)
					str = next_str
					next_str = HTMLEntities.new.decode(str)
				end
				return next_str
			end
			
			def decoded_text_field(method, options = {})
				options[:value] = d(@object[method])
				return text_field(method, options)
			end
		end
	end
end
