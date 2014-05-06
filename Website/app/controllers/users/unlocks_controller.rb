# -*- encoding : utf-8 -*-
# The business logic for unlocking of user accounts.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class Users::UnlocksController < Devise::UnlocksController
	def new
		@title = t "unlock.title"
		@description = t "unlock.description"
		super
	end
end
