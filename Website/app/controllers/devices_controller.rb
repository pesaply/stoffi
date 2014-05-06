# -*- encoding : utf-8 -*-
# The business logic for devices.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class DevicesController < ApplicationController

	oauthenticate
	respond_to :html, :mobile, :xml, :json
	
	# GET /devices
	def index
		l, o = pagination_params
		@title = "Your devices"
		@description = "The devices that are connected to your account"
		@devices = current_user.devices.limit(l).offset(o)
		respond_with @devices
	end

	# GET /devices/1
	def show
		@device = current_user.devices.find(params[:id])
		
		country = origin_country(@device.last_ip)
		city = origin_city(@device.last_ip)
		network = origin_network(@device.last_ip)
		
		@flag = flag(country)
		@country = country_name(country)
		@country_code = country_code(country)
		@city = city_name(city)
		@longitude = longitude(city)
		@latitude = latitude(city)
		@network = asn(network)
		
		@title = "Device '#{d(@device.name)}'"
		@description = "The device named #{d(@device.name)} which is connected to your account"
		respond_with @device
	end

	# GET /devices/new
	def new
		@device = current_user.devices.new
		respond_with @device
	end

	# GET /devices/1/edit
	def edit
		@device = current_user.devices.find(params[:id])
		@title = "Edit device '#{d(@device.name)}'"
		@description = "Modify the device named #{d(@device.name)} which is connected to your account"
	end

	# POST /devices
	def create
		@device = current_user.devices.new(params[:device])
		@device.status = 'online'
		if @device.save
			@device.poke(current_client_application, request.ip)
			SyncController.send('create', @device, request)
		end
		respond_with(@device)
	end

	# PUT /devices/1
	def update
		@device = current_user.devices.find(params[:id])
		success = @device.update_attributes(params[:device])
		SyncController.send('update', @device, request) if success
		respond_with(@device)
	end

	# DELETE /devices/1
	def destroy
		@device = current_user.devices.find(params[:id])
		SyncController.send('delete', @device, request)
		@device.destroy
		respond_with(@device)
	end
	
	private
	
	def flag(country)
		code = country_code(country)
		code = "xx" if code.to_s == "" or code.to_s == "--"
		"/assets/flags/#{code.downcase}.png"
	end
	
	def country_code(country)
		return country.country_code2 if country
		""
	end
	
	def country_name(country)
		return country.country_name if country
		"N/A"
	end
	
	def city_name(city)
		return city.city_name if city
		"N/A"
	end
	
	def longitude(city)
		return city.longitude if city
		0
	end
	
	def latitude(city)
		return city.latitude if city
		0
	end
	
	def asn(network)
		return network.asn if network
		"N/A"
	end
end
