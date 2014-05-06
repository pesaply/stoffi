# -*- encoding : utf-8 -*-
# The business logic for artists.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class ArtistsController < ApplicationController

	oauthenticate :interactive => true, :except => [ :index, :show ]
	before_filter :ensure_admin, :except => [ :index, :show ]
	respond_to :html, :mobile, :embedded, :xml, :json
	
	# GET /artists
	def index
		l, o = pagination_params
		respond_with(@artists = Artist.limit(l).offset(o))
	end

	# GET /artists/1
	def show
		l, o = pagination_params
		@artist = Artist.find(params[:id])
		@title = @artist.name
		@description = t "artist.description", :artist => d(@artist.name)
		@head_prefix = "og: http://ogp.me/ns# fb: http://ogp.me/ns/fb# stoffiplayer: http://ogp.me/ns/fb/stoffiplayer#"
		@meta_tags =
		[
			{ :property => "og:title", :content => d(@artist.name) },
			{ :property => "og:type", :content => "stoffiplayer:artist" },
			{ :property => "og:image", :content => @artist.picture },
			{ :property => "og:url", :content => @artist.url },
			{ :property => "og:site_name", :content => "Stoffi" },
			{ :property => "fb:app_id", :content => "243125052401100" },
			{ :property => "og:description", :content => t("artist.short_description", :artist => d(@artist.name)) },
			{ :property => "stoffiplayer:donations", :content => @artist.donations.count },
			{ :property => "stoffiplayer:support_generated", :content => "$#{@artist.donated_sum}" },
			{ :property => "stoffiplayer:charity_generated", :content => "$#{@artist.charity_sum}" }
		]
		
		@donations = @artist.donations

		@artist.paginate_songs(l, o)
		respond_with(@artist, :methods => [ :paginated_songs ])
	end

	# GET /artists/new
	def new
		respond_with(@artist = Artist.new)
	end

	# GET /artists/1/edit
	def edit
		@artist = Artist.find(params[:id])
	end

	# POST /artists
	def create
		if params[:artist] and params[:artist][:name]
			params[:artist][:name] = h(params[:artist][:name])
			@artist = Artist.find_by_name(params[:artist][:name])
		end
		
		if @artist
			@artist.update_attributes(params[:artist])
		else
			@artist = Artist.new(params[:artist])
			@artist.save
		end
		
		respond_with(@artist)
	end

	# PUT /artists/1
	def update
		@artist = Artist.find(params[:id])
		
		if params[:donation_update]
			@artist.donations.each do |donation|
				unless donation.update_attributes(params[:donation])
					respond_to do |format|
						format.html { render :action => "edit" }
						format.xml  { render :xml => donation.errors, :status => :unprocessable_entity }
						format.json { render :json => donation.errors, :status => :unprocessable_entity }
						format.yaml { render :text => donation.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
					end and return
				end
			end
			result = true
		else
			result = @artist.update_attributes(params[:artist])
		end
		
		respond_with(@artist)
	end

	# DELETE /artists/1
	def destroy
		render :status => :forbidden and return if ["xml","json"].include?(params[:format])
		@artist = Artist.find(params[:id])
		@artist.destroy
		respond_with(@artist)
	end
end
