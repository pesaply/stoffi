# -*- encoding : utf-8 -*-
class SourcesController < ApplicationController

	before_filter :login_or_oauth_required
	
	# GET /sources
	# GET /sources.xml
	# GET /sources.json
	# GET /sources.yaml
	def index
		@sources = current_user.sources.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @sources }
			format.json { render :json => @sources }
			format.yaml { render :text => @sources.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /sources/1
	# GET /sources/1.xml
	# GET /sources/1.json
	# GET /sources/1.yaml
	def show
		@source = current_user.sources.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @source }
			format.json { render :json => @source }
			format.yaml { render :text => @source.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /sources/new
	# GET /sources/new.xml
	# GET /sources/new.json
	# GET /sources/new.yaml
	def new
		@source = current_user.sources.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @source }
			format.json { render :json => @source }
			format.yaml { render :text => @source.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /sources/1/edit
	def edit
		@source = current_user.sources.find(params[:id])
	end

	# POST /sources
	# POST /sources.xml
	# POST /sources.json
	# POST /sources.yaml
	def create
		@source = current_user.sources.new(params[:source])

		respond_to do |format|
			if @source.save
				format.html { redirect_to(@source, :notice => 'Source was successfully created.') }
				format.xml  { render :xml => @source, :status => :created, :location => @source }
				format.json { render :json => @source, :status => :created, :location => @source }
				format.yaml { render :text => @source.to_yaml, :content_type => 'text/yaml', :status => :created, :location => @source }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @source.errors, :status => :unprocessable_entity }
				format.json { render :json => @source.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @source.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# PUT /sources/1
	# PUT /sources/1.xml
	# PUT /sources/1.json
	# PUT /sources/1.yaml
	def update
		@source = current_user.sources.find(params[:id])

		respond_to do |format|
			if @source.update_attributes(params[:source])
				format.html { redirect_to(@source, :notice => 'Source was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
				format.yaml { render :text => "", :content_type => 'text/yaml' }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @source.errors, :status => :unprocessable_entity }
				format.json { render :json => @source.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @source.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /sources/1
	# DELETE /sources/1.xml
	# DELETE /sources/1.json
	# DELETE /sources/1.yaml
	def destroy
		@source = current_user.sources.find(params[:id])
		@source.destroy

		respond_to do |format|
			format.html { redirect_to(sources_url) }
			format.xml  { head :ok }
			format.json { head :ok }
			format.yaml { render :text => "", :content_type => 'text/yaml' }
		end
	end
end
