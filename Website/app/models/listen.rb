# -*- encoding : utf-8 -*-
# The model of the listen resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describes a listen by a user to a song.
class Listen < ActiveRecord::Base
	include Base
	
	# associations
	belongs_to :user
	belongs_to :song
	belongs_to :playlist
	belongs_to :device
	#belongs_to :album
	has_many :link_backlogs, :as => :resource, :dependent => :destroy
	
	# The string to display to users for representing the resource.
	def display
		song.display
	end
	
	# The options to use when the listen is serialized.
	def serialize_options
		{
			:except => [ :device_id ],
			:include => [ :song, :playlist ],
			:methods => [ :kind, :display, :url ]
		}
	end
end
