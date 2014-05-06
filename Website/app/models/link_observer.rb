# -*- encoding : utf-8 -*-
# The observer of the link resources.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes an observer which watches the link resources.
class LinkObserver < ActiveRecord::Observer
	
	# Called when a link has been updated.
	def after_update(record)
		# if the link has a new access_key then all backlogs for this link should be removed.
	end
	
end
