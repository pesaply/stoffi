# -*- encoding : utf-8 -*-
class LanguagesController < ApplicationController

	oauthenticate
	respond_to :html, :mobile, :embedded, :json, :xml
	
	# GET /languages
	# GET /languages.json
	def index
		@languages = Language.all
		@title = t("translate.title")
		respond_with @languages
	end

	# GET /languages/1
	# GET /languages/1.json
	def show
		@language = Language.find(params[:id])
		@translatees = Admin::Translatee.all
		@title = t("translate.show_title", :language => t("languages.#{@language.english_name.downcase}"))
		
		respond_with @language
	end

	# GET /languages/new
	# GET /languages/new.json
	def new
		@language = Language.new
		respond_with @language
	end

	# GET /languages/1/edit
	def edit
		@language = Language.find(params[:id])
	end

	# POST /languages
	# POST /languages.json
	def create
		@language = Language.new(params[:language])

		respond_with @language do |format|
			if @language.save
				format.html { redirect_to @language, :notice => 'Language was successfully created.' }
				format.json { render :json => @language, :status => :created, :location => @language }
			else
				format.html { render :action => "new" }
				format.json { render :json => @language.errors, :status => :unprocessable_entity }
			end
		end
	end

	# PUT /languages/1
	# PUT /languages/1.json
	def update
		@language = Language.find(params[:id])

		respond_with @language do |format|
			if @language.update_attributes(params[:language])
				format.html { redirect_to @language, :notice => 'Language was successfully updated.' }
				format.json { head :ok }
			else
				format.html { render :action => "edit" }
				format.json { render :json => @language.errors, :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /languages/1
	# DELETE /languages/1.json
	def destroy
		@language = Language.find(params[:id])
		@language.destroy

		respond_with @language do |format|
			format.html { redirect_to languages_url }
			format.json { head :ok }
		end
	end
	
	def english(translatee)
		english = Language.find_by_english_name("english")
		t = translatee.translations.find_by_language_id(english.id)
		return t == nil ? translatee.name : t.content
	end
	helper_method :english
end
