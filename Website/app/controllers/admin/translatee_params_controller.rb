# -*- encoding : utf-8 -*-
class Admin::TranslateeParamsController < ApplicationController
	# GET /admin/translatee_params
	# GET /admin/translatee_params.json
	def index
		@admin_translatee_params = Admin::TranslateeParam.all

		respond_to do |format|
			format.html # index.html.erb
			format.json { render :json => @admin_translatee_params }
		end
	end

	# GET /admin/translatee_params/1
	# GET /admin/translatee_params/1.json
	def show
		@admin_translatee_param = Admin::TranslateeParam.find(params[:id])

		respond_to do |format|
			format.html # show.html.erb
			format.json { render :json => @admin_translatee_param }
		end
	end

	# GET /admin/translatee_params/new
	# GET /admin/translatee_params/new.json
	def new
		@admin_translatee_param = Admin::TranslateeParam.new

		respond_to do |format|
			format.html # new.html.erb
			format.json { render :json => @admin_translatee_param }
		end
	end

	# GET /admin/translatee_params/1/edit
	def edit
		@admin_translatee_param = Admin::TranslateeParam.find(params[:id])
	end

	# POST /admin/translatee_params
	# POST /admin/translatee_params.json
	def create
		@admin_translatee_param = Admin::TranslateeParam.new(params[:admin_translatee_param])

		respond_to do |format|
			if @admin_translatee_param.save
				format.html { redirect_to @admin_translatee_param, :notice => 'Translatee parameter was successfully created.' }
				format.json { render :json => @admin_translatee_param, :status => :created, :location => @admin_translatee_param }
			else
				format.html { render :action => "new" }
				format.json { render :json => @admin_translatee_param.errors, :status => :unprocessable_entity }
			end
		end
	end

	# PUT /admin/translatee_params/1
	# PUT /admin/translatee_params/1.json
	def update
		@admin_translatee_param = Admin::TranslateeParam.find(params[:id])

		respond_to do |format|
			if @admin_translatee_param.update_attributes(params[:admin_translatee_param])
				format.html { redirect_to @admin_translatee_param, :notice => 'Translatee parameter was successfully updated.' }
				format.json { head :ok }
			else
				format.html { render :action => "edit" }
				format.json { render :json => @admin_translatee_param.errors, :status => :unprocessable_entity }
			end
		end
	end

	# DELETE /admin/translatee_params/1
	# DELETE /admin/translatee_params/1.json
	def destroy
		@admin_translatee_param = Admin::TranslateeParam.find(params[:id])
		@admin_translatee_param.destroy

		respond_to do |format|
			format.html { redirect_to admin_translatee_params_url }
			format.json { head :ok }
		end
	end
end
