# -*- encoding : utf-8 -*-
# The business logic for shares.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class SharesController < ApplicationController

	oauthenticate :only => [ :index, :show, :new, :create ]
	oauthenticate :interactive => true, :only => [ :edit, :update, :destroy ]
	respond_to :html, :mobile, :embedded, :xml, :json
	
	# GET /shares
	def index
		l, o = pagination_params
		@share = current_user.shares.new
		@shares = current_user.shares.limit(l).offset(o)
		respond_with @shares
	end
	
	# GET /shares/by/1
	def by
		l, o = pagination_params
		params[:user_id] = process_me(params[:user_id])
		id = logged_in? ? current_user.id : -1
		@user = User.find(params[:user_id])
		
		if current_user != nil and params[:user_id] == current_user.id
			@shares = current_user.shares.limit(l).offset(o)
		else
			@shares = Share.where("user_id = ? AND is_public = 1", params[:user_id]).limit(l).offset(o)
		end
		
		@channels = []
		@shares.each { |s| @channels << "user_#{s.user_id}" }
		
		respond_with(@shares)
	end

	# GET /share
	def show
		@share = current_user.shares.new
		@shares = current_user.shares.all
		respond_with @share
	end

	# GET /shares/new
	def new
		@share = current_user.shares.new
		respond_with @share
	end

	# GET /shares/1/edit
	def edit
		@share = current_user.shares.find(params[:id])
	end

	# POST /shares
	def create
		@share = current_user.shares.new(params[:share])
		@share.resource_type = params[:object] if params[:object]
		
		playlist = Playlist.get(current_user, params[:playlist]) if params[:playlist].to_s != ""
		artist   = Artist.get(params[:artist])                   if params[:artist].to_s != ""
		album    = Album.get(params[:album])                     if params[:album].to_s != ""
		song = Song.get(current_user,
		{
			:title => params[:track][:title],
			:path => params[:track][:path],
			:length => params[:track][:length],
			:foreign_url => params[:track][:foreign_url],
			:art_url => params[:track][:art_url],
			:genre => params[:track][:genre],
			:artist => params[:track][:artist],
			:album => params[:track][:album]
		}) if params[:track]
		
		playlist.songs << song if song && playlist && playlist.songs.find_by_id(song.id) == nil
		@share.playlist = playlist
		@share.resource_id = @share.resource_type == "song" ? song.id : playlist.id 
		@share.device = @current_device
		
		current_user.links.each do |link|
			link.share(@share)
		end

		success = @share.save

		SyncController.send('create', @share, request) if success
		respond_with @share
	end

	# PUT /shares/1
	def update
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
		@share = current_user.shares.find(params[:id])
		success = @share.update_attributes(params[:share])
		SyncController.send('update', @share, request) if success
		respond_with @share
	end

	# DELETE /shares/1
	def destroy
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
		@share = current_user.shares.find(params[:id])
		SyncController.send('delete', @share, request)
		@share.destroy
		respond_with @share
	end
end
