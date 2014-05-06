# -*- encoding : utf-8 -*-
# The observer of the link backlog resources.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes an observer which watches the link backlog resources.
class LinkBacklogObserver < ActiveRecord::Observer
	
	# Called when a new link backlog has been created and saved to the database.
	def after_save(record)
		channel = "hash_#{record.link.user.unique_hash}"
		error = record.error.gsub("\"", "&quot;").gsub("'", "&#39;")
		cmd = "linkError(#{record.link.id}, '#{error}');"
		Juggernaut.publish(channel, cmd)
	end
	
end
