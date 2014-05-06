# -*- encoding : utf-8 -*-
class TranslationsController < ApplicationController
	respond_to :html, :mobile, :embedded, :json, :xml

	# GET /translations
	# GET /translations.json
	def index
		@translations = Translation.all
		respond_with @translations
	end

	# GET /translations/1
	# GET /translations/1.json
	def show
		@translation = Translation.find(params[:id])
		respond_with @translation
	end

	# GET /translations/new
	# GET /translations/new.json
	def new
		@translation = Translation.new
		respond_with @translation
	end

	# GET /translations/1/edit
	def edit
		@translation = Translation.find(params[:id])
	end

	# POST /translations
	# POST /translations.json
	def create
		@translation = Translation.new(params[:translation])
		@translation.user = current_user

		respond_with @translation do |format|
			if @translation.save
				format.html { redirect_to @translation.language, :notice => 'Translation was successfully created.' }
				format.json { render :json => @translation, :status => :created, :location => @translation }
			else
				format.html { render :action => "new" }
				format.json { render :json => @translation.errors, :status => :unprocessable_entity }
			end
		end
	end

	# PUT /translations/1
	# PUT /translations/1.json
	def update
		@translation = Translation.find(params[:id])

		respond_with @translation do |format|
			if @translation.update_attributes(params[:translation])
				format.html { redirect_to @translation, :notice => 'Translation was successfully updated.' }
				format.json { head :ok }
			else
				format.html { render :action => "edit" }
				format.json { render :json => @translation.errors, :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /translations/1
	# DELETE /translations/1.json
	def destroy
		@translation = Translation.find(params[:id])
		@translation.destroy

		respond_with @translation do |format|
			format.html { redirect_to translations_url }
			format.json { head :ok }
		end
	end
end
