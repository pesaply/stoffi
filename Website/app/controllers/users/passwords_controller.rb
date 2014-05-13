# -*- encoding : utf-8 -*-
# The business logic for password management of accounts.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

class Users::PasswordsController < Devise::PasswordsController
	def edit
		u = User.find_by_reset_password_token(params[:reset_password_token])
		@email = u.email if u
		
		@title = t "reset.title"
		@description = t "reset.description"
		
		super
	end
	
	def new
		@title = t "forgot.title"
		@description = t "forgot.description"
		super
	end
end
