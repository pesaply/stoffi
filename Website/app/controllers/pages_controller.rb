# -*- encoding : utf-8 -*-
# The business logic for the main pages of the website (non-resources).
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class PagesController < ApplicationController
	oauthenticate :only => [ :remote, :foo ]
	
	before_filter :set_title_and_description, :except => :search
	respond_to :html, :mobile, :embedded, :json, :xml
	
	def foo
		params = {
			:artist => "Afasi & Filthy",
			:track => {
				:title => "Jobb",
				:length => "236.99",
				:path => "03-jobb-pBk.mp3"
			}
		}
		artist = Artist.get(params[:artist])
		song = Song.get(current_user,
		{
			:title => params[:track][:title],
			:path => params[:track][:path],
			:length => params[:track][:length],
			:foreign_url => params[:track][:foreign_url],
			:art_url => params[:track][:art_url],
			:genre => params[:track][:genre]
		})
		if song && artist && artist.songs.find_by_id(song.id) == nil
			render :text => "add" and return
		else
			render :text => "no add" and return
		end
		#render :text => "#{artist.class} #{song.class}" and return
		#dups = []
		#Song.all.each do |song|
		#	Song.all.each do |s|
		#		if not dups.include?(s.id) and song.id != s.id and song.path == s.path and song.path.start_with? "stoffi:track:"
		#			dups << s.id
		#		end
		#	end
		#end
		#txt = ""
		#dups.each do |dup|
		#	song = Song.find(dup)
		#	User.all.each { |u| u.songs.delete(song) }
		#	Artist.all.each { |u| u.songs.delete(song) }
		#	Playlist.all.each { |u| u.songs.delete(song) }
		#	song.delete
		#end
		#render :text => txt
	end

	def old
		render :layout => false
	end
	
	def index
		redirect_to dashboard_url if params[:format] == :embedded
	end

	def news
	end

	def get
	end

	def download
		params[:channel] = "stable" unless params[:channel]
		params[:arch] = "32" unless params[:arch]
		@type = params[:type] || "installer"
		
		unless ["alpha", "beta", "stable"].include? params[:channel]
			redirect_to "/get" and return
		end
		
		unless ["32", "64"].include? params[:arch]
			redirect_to "/get" and return
		end
		
		unless ["installer", "checksum"].include? @type
			redirect_to "/get" and return
		end
		
		filename = "InstallStoffi"
		filename = "InstallStoffiAlpha" if params[:channel] == "alpha"
		filename = "InstallStoffiBeta" if params[:channel] == "beta"
		
		filename += "AndDotNet" if params[:fat] && params[:fat] == "1"
		
		@fname = filename
		
		filename += case @type
			when "checksum" then ".sum"
			else ".exe"
		end
		
		@file = "/downloads/" + params[:channel] + "/" + params[:arch] + "bit/" + filename
		@autodownload = @type == "installer"
	end

	def tour
		redirect_to :action => :index and return if params[:format] == "mobile"
	end

	def about
	end

	def contact
	end

	def legal
	end

	def money
	end

	def history
		redirect_to "http://dev.stoffiplayer.com/wiki/History"
	end

	def remote
	
		if current_user and current_user.configurations.count > 0 and current_user.configurations.first.devices.count > 0
			@configuration = current_user.configurations.first
			
			@devices = @configuration.devices.order(:name)
		end
		
		@title = t("remote.title")
		@description = t("remote.description")
		
		render "configurations/show", :layout => (params[:format] != "mobile" ? true : 'empty')
	end

	def language
		respond_to do |format|
			format.html { redirect_to root_url }
			format.mobile { render }
		end
	end

	def donate
		logger.info "redirecting donate shortcut"
		respond_with do |format|
			format.html { redirect_to donations_url, :flash => flash }
			format.mobile { redirect_to new_donation_url, :flash => flash }
		end
	end
  
	def mail				
		if !params[:name] or params[:name].length < 2
				flash[:error] = t("contact.errors.name")
				render :action => 'contact'
				
		elsif !params[:email] or params[:email].match(/^([a-z0-9_.\-]+)@([a-z0-9\-.]+)\.([a-z.]+)$/i).nil?
				flash[:error] = t("contact.errors.email")
				render :action => 'contact'
				
		elsif !params[:subject] or params[:subject].length < 4
				flash[:error] = t("contact.errors.subject") 
				render :action => 'contact'
				
		elsif !params[:message] or params[:message].length < 20
				flash[:error] = t("contact.errors.message")
				render :action => 'contact'

		elsif !verify_recaptcha
			flash[:error] = t("contact.errors.captcha")
			render :action => 'contact'
			
		else
			Mailer.contact(:domain => "beta.stoffiplayer.com",
						   :subject => params[:subject],
						   :from => params[:email],
						   :name => params[:name],
						   :message => params[:message]).deliver
			redirect_to :action => 'contact', :sent => 'success'
		end
	end

	def facebook
		render :layout => "facebook"
	end

	def channel
		render :layout => false
	end
	
	def search
		redirect_to :action => :index and return if params[:format] == "mobile"
	
		# get query parameter
		params[:q] = params[:term] if params[:q] == nil && params[:term]
		params[:q] = "" unless params[:q]
		params[:q] = CGI::escapeHTML(params[:q])
		
		# get category parameter
		params[:c] = params[:categories] if params[:c] == nil && params[:categories]
		params[:c] = params[:category] if params[:c] == nil && params[:category]
		params[:c] = "artists|songs|devices|playlists" unless params[:c]
		c = params[:c].split('|')
		
		# get limit parameter
		params[:l] = params[:limit] if params[:l] == nil && params[:limit]
		params[:l] = '5' unless params[:l]
		l = params[:l].to_i
		l = 50 if l > 50
		
		@result = Array.new
		
		if c.include? 'artists'
			@exact_artist = Artist.find_by_name(params[:q])
			Artist.search(params[:q]).limit(l).each do |i|
				@result.push(
				{
					:url => i.url,
					:display => i.display,
					:category => t("search.categories.artists"),
					:desc => t("plays", :count => i.listens_count),
					:id => i.id,
					:field => "artist_#{i.id}",
					:kind => i.kind
				})
			end
		end
		
		if c.include? 'songs'
			@exact_song = Song.find_by_title(params[:q])
		
			# get source parameter
			params[:s] = params[:sources] if params[:s] == nil && params[:sources]
			params[:s] = params[:source] if params[:s] == nil && params[:source]
			params[:s] = "files|youtube|soundcloud" unless params[:s]
			
			if params[:s]
				s = params[:s].split('|')
				q = params[:q].gsub(/ /, "+")
		
				if s.include? 'files'
					cat = t("search.categories.songs")
					cat = t("search.categories.files") if c.length == 1
					Song.search_files(params[:q]).limit(l).each do |i|
						begin
							@result.push(
							{
								:url => i.url,
								:path => i.path,
								:display => i.display,
								:title => i.title,
								:genre => i.genre == nil ? "" : i.genre,
								:album => i.album == nil ? "" : i.album.name,
								:artist => i.artist == nil ? "" : i.artist.name,
								:category => cat,
								:desc => "",
								:id => i.id,
								:length => i.length,
								:picture => i.art_url,
								:field => "song_#{i.id}",
								:icon => "/assets/gfx/icons/file.ico",
								:kind => i.kind
							})
						rescue
						end
					end
				end
				
				if s.include? 'soundcloud'
					cat = t("search.categories.songs")
					cat = t("search.categories.soundcloud") if c.length == 1
					id = "2ad7603ebaa9cd252eabd8dd293e9c40"
					
					tracks = https_get("https://api.soundcloud.com/tracks.json?client_id=#{id}&limit=#{l}&q=#{q}")
					tracks.each do |track|
						begin
							artist, title = Song.parse_title(track['title'])
							artist = track['user']['username'] unless artist
							@result.push(
							{
								:url => track['permalink_url'],
								:title => title,
								:artist => artist,
								:length => track['duration'],
								:genre => track['genre'],
								:path => "stoffi:track:soundcloud:#{track['id']}",
								:album => "",
								:display => title,
								:category => cat,
								:picture => track['artwork_url'],
								:desc => artist,
								:field => "song_soundcloud_#{track['id']}",
								:icon => "/assets/gfx/icons/soundcloud.png",
								:kind => "song"
							})
						rescue
						end
					end
				end
				
				if s.include? 'youtube'
					cat = t("search.categories.songs")
					cat = t("search.categories.youtube") if c.length == 1
					feed = https_get("https://gdata.youtube.com/feeds/api/videos?max-results=#{l}&category=Music&alt=json&v=2&q=#{q}")
					feed['feed']['entry'].each do |entry|
					
						begin
							artist, title = Song.parse_title(entry['title']['$t'])
							artist = feed['entry']['author']['name']['$t'] unless artist
							id = entry['media$group']['yt$videoid']['$t']
						
							@result.push(
							{
								:url => "https://www.youtube.com/watch?v=#{id}",
								:path => "stoffi:track:youtube:#{id}",
								:album => "",
								:display => entry['title']['$t'],
								:title => title,
								:artist => artist,
								:length => entry['media$group']['yt$duration']['seconds'],
								:category => cat,
								:picture => entry['media$group']['media$thumbnail'][0]['url'],
								:desc => artist,
								:field => "song_youtube_#{id}",
								:icon => "/assets/gfx/icons/youtube.gif",
								:kind => "song"
							})
						rescue
						end
					end
				end
			
			else
				Song.search(params[:q]).limit(l).each do |i|
					begin
						@result.push(
						{
							:url => i.url,
							:display => i.display,
							:category => t("search.categories.songs"),
							:picture => i.picture,
							:path => i.path,
							:desc => "",
							:id => i.id,
							:field => "song_#{i.id}",
							:kind => i.kind
						})
					rescue
					end
				end
			end
		end
		
		if c.include? 'devices' and signed_in?
			@exact_device = current_user.devices.find_by_name(params[:q])
			current_user.devices.search(params[:q]).limit(l).each do |i|
				begin
					@result.push(
					{
						:url => i.url,
						:display => i.display,
						:category => t("search.categories.devices"),
						:desc => "",
						:id => i.id,
						:field => "device_#{i.id}",
						:kind => i.kind
					})
				rescue
				end
			end
		end
		
		if c.include? 'playlists'
			@exact_playlist = current_user.playlists.find_by_name(params[:q]) if signed_in?
			@exact_playlist = Playlist.find_by_name_and_is_public(params[:q], true) unless @exact_playlist
			Playlist.search(current_user, params[:q]).limit(l).each do |i|
				begin
					@result.push(
					{
						:url => i.url,
						:display => i.display,
						:category => t("search.categories.playlists"),
						:desc => "",
						:id => i.id,
						:field => "device_#{i.id}",
						:kind => i.kind
					})
				rescue
				end
			end
		end
		
		if c.include? 'users'
			#User.search(params[:q]).limit(5).each do |i|
			#	@result.push(
			#	{
			#		:url => i.url,
			#		:display => i.display,
			#		:category => t("search.categories.users"),
			#		:desc => "",
			#		:kind => i.kind
			#	})
			#end
		end
		
		@title = e(params[:q])
		@description = t("index.description")
		
		respond_with(@result)
	end
	
	private
	
	def set_title_and_description
		@title = t("#{action_name}.title")
		@description = t("#{action_name}.description")
	end

end
