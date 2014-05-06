# -*- encoding : utf-8 -*-
class KeyboardShortcutProfilesController < ApplicationController

	before_filter :login_or_oauth_required
	
	# GET /keyboard_shortcut_profiles
	# GET /keyboard_shortcut_profiles.xml
	# GET /keyboard_shortcut_profiles.json
	# GET /keyboard_shortcut_profiles.yaml
	def index
		@keyboard_shortcut_profiles = current_user.keyboard_shortcut_profiles.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @keyboard_shortcut_profiles }
			format.json { render :json => @keyboard_shortcut_profiles }
			format.yaml { render :text => @keyboard_shortcut_profiles.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /keyboard_shortcut_profiles/1
	# GET /keyboard_shortcut_profiles/1.xml
	# GET /keyboard_shortcut_profiles/1.json
	# GET /keyboard_shortcut_profiles/1.yaml
	def show
		@keyboard_shortcut_profile = current_user.keyboard_shortcut_profiles.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @keyboard_shortcut_profile }
			format.json { render :json => @keyboard_shortcut_profile }
			format.yaml { render :text => @keyboard_shortcut_profile.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /keyboard_shortcut_profiles/new
	# GET /keyboard_shortcut_profiles/new.xml
	# GET /keyboard_shortcut_profiles/new.json
	# GET /keyboard_shortcut_profiles/new.yaml
	def new
		@keyboard_shortcut_profile = current_user.keyboard_shortcut_profiles.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @keyboard_shortcut_profile }
			format.json { render :json => @keyboard_shortcut_profile }
			format.yaml { render :text => @keyboard_shortcut_profile.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /keyboard_shortcut_profiles/1/edit
	def edit
		@keyboard_shortcut_profile = current_user.keyboard_shortcut_profiles.find(params[:id])
	end

	# POST /keyboard_shortcut_profiles
	# POST /keyboard_shortcut_profiles.xml
	# POST /keyboard_shortcut_profiles.json
	# POST /keyboard_shortcut_profiles.yaml
	def create
		@keyboard_shortcut_profile = current_user.keyboard_shortcut_profiles.new(params[:keyboard_shortcut_profile])

		respond_to do |format|
			if @keyboard_shortcut_profile.save
				format.html { redirect_to(@keyboard_shortcut_profile, :notice => 'Keyboard shortcut profile was successfully created.') }
				format.xml  { render :xml => @keyboard_shortcut_profile, :status => :created, :location => @keyboard_shortcut_profile }
				format.json { render :json => @keyboard_shortcut_profile, :status => :created, :location => @keyboard_shortcut_profile }
				format.yaml { render :text => @keyboard_shortcut_profile.to_yaml, :content_type => 'text/yaml', :status => :created, :location => @keyboard_shortcut_profile }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @keyboard_shortcut_profile.errors, :status => :unprocessable_entity }
				format.json { render :json => @keyboard_shortcut_profile.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @keyboard_shortcut_profile.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# PUT /keyboard_shortcut_profiles/1
	# PUT /keyboard_shortcut_profiles/1.xml
	# PUT /keyboard_shortcut_profiles/1.json
	# PUT /keyboard_shortcut_profiles/1.yaml
	def update
		@keyboard_shortcut_profile = current_user.keyboard_shortcut_profiles.find(params[:id])

		respond_to do |format|
			if @keyboard_shortcut_profile.update_attributes(params[:keyboard_shortcut_profile])
				format.html { redirect_to(@keyboard_shortcut_profile, :notice => 'Keyboard shortcut profile was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
				format.yaml { render :text => "", :content_type => 'text/yaml' }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @keyboard_shortcut_profile.errors, :status => :unprocessable_entity }
				format.json { render :json => @keyboard_shortcut_profile.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @keyboard_shortcut_profile.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /keyboard_shortcut_profiles/1
	# DELETE /keyboard_shortcut_profiles/1.xml
	# DELETE /keyboard_shortcut_profiles/1.json
	# DELETE /keyboard_shortcut_profiles/1.yaml
	def destroy
		@keyboard_shortcut_profile = current_user.keyboard_shortcut_profiles.find(params[:id])
		@keyboard_shortcut_profile.destroy

		respond_to do |format|
			format.html { redirect_to(keyboard_shortcut_profiles_url) }
			format.xml  { head :ok }
			format.json { head :ok }
			format.yaml { render :text => "", :content_type => 'text/yaml' }
		end
	end
end
