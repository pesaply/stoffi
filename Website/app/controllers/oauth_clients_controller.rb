# -*- encoding : utf-8 -*-
# The business logic for apps with OAuth access.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class OauthClientsController < ApplicationController
	oauthenticate :only => [:index, :show]
	oauthenticate :interactive => true
	before_filter :get_app, :only => [:edit, :update, :destroy]
	before_filter :no_mobile
	respond_to :html, :mobile, :xml, :json
	
	# GET /apps
	def index
		@created = current_user.get_apps(:created)
		@added = current_user.get_apps(:added)
		@rest = ClientApplication.not_added_by(current_user)
		
		@tokens = {}
		@added.each do |app|
			token = current_user.tokens.
				where("client_application_id = ? and "+
					"invalidated_at is null and "+
					"authorized_at is not null and "+
					"type = 'AccessToken'", app.id).first
			
			@tokens[app.id] = token
		end
		
		@title = t "apps.title"
		@description = t "apps.description"
		respond_with ClientApplication.all
	end

	# GET /apps/new
	def new
		@app = ClientApplication.new
		@title = t "apps.title"
		@description = t "apps.description"
	end

	# POST /apps
	def create
		@app = current_user.apps.build(params[:app])
		respond_with(@app)
	end

	# GET /apps/1
	def show
		@app = ClientApplication.find_by_id(params[:id])
		@channels = @app.user == nil ? [] : ["user_"+@app.user.id]
		@title = @app.name
		@description = t "apps.description"
		respond_with @app
	end

	# GET /apps/1/edit
	def edit
		@title = @app.name
		@description = t "apps.description"
	end

	# PUT /apps/1
	def update
		@app.update_attributes(params[:app])
		respond_with(@app)
	end

	# DELETE /apps/1
	def destroy
		@app.destroy
		respond_with(@app)
	end

	# GET /apps/1/revoke
	def revoke
		@app = ClientApplication.find_by_id(params[:id])
		tokens = current_user.tokens.find_all_by_client_application_id(@app.id)
		
		tokens.each do |t|
			t.delete
		end
		
		respond_with(@app) do |format|
			format.html { redirect_to(apps_url) }
			format.xml  { head :ok }
			format.json { head :ok }
		end
	end

	private
	def get_app
		unless @app = current_user.apps.find(params[:id])
			flash.now[:error] = "Wrong application id"
			raise ActiveRecord::RecordNotFound
		end
	end
	
	def no_mobile
		redirect_to root_url and return if params[:format] == "mobile"
	end
end
