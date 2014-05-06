# -*- encoding : utf-8 -*-
# The model of the share resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describe a share of an object made by a user.
class Share < ActiveRecord::Base
	include Base
	
	# associations
	belongs_to :resource, :polymorphic => true
	belongs_to :playlist
	belongs_to :user
	belongs_to :device
	has_many :link_backlogs, :as => :resource, :dependent => :destroy
	
	# The string to display to users for representing the resource.
	def display
		object == "song" ? song.display : playlist.display
	end
	
	# The options to use when the share is serialized.
	def serialize_options
		{
			:include => [ object == "song" ? :song : :playlist ],
			:methods => [ :kind, :display, :url ]
		}
	end
end
