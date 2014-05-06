# -*- encoding : utf-8 -*-
class ListConfigsController < ApplicationController

	before_filter :login_or_oauth_required
	
	# GET /list_configs
	# GET /list_configs.xml
	# GET /list_configs.json
	# GET /list_configs.yaml
	def index
		@list_configs = current_user.list_configs.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @list_configs }
			format.json { render :json => @list_configs }
			format.yaml { render :text => @list_configs.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /list_configs/1
	# GET /list_configs/1.xml
	# GET /list_configs/1.json
	# GET /list_configs/1.yaml
	def show
		@list_config = current_user.list_configs.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @list_config }
			format.json { render :json => @list_config }
			format.yaml { render :text => @list_config.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /list_configs/new
	# GET /list_configs/new.xml
	# GET /list_configs/new.json
	# GET /list_configs/new.yaml
	def new
		@list_config = ListConfig.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @list_config }
			format.json { render :json => @list_config }
			format.yaml { render :text => @list_config.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /list_configs/1/edit
	def edit
		@list_config = ListConfig.find(params[:id])
	end

	# POST /list_configs
	# POST /list_configs.xml
	# POST /list_configs.json
	# POST /list_configs.yaml
	def create
		@list_config = ListConfig.new(params[:list_config])

		respond_to do |format|
			if @list_config.save
				format.html { redirect_to(@list_config, :notice => 'ListConfig was successfully created.') }
				format.xml  { render :xml => @list_config, :status => :created, :location => @list_config }
				format.json { render :json => @list_config, :status => :created, :location => @list_config }
				format.yaml { render :text => @list_config.to_yaml, :content_type => 'text/yaml', :status => :created, :location => @list_config }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @list_config.errors, :status => :unprocessable_entity }
				format.json { render :json => @list_config.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @list_config.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# PUT /list_configs/1
	# PUT /list_configs/1.xml
	# PUT /list_configs/1.json
	# PUT /list_configs/1.yaml
	def update
		@list_config = ListConfig.find(params[:id])

		respond_to do |format|
			if @list_config.update_attributes(params[:list_config])
				format.html { redirect_to(@list_config, :notice => 'ListConfig was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
				format.yaml { render :text => "", :content_type => 'text/yaml' }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @list_config.errors, :status => :unprocessable_entity }
				format.json { render :json => @list_config.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @list_config.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /list_configs/1
	# DELETE /list_configs/1.xml
	# DELETE /list_configs/1.json
	# DELETE /list_configs/1.yaml
	def destroy
		@list_config = ListConfig.find(params[:id])
		@list_config.destroy

		respond_to do |format|
			format.html { redirect_to(list_configs_url) }
			format.xml  { head :ok }
			format.json { head :ok }
			format.yaml { render :text => "", :content_type => 'text/yaml' }
		end
	end
end
