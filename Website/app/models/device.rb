# -*- encoding : utf-8 -*-
# The model of the device resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describe a device used to access the API.
class Device < ActiveRecord::Base
	include Base
	extend StaticBase
	
	# associations
	belongs_to :configuration
	belongs_to :app, :foreign_key => :client_application_id, :class_name => "ClientApplication"
	belongs_to :user
	
	attr_accessible :name, :version, :configuration_id
	
	# Whether or not the device is currently online.
	def online?
		status == "online"
	end
	
	# The string to display to users for representing the resource.
	def display
		name
	end
	
	# Updates the last access of the device.
	def poke(app, ip)
		update_attribute(:client_application_id, app.id) if app
		update_attribute(:last_ip, ip) if ip
	end
	
	
	# The options to use when the device is serialized.
	def serialize_options
		{
			:except => :last_ip,
			:methods => [ :kind, :display, :url ]
		}
	end
	
	# Searches for devices.
	#
	# TODO: scope for current_user
	def self.search(search, limit = 5)
		if search
			search = e(search)
			self.where("name LIKE ?", "%#{search}%").
			limit(limit)
		else
			scoped
		end
	end
end
