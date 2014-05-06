# -*- encoding : utf-8 -*-
class KeyboardShortcutsController < ApplicationController

	before_filter :login_or_oauth_required
	
	# GET /keyboard_shortcuts
	# GET /keyboard_shortcuts.xml
	# GET /keyboard_shortcuts.json
	# GET /keyboard_shortcuts.yaml
	def index
		@keyboard_shortcuts = current_user.keyboard_shortcuts.all

		respond_to do |format|
			format.html # index.html.erb
			format.xml  { render :xml => @keyboard_shortcuts }
			format.json { render :json => @keyboard_shortcuts }
			format.yaml { render :text => @keyboard_shortcuts.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /keyboard_shortcuts/1
	# GET /keyboard_shortcuts/1.xml
	# GET /keyboard_shortcuts/1.json
	# GET /keyboard_shortcuts/1.yaml
	def show
		@keyboard_shortcut = current_user.keyboard_shortcuts.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.xml  { render :xml => @keyboard_shortcut }
			format.json { render :json => @keyboard_shortcut }
			format.yaml { render :text => @keyboard_shortcut.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /keyboard_shortcuts/new
	# GET /keyboard_shortcuts/new.xml
	# GET /keyboard_shortcuts/new.json
	# GET /keyboard_shortcuts/new.yaml
	def new
		@keyboard_shortcut = current_user.keyboard_shortcuts.new

		respond_to do |format|
			format.html # new.html.erb
			format.xml  { render :xml => @keyboard_shortcut }
			format.json { render :json => @keyboard_shortcut }
			format.yaml { render :text => @keyboard_shortcut.to_yaml, :content_type => 'text/yaml' }
		end
	end

	# GET /keyboard_shortcuts/1/edit
	def edit
		@keyboard_shortcut = current_user.keyboard_shortcuts.find(params[:id])
	end

	# POST /keyboard_shortcuts
	# POST /keyboard_shortcuts.xml
	# POST /keyboard_shortcuts.json
	# POST /keyboard_shortcuts.yaml
	def create
		@keyboard_shortcut = current_user.keyboard_shortcuts.new(params[:keyboard_shortcut])

		respond_to do |format|
			if @keyboard_shortcut.save
				format.html { redirect_to(@keyboard_shortcut, :notice => 'Keyboard shortcut was successfully created.') }
				format.xml  { render :xml => @keyboard_shortcut, :status => :created, :location => @keyboard_shortcut }
				format.json { render :json => @keyboard_shortcut, :status => :created, :location => @keyboard_shortcut }
				format.yaml { render :text => @keyboard_shortcut.to_yaml, :content_type => 'text/yaml', :status => :created, :location => @keyboard_shortcut }
			else
				format.html { render :action => "new" }
				format.xml  { render :xml => @keyboard_shortcut.errors, :status => :unprocessable_entity }
				format.json { render :json => @keyboard_shortcut.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @keyboard_shortcut.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# PUT /keyboard_shortcuts/1
	# PUT /keyboard_shortcuts/1.xml
	# PUT /keyboard_shortcuts/1.json
	# PUT /keyboard_shortcuts/1.yaml
	def update
		@keyboard_shortcut = current_user.keyboard_shortcuts.find(params[:id])

		respond_to do |format|
			if @keyboard_shortcut.update_attributes(params[:keyboard_shortcut])
				format.html { redirect_to(@keyboard_shortcut, :notice => 'Keyboard shortcut was successfully updated.') }
				format.xml  { head :ok }
				format.json { head :ok }
				format.yaml { render :text => "", :content_type => 'text/yaml' }
			else
				format.html { render :action => "edit" }
				format.xml  { render :xml => @keyboard_shortcut.errors, :status => :unprocessable_entity }
				format.json { render :json => @keyboard_shortcut.errors, :status => :unprocessable_entity }
				format.yaml { render :text => @keyboard_shortcut.errors.to_yaml, :content_type => 'text/yaml', :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /keyboard_shortcuts/1
	# DELETE /keyboard_shortcuts/1.xml
	# DELETE /keyboard_shortcuts/1.json
	# DELETE /keyboard_shortcuts/1.yaml
	def destroy
		@keyboard_shortcut = KeyboardShortcut.find(params[:id])
		@keyboard_shortcut.destroy

		respond_to do |format|
			format.html { redirect_to(keyboard_shortcuts_url) }
			format.xml  { head :ok }
			format.json { head :ok }
			format.yaml { render :text => "", :content_type => 'text/yaml' }
		end
	end
end
