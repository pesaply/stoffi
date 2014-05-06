# -*- encoding : utf-8 -*-
# The business logic for links to third party services.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class LinksController < ApplicationController

	oauthenticate :only => [:index, :show, :update, :destroy]
	oauthenticate :interactive => true, :only => [:edit]
	respond_to :html, :mobile, :embedded, :xml, :json
	
	# GET /links
	def index
		@links = current_user.links
		@new_links = Array.new
		Link.available.each do |link|
			n = link[:name]
			ln = link[:link_name] || n.downcase
			if current_user.links.find_by_provider(ln) == nil
				path = "#{request.protocol}#{request.host_with_port}/auth/#{ln}"
				@new_links <<
				{
					:display => n,
					:kind => :link,
					:url => path
				}
			end
		end
		
		@all = { :connected => @links, :not_connected => @new_links }
		
		respond_with @links do |format|
			format.html
			format.mobile
			format.embedded
			format.xml { render :xml => @all }
			format.json { render :json => @all }
		end
	end

	# GET /links/1
	def show
		@link = current_user.links.find(params[:id])
		respond_with @link
	end

	# GET /links/new
	def new
		@link = current_user.links.new
		respond_with @link
	end

	# GET /links/1/edit
	def edit
		@link = current_user.links.find(params[:id])
	end

	# POST /links
	def create
		render :status => :forbidden and return if ["xml","json"].include?(params[:format].to_s)
		auth = request.env["omniauth.auth"]
		
		if current_user != nil
			@link = current_user.links.find_by_provider_and_uid(auth['provider'], auth['uid'])
			if @link
				logger.debug "update link credentials"
				@link.update_credentials(auth)
			else
				@link = current_user.create_link(auth)
			end
		else
			@user = User.find_or_create_with_omniauth(auth)
			
			success = @user
			if success && @user.errors.empty?
				sign_in(:user, @user)
			end
			
			@link = @user.links.first if success
		end
		
		if @link
			SyncController.send('create', @link, request)
			@link.user.playlists.each do |playlist|
				@link.create_playlist(playlist)
			end
		end

		respond_with(@link) do |format|
			if @link
				format.html { redirect_to request.env['omniauth.origin'] || settings_path }
				format.mobile { redirect_to request.env['omniauth.origin'] || settings_path }
				format.embedded { redirect_to request.env['omniauth.origin'] || dashboard_path }
				format.xml  { render :xml => @link, :status => :created, :location => @link }
				format.json { render :json => @link, :status => :created, :location => @link }
			else
				format.html { redirect_to request.env['omniauth.origin'] || login_path }
				format.mobile { redirect_to request.env['omniauth.origin'] || login_path }
				format.embedded { redirect_to request.env['omniauth.origin'] || login_path }
				format.xml  { render :xml => @link.errors, :status => :unprocessable_entity }
				format.json { render :json => @link.errors, :status => :unprocessable_entity }
			end
		end
	end

	# PUT /links/1
	def update
		@link = current_user.links.find(params[:id])
		success = @link.update_attributes(params[:link])
		SyncController.send('update', @link, request) if success

		respond_with(@link) do |format|
			if success
				format.html { redirect_to(settings_path, :notice => 'Link was successfully updated.') }
				format.mobile { redirect_to(settings_path, :notice => 'Link was successfully updated.') }
				format.embedded { redirect_to(settings_path, :notice => 'Link was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
			else
				format.html { render :action => "edit" }
				format.mobile { render :action => "edit" }
				format.embedded { render :action => "edit" }
				format.xml  { render :xml => @link.errors, :status => :unprocessable_entity }
				format.json { render :json => @link.errors, :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /links/1
	def destroy
		@link = current_user.links.find(params[:id])
		SyncController.send('delete', @link, request)
			@link.user.playlists.each do |playlist|
				@link.delete_playlist(playlist)
			end
		@link.destroy

		respond_with(@link) do |format|
			format.html { redirect_to(settings_path) }
			format.mobile { redirect_to(settings_path) }
			format.embedded { redirect_to(settings_path) }
		end
	end
end
