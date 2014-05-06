# -*- encoding : utf-8 -*-
# The business logic for songs.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class SongsController < ApplicationController

	oauthenticate :interactive => true, :except => [ :index, :show ]
	before_filter :ensure_admin, :except => [ :index, :show, :create, :destroy ]
	respond_to :html, :mobile, :embedded, :xml, :json
	
	# GET /songs
	def index
		l = params[:limit] || 25
		o = params[:offset] || 0
		if logged_in?
			@songs = current_user.songs.limit(l).offset(o)
		else
			@songs = Song.limit(l).offset(o)
		end

		respond_with(@songs)
	end

	# GET /songs/1
	def show
		not_found('song') and return unless Song.exists? params[:id]
		@song = Song.find(params[:id])
		@title = CGI.unescapeHTML(@song.pretty_name)
		if @song.artist
			@description = t "songs.description", :song => @song.title, :artist => @song.artist.name
		else
			@description = t "songs.description_without_artist", :song => @song.title
		end
		@head_prefix = "og: http://ogp.me/ns# fb: http://ogp.me/ns/fb# stoffiplayer: http://ogp.me/ns/fb/stoffiplayer#"
		@meta_tags =
		[
			{ :property => "og:title", :content => d(@song.title) },
			{ :property => "og:type", :content => "music.song" },
			{ :property => "og:image", :content => @song.picture },
			{ :property => "og:url", :content => @song.url },
			{ :property => "og:description", :content => d(@description) },
			{ :property => "og:site_name", :content => "Stoffi" },
			{ :property => "fb:app_id", :content => "243125052401100" },
			{ :property => "music:duration", :content => @song.length.to_i },
			{ :property => "og:audio", :content => @song.play },
			{ :property => "og:audio:type", :content => "audio/vnd.facebook.bridge" }
		] |
			@song.artists.map { |artist| { :property => "music:musician", :content => artist.url } } |
			@song.albums.map { |album| { :property => "music:album", :content => album.url } }

		respond_with(@song, :include => :artists)
	end

	# GET /songs/new
	def new
		redirect_to songs_path and return
		
		@title = "Add song"
		@description = "Add a new song to your collection"
		@song = current_user.songs.new

		respond_with(@song)
	end

	# GET /songs/1/edit
	def edit
		@song = current_user.songs.find(params[:id])
		redirect_to song_path(@song)
	end

	# POST /songs
	def create
		@song = Song.get_by_path(params[:song][:path])
		@song = current_user.songs.new(params[:song]) unless @song
		
		if current_user.songs.find_all_by_id(@song.id).count == 0
			current_user.songs << @song
		end
		
		@song.save
		respond_with(@song)
	end

	# PUT /songs/1
	def update
		@song = Song.find(params[:id])
		@song.update_attributes(params[:song])
		respond_with(@song)
	end

	# DELETE /songs/1
	def destroy
		@song = Song.find(params[:id])
		current_user.songs.delete(@song)
		
		unless @song.users.count > 0 || @song.youtube? || @song.soundcloud?
			@song.destroy
		end
		
		respond_with(@song)
	end
end
