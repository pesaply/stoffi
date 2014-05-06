# -*- encoding : utf-8 -*-
# The business logic for playlists.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class PlaylistsController < ApplicationController
	oauthenticate :except => [ :index, :show ]
	
	respond_to :html, :mobile, :embedded, :json, :xml
	
	# GET /playlists
	def index
		l, o = pagination_params
		id = logged_in? ? current_user.id : -1
		
		if id > 0
			@follows = current_user.playlist_subscriptions.limit(l).offset(o)
			@personal = current_user.playlists.limit(l).offset(o)
		end
		@global = Playlist.top(l, o)
		
		@channels = Array.new
		@global.each { |p| @channels << "user_#{p.user_id}" }
		
		@title = t "playlists.all.title"
		@description = t "playlists.all.description"
		
		respond_with(@global)
	end
	
	# GET /playlists/by/1
	def by
		l, o = pagination_params
		params[:user_id] = process_me(params[:user_id])
		follows = params[:follows] != nil && params[:follows] == "1"
		id = logged_in? ? current_user.id : -1
		
		@user = User.find(params[:user_id])
		
		if current_user != nil and params[:user_id] == current_user.id
			if follows
				@playlists = current_user.playlist_subscriptions.limit(l).offset(o)
			else
				@playlists = current_user.playlists.limit(l).offset(o)
			end
		else
			if follows
				@playlists = @user.playlist_subscriptions.limit(l).offset(o)
			else
				@playlists = @user.playlists.where(is_public: 1).limit(l).offset(o)
			end
		end
		
		@channels = ["user_#{id}"]
		
		@title = t "playlists.by.title", :username => @user.name.possessive
		@description = t "playlists.by.description", :username => @user.name
		
		respond_with(@playlists, :include => [ :songs ])
	end

	# GET /playlists/1
	def show
		not_found('playlist') and return unless Playlist.exists? params[:id]
		l, o = pagination_params
		@playlist = Playlist.find(params[:id])
		
		unless user_signed_in? && @playlist.user == current_user || @playlist.is_public
			access_denied and return
		end
		
		@channels = ["user_#{@playlist.user.id}"]
		@title = @playlist.name
		@description = t "playlist.description", :name => d(@playlist.name), :username => d(@playlist.user.name)
		
		t=0
		@head_prefix = "og: http://ogp.me/ns# fb: http://ogp.me/ns/fb# stoffiplayer: http://ogp.me/ns/fb/stoffiplayer#"
		@meta_tags =
		[
			{ :property => "og:title", :content => d(@playlist.name) },
			{ :property => "og:type", :content => "music.playlist" },
			{ :property => "og:image", :content => @playlist.picture },
			{ :property => "og:url", :content => playlist_url(@playlist) },
			{ :property => "og:description", :content => @description },
			{ :property => "og:audio", :content => playlist_url(@playlist, :protocol => "playlist") },
			{ :property => "og:audio:type", :content => "audio/vnd.facebook.bridge" },
			{ :property => "og:site_name", :content => "Stoffi" },
			{ :property => "fb:app_id", :content => "243125052401100" },
			{ :property => "music:creator", :content => profile_url(@playlist.user) },
		] |
			@playlist.songs.map { |song| { :property => "music:song", :content => song_url(song) } }
			
		@playlist.paginate_songs(l, o)
		respond_with(@playlist, :methods => [ :paginated_songs ])
	end

	# GET /playlists/new
	def new
		redirect_to :action => :index and return if params[:format] == "mobile"
		respond_with(@playlist = current_user.playlists.new)
	end

	# GET /playlists/1/edit
	def edit
		redirect_to :action => :show, :id => params[:id] and return if params[:format] == "mobile"
		not_found('playlist') and return unless owns(Playlist, params[:id])
		@playlist = current_user.playlists.find(params[:id])
		@title = "Edit #{@playlist.name}"
	end

	# POST /playlists
	def create
		exists = current_user.playlists.find_by_name(params[:playlist][:name]) != nil
		@playlist = Playlist.get(current_user, params[:playlist][:name])
		@playlist.assign_attributes(params[:playlist])
		@playlist.is_public = true # TODO: set default in db instead
		success = @playlist.save
		
		if success
		
			begin
				songs = params[:songs]
				unless songs
					body = request.body.read
					if body && body != ""
						begin
							songs = JSON.parse(body)
						rescue
						end
					end
				end
				
				if songs.is_a?(Array)
					songs.each do |track|
						song = Song.get(current_user,
						{
							:title => track['title'],
							:path => track['path'],
							:length => track['length'],
							:foreign_url => track['foreign_url'],
							:art_url => track['art_url'],
							:genre => track['genre'],
							:artist => track['artist'],
							:album => track['album']
						})
						if song and not @playlist.songs.find_by_id(song.id)
							@playlist.songs << song
						end
					end
				end
			rescue Exception => err
				logger.error "Could not add songs to playlist"
				logger.error err.message
				logger.error err.backtrace.inspect
			end
			SyncController.send('create', @playlist, request)
			if @playlist.is_public and @playlist.songs.count > 0
				current_user.links.each { |link|
					if exists
						link.update_playlist(@playlist)
					else
						link.create_playlist(@playlist)
					end
				}
			end
		end

		respond_with(@playlist)
	end

	# PUT /playlists/1
	def update
		not_found('playlist') and return unless owns(Playlist, params[:id])
		@playlist = current_user.playlists.find(params[:id])
		
		newProps = Hash.new
		newProps['songs'] = { 'added' => [], 'removed' => [] }
		
		songs = params[:songs]
		
		unless songs
			body = request.body.read
			if body && body != ""
				begin
					songs = JSON.parse(body)
				rescue
				end
			end
		end
		
		if songs
			logger.debug songs.to_yaml
			
			# make sure that params['playlist'] exists
			# so create_properties will run diff check
			params['playlist'] = Hash.new unless params['playlist']
			props = SyncController.create_properties(@playlist, params)
			
			# add songs
			if songs['added']
				songs['added'].each do |track|
					song = Song.get(current_user,
					{
						:title => track['title'],
						:path => track['path'],
						:length => track['length'],
						:foreign_url => track['foreign_url'],
						:art_url => track['art_url'],
						:genre => track['genre'],
						:artist => track['artist'],
						:album => track['album']
					})
					
					if song and @playlist.songs.find_by_id(song.id) == nil
						@playlist.songs << song
						props['songs']['added'] << song
					end
				end
			end
			
			# remove songs
			if songs['removed']
				songs['removed'].each do |track|
					song = Song.find(track['id']) if (track['id'] and not track['id'].starts_with?("tmp_"))
					song = Song.find_by_path(track['path']) if track['path'] and not song
					
					@playlist.songs.delete(song) if song
					
					props['songs']['removed'] << song if song
				end
			end
			
		end
		
		success = @playlist.update_attributes(params[:playlist])
		
		if success
			if not @playlist.is_public or @playlist.songs.count == 0
				current_user.links.each { |link| link.delete_playlist(@playlist) }
			elsif @playlist.songs.count > 0
				current_user.links.each { |link| link.update_playlist(@playlist) }
			end
			SyncController.send('update', @playlist, request, props)
		end

		respond_with(@playlist)
	end

	# DELETE /playlists/1
	def destroy
		not_found('playlist') and return unless Playlist.exists? params[:id]
		@playlist = Playlist.find(params[:id])
		
		# destroy playlist
		if @playlist.user == current_user
			SyncController.send('delete', @playlist, request)
			current_user.links.each { |link| link.delete_playlist(@playlist) }
			@playlist.destroy
			
		# unfollow playlist
		else
			SyncController.send_privately('delete', @playlist, request, current_user)
			@playlist.subscribers.delete current_user
		end
		respond_with(@playlist)
	end
	
	# PUT /playlists/1/follow
	def follow
		not_found('playlist') and return unless Playlist.exists? params[:id]
		@playlist = Playlist.find(params[:id])
		
		not_found('playlist') and return unless @playlist.is_public
		
		unless @playlist.subscribers.find_by_id(current_user.id)
			@playlist.subscribers << current_user
			SyncController.send('execute', @playlist, request, 'follow')
		end
		
		respond_with(@playlist)
	end
end
