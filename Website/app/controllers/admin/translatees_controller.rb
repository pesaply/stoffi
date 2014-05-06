# -*- encoding : utf-8 -*-
class Admin::TranslateesController < ApplicationController
	# GET /admin/translatees
	# GET /admin/translatees.json
	def index
		@admin_translatees = Admin::Translatee.all

		respond_to do |format|
			format.html # index.html.erb
			format.json { render :json => @admin_translatees }
		end
	end

	# GET /admin/translatees/1
	# GET /admin/translatees/1.json
	def show
		@admin_translatee = Admin::Translatee.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.json { render :json => @admin_translatee }
		end
	end

	# GET /admin/translatees/new
	# GET /admin/translatees/new.json
	def new
		@admin_translatee = Admin::Translatee.new

		respond_to do |format|
			format.html # new.html.erb
			format.json { render :json => @admin_translatee }
		end
	end

	# GET /admin/translatees/1/edit
	def edit
		@admin_translatee = Admin::Translatee.find(params[:id])
	end

	# POST /admin/translatees
	# POST /admin/translatees.json
	def create
		@admin_translatee = Admin::Translatee.new(params[:admin_translatee])

		respond_to do |format|
			if @admin_translatee.save
				format.html { redirect_to @admin_translatee, :notice => 'Translatee was successfully created.' }
				format.json { render :json => @admin_translatee, :status => :created, :location => @admin_translatee }
			else
				format.html { render :action => "new" }
				format.json { render :json => @admin_translatee.errors, :status => :unprocessable_entity }
			end
		end
	end

	# PUT /admin/translatees/1
	# PUT /admin/translatees/1.json
	def update
		@admin_translatee = Admin::Translatee.find(params[:id])

		respond_to do |format|
			if @admin_translatee.update_attributes(params[:admin_translatee])
				format.html { redirect_to @admin_translatee, :notice => 'Translatee was successfully updated.' }
				format.json { head :ok }
			else
				format.html { render :action => "edit" }
				format.json { render :json => @admin_translatee.errors, :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /admin/translatees/1
	# DELETE /admin/translatees/1.json
	def destroy
		@admin_translatee = Admin::Translatee.find(params[:id])
		@admin_translatee.destroy

		respond_to do |format|
			format.html { redirect_to admin_translatees_url }
			format.json { head :ok }
		end
	end
end
