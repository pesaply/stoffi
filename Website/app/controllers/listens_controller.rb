# -*- encoding : utf-8 -*-
# The business logic for listens to songs.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class ListensController < ApplicationController

	oauthenticate
	respond_to :html, :mobile, :embedded, :xml, :json
	
	# GET /listens
	def index
		l, o = pagination_params
		@listen = current_user.listens.new
		@listens = current_user.listens.limit(l).offset(o)
		respond_with @listens
	end
	
	# GET /listens/by/1
	def by
		l, o = pagination_params
		params[:user_id] = process_me(params[:user_id])
		id = logged_in? ? current_user.id : -1
		@user = User.find(params[:user_id])
		
		if current_user != nil and params[:user_id] == current_user.id
			@listens = current_user.listens.limit(l).offset(o)
		else
			@listens = Listen.where("user_id = ? AND is_public = 1", params[:user_id]).limit(l).offset(o)
		end
		
		@channels = ["user_#{id}"]
		
		respond_with(@listens)
	end

	# GET /listens/1
	def show
		@listen = current_user.listens.find(params[:id])
		respond_with @listen
	end

	# GET /listens/new
	def new
		@listen = current_user.listens.new
		respond_with @listen
	end

	# GET /listens/1/edit
	def edit
		@listen = current_user.listens.find(params[:id])
	end

	# POST /listens
	def create
		@listen = current_user.listens.new(params[:listen])
		
		playlist = Playlist.get(current_user, params[:playlist]) if not params[:playlist].to_s.empty?
		artist   = Artist.get(params[:track][:artist])           if params[:track] and not params[:track][:artist].to_s.empty?
		album    = Album.get(params[:album])                     if not params[:album].to_s.empty?
		song = Song.get(current_user,
		{
			:title => params[:track][:title],
			:path => params[:track][:path],
			:length => params[:track][:length],
			:foreign_url => params[:track][:foreign_url],
			:art_url => params[:track][:art_url],
			:genre => params[:track][:genre]
		}) if params[:track]
		
		album.songs << song if song && album && album.songs.find_by_id(song.id) == nil
		artist.songs << song if song && artist && artist.songs.find_by_id(song.id) == nil
		artist.albums << album if artist && album && artist.albums.find_by_id(album.id) == nil
		
		@listen.song = song unless @listen.song
		@listen.playlist = playlist if playlist
		@listen.album = album if album
		@listen.device = @current_device
		
		@listen.playlist.songs << @listen.song if @listen.playlist
		current_user.songs << @listen.song
		
		@listen.started_at = Time.now unless @listen.started_at
		@listen.ended_at = @listen.started_at + @listen.song.length unless @listen.ended_at
		
		success = @listen.save
		
		if success
			SyncController.send('create', @listen, request)
			current_user.links.each { |link| link.start_listen(@listen) }
		end

		respond_with @listen
	end

	# PUT /listens/1
	def update
		@listen = current_user.listens.find(params[:id])
		success = @listen.update_attributes(params[:listen])
		if success
			SyncController.send('update', @listen, request)
			if params[:listen].key? :ended_at
				current_user.links.each { |link| link.update_listen(@listen) }
			end
		end
		respond_with @listen
	end

	# DELETE /listens/1
	def destroy
		@listen = current_user.listens.find(params[:id])
		current_user.links.each { |link| link.delete_listen(@listen) }
		SyncController.send('delete', @listen, request)
		@listen.destroy
		respond_with @listen
	end

	# POST /listens/1/end
	def end
		@listen = current_user.listens.find(params[:id])
		@listen.update_attribute(:ended_at, Time.now)
		current_user.links.each { |link| link.end_listen(@listen) }
		respond_with @listen
	end
end
