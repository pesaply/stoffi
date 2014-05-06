# -*- encoding : utf-8 -*-
# The common model of all entities under the admin namespace.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# The module for administrative management.
module Admin
	
	# The prefix for all administrative tables in the database.
	def self.table_name_prefix
		'admin_'
	end
end
