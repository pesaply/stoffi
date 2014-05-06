# -*- encoding : utf-8 -*-
class ColumnSortsController < ApplicationController

	before_filter :login_or_oauth_required
	
	# GET /column_sorts
	# GET /column_sorts.xml
	# GET /column_sorts.json
	# GET /column_sorts.yaml
	def index
		@column_sorts = current_user.column_sorts.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @column_sorts }
			format.json { render :json => @column_sorts }
			format.yaml { render :text => @column_sorts.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /column_sorts/1
	# GET /column_sorts/1.xml
	# GET /column_sorts/1.json
	# GET /column_sorts/1.yaml
	def show
		@column_sort = current_user.column_sorts.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @column_sort }
			format.json { render :json => @column_sort }
			format.yaml { render :text => @column_sort.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /column_sorts/new
	# GET /column_sorts/new.xml
	# GET /column_sorts/new.json
	# GET /column_sorts/new.yaml
	def new
		@column_sort = current_user.column_sorts.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @column_sort }
			format.json { render :json => @column_sort }
			format.yaml { render :text => @column_sort.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /column_sorts/1/edit
	def edit
		@column_sort = current_user.column_sorts.find(params[:id])
	end

	# POST /column_sorts
	# POST /column_sorts.xml
	# POST /column_sorts.json
	# POST /column_sorts.yaml
	def create
		@column_sort = current_user.column_sorts.new(params[:column_sort])

		respond_to do |format|
			if @column_sort.save
				format.html { redirect_to(@column_sort, :notice => 'Column sort was successfully created.') }
				format.xml  { render :xml => @column_sort, :status => :created, :location => @column_sort }
				format.json { render :json => @column_sort, :status => :created, :location => @column_sort }
				format.yaml { render :text => @column_sort.to_yaml, :content_type => 'text/yaml', :status => :created, :location => @column_sort }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @column_sort.errors, :status => :unprocessable_entity }
				format.json { render :json => @column_sort.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @column_sort.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# PUT /column_sorts/1
	# PUT /column_sorts/1.xml
	# PUT /column_sorts/1.json
	# PUT /column_sorts/1.yaml
	def update
		@column_sort = current_user.column_sorts.find(params[:id])

		respond_to do |format|
			if @column_sort.update_attributes(params[:column_sort])
				format.html { redirect_to(@column_sort, :notice => 'Column sort was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
				format.yaml { render :text => "", :content_type => 'text/yaml' }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @column_sort.errors, :status => :unprocessable_entity }
				format.json { render :json => @column_sort.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @column_sort.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /column_sorts/1
	# DELETE /column_sorts/1.xml
	# DELETE /column_sorts/1.json
	# DELETE /column_sorts/1.yaml
	def destroy
		@column_sort = current_user.column_sorts.find(params[:id])
		@column_sort.destroy

		respond_to do |format|
			format.html { redirect_to(column_sorts_url) }
			format.xml  { head :ok }
			format.json { head :ok }
			format.yaml { render :text => "", :content_type => 'text/yaml' }
		end
	end
end
