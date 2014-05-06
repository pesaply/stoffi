# -*- encoding : utf-8 -*-
class EqualizerProfilesController < ApplicationController

	before_filter :login_or_oauth_required
	
	# GET /equalizer_profiles
	# GET /equalizer_profiles.xml
	# GET /equalizer_profiles.json
	# GET /equalizer_profiles.yaml
	def index
		@equalizer_profiles = current_user.equalizer_profiles.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @equalizer_profiles }
			format.json { render :json => @equalizer_profiles }
			format.yaml { render :text => @equalizer_profiles.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /equalizer_profiles/1
	# GET /equalizer_profiles/1.xml
	# GET /equalizer_profiles/1.json
	# GET /equalizer_profiles/1.yaml
	def show
		@equalizer_profile = current_user.equalizer_profiles.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @equalizer_profile }
			format.json { render :json => @equalizer_profile }
			format.yaml { render :text => @equalizer_profile.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /equalizer_profiles/new
	# GET /equalizer_profiles/new.xml
	# GET /equalizer_profiles/new.json
	# GET /equalizer_profiles/new.yaml
	def new
		@equalizer_profile = current_user.equalizer_profiles.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @equalizer_profile }
			format.json { render :json => @equalizer_profile }
			format.yaml { render :text => @equalizer_profile.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /equalizer_profiles/1/edit
	def edit
		@equalizer_profile = current_user.equalizer_profiles.find(params[:id])
	end

	# POST /equalizer_profiles
	# POST /equalizer_profiles.xml
	# POST /equalizer_profiles.json
	# POST /equalizer_profiles.yaml
	def create
		@equalizer_profile = current_user.equalizer_profiles.new(params[:equalizer_profile])

		respond_to do |format|
			if @equalizer_profile.save
				format.html { redirect_to(@equalizer_profile, :notice => 'Equalizer profile was successfully created.') }
				format.xml  { render :xml => @equalizer_profile, :status => :created, :location => @equalizer_profile }
				format.json { render :json => @equalizer_profile, :status => :created, :location => @equalizer_profile }
				format.yaml { render :text => @equalizer_profile.to_yaml, :content_type => 'text/yaml', :status => :created, :location => @equalizer_profile }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @equalizer_profile.errors, :status => :unprocessable_entity }
				format.json { render :json => @equalizer_profile.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @equalizer_profile.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# PUT /equalizer_profiles/1
	# PUT /equalizer_profiles/1.xml
	# PUT /equalizer_profiles/1.json
	# PUT /equalizer_profiles/1.yaml
	def update
		@equalizer_profile = current_user.equalizer_profiles.find(params[:id])

		respond_to do |format|
			if @equalizer_profile.update_attributes(params[:equalizer_profile])
				format.html { redirect_to(@equalizer_profile, :notice => 'Equalizer profile was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
				format.yaml { render :text => "", :content_type => 'text/yaml' }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @equalizer_profile.errors, :status => :unprocessable_entity }
				format.json { render :json => @equalizer_profile.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @equalizer_profile.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /equalizer_profiles/1
	# DELETE /equalizer_profiles/1.xml
	# DELETE /equalizer_profiles/1.json
	# DELETE /equalizer_profiles/1.yaml
	def destroy
		@equalizer_profile = current_user.equalizer_profiles.find(params[:id])
		@equalizer_profile.destroy

		respond_to do |format|
			format.html { redirect_to(equalizer_profiles_url) }
			format.xml  { head :ok }
			format.json { head :ok }
			format.yaml { render :text => "", :content_type => 'text/yaml' }
		end
	end
end
