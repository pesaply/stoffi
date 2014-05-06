# -*- encoding : utf-8 -*-
# The business logic for song albums.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)
 
class AlbumsController < ApplicationController

	oauthenticate :interactive => true, :except => [ :index, :show ]
	respond_to :html, :mobile, :embedded, :xml, :json
	
	# GET /albums
	def index
		l, o = pagination_params
		respond_with(@albums = Album.limit(l).offset(o))
	end
	
	# GET /albums/by/1
	def by
		l, o = pagination_params
		@artist = Artist.find(params[:artist_id])
		@albums = @artist.albums.limit(l).offset(o)
		respond_with(@albums)
	end

	# GET /albums/1
	def show
		l, o = pagination_params
		@album = Album.find(params[:id])
		@album.paginate_songs(l, o)
		respond_with(@album, :methods => [ :paginated_songs ])
	end

	# GET /albums/new
	def new
		respond_with(@album = Album.new)
	end

	# GET /albums/1/edit
	def edit
		@album = Album.find(params[:id])
	end

	# POST /albums
	def create
		@album = Album.new(params[:album])
		respond_with @album
	end

	# PUT /albums/1
	def update
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
		@album = Album.find(params[:id])
		@album.update_attributes(params[:album])
		respond_with @album
	end

	# DELETE /albums/1
	def destroy
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
		@album = Album.find(params[:id])
		@album.destroy
		respond_with @album
	end
end
