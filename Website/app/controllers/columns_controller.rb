# -*- encoding : utf-8 -*-
class ColumnsController < ApplicationController

	before_filter :login_or_oauth_required
	
	# GET /columns
	# GET /columns.xml
	# GET /columns.json
	# GET /columns.yaml
	def index
		@columns = current_user.columns.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @columns }
			format.json { render :json => @columns }
			format.yaml { render :text => @columns.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /columns/1
	# GET /columns/1.xml
	# GET /columns/1.json
	# GET /columns/1.yaml
	def show
		@column = current_user.columns.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @column }
			format.json { render :json => @column }
			format.yaml { render :text => @column.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /columns/new
	# GET /columns/new.xml
	# GET /columns/new.json
	# GET /columns/new.yaml
	def new
		@column = current_user.columns.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @column }
			format.json { render :json => @column }
			format.yaml { render :text => @column.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /columns/1/edit
	def edit
		@column = current_user.columns.find(params[:id])
	end

	# POST /columns
	# POST /columns.xml
	# POST /columns.json
	# POST /columns.yaml
	def create
		@column = current_user.columns.new(params[:column])

		respond_to do |format|
			if @column.save
				format.html { redirect_to(@column, :notice => 'Column was successfully created.') }
				format.xml  { render :xml => @column, :status => :created, :location => @column }
				format.json { render :json => @column, :status => :created, :location => @column }
				format.yaml { render :text => @column.to_yaml, :content_type => 'text/yaml', :status => :created, :location => @column }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @column.errors, :status => :unprocessable_entity }
				format.json { render :json => @column.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @column.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# PUT /columns/1
	# PUT /columns/1.xml
	# PUT /columns/1.json
	# PUT /columns/1.yaml
	def update
		@column = current_user.columns.find(params[:id])

		respond_to do |format|
			if @column.update_attributes(params[:column])
				format.html { redirect_to(@column, :notice => 'Column was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
				format.yaml { render :text => "", :content_type => 'text/yaml' }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @column.errors, :status => :unprocessable_entity }
				format.json { render :json => @column.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @column.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /columns/1
	# DELETE /columns/1.xml
	# DELETE /columns/1.json
	# DELETE /columns/1.yaml
	def destroy
		@column = current_user.columns.find(params[:id])
		@column.destroy

		respond_to do |format|
			format.html { redirect_to(columns_url) }
			format.xml  { head :ok }
			format.json { head :ok }
			format.yaml { render :text => "", :content_type => 'text/yaml' }
		end
	end
end
