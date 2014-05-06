# -*- encoding : utf-8 -*-
# The model of the vote resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a vote on a translation by a user.
class Vote < ActiveRecord::Base
	belongs_to :translation
	belongs_to :user
end
