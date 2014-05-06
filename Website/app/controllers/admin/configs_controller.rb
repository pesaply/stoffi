# -*- encoding : utf-8 -*-
class Admin::ConfigsController < ApplicationController
  # GET /admin/configs
  # GET /admin/configs.json
  def index
    @admin_configs = Admin::Config.all

    respond_to do |format|
      format.html # index.html.erb
      format.json { render :json => @admin_configs }
    end
  end

  # GET /admin/configs/1
  # GET /admin/configs/1.json
  def show
    @admin_config = Admin::Config.find(params[:id])

    respond_to do |format|
      format.html # show.html.erb
      format.json { render :json => @admin_config }
    end
  end

  # GET /admin/configs/new
  # GET /admin/configs/new.json
  def new
    @admin_config = Admin::Config.new

    respond_to do |format|
      format.html # new.html.erb
      format.json { render :json => @admin_config }
    end
  end

  # GET /admin/configs/1/edit
  def edit
    @admin_config = Admin::Config.find(params[:id])
  end

  # POST /admin/configs
  # POST /admin/configs.json
  def create
    @admin_config = Admin::Config.new(params[:admin_config])

    respond_to do |format|
      if @admin_config.save
        format.html { redirect_to @admin_config, :notice => 'Config was successfully created.' }
        format.json { render :json => @admin_config, :status => :created, :location => @admin_config }
      else
        format.html { render :action => "new" }
        format.json { render :json => @admin_config.errors, :status => :unprocessable_entity }
      end
    end
  end

  # PUT /admin/configs/1
  # PUT /admin/configs/1.json
  def update
    @admin_config = Admin::Config.find(params[:id])

    respond_to do |format|
      if @admin_config.update_attributes(params[:admin_config])
        format.html { redirect_to @admin_config, :notice => 'Config was successfully updated.' }
        format.json { head :ok }
      else
        format.html { render :action => "edit" }
        format.json { render :json => @admin_config.errors, :status => :unprocessable_entity }
      end
    end
  end

  # DELETE /admin/configs/1
  # DELETE /admin/configs/1.json
  def destroy
    @admin_config = Admin::Config.find(params[:id])
    @admin_config.destroy

    respond_to do |format|
      format.html { redirect_to admin_configs_url }
      format.json { head :ok }
    end
  end
end
