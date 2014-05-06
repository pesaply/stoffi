# -*- encoding : utf-8 -*-
# The model of the translation resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describe a translation of a string.
class Translation < ActiveRecord::Base
	belongs_to :translatee, :foreign_key => :translatee_id, :class_name => Admin::Translatee
	belongs_to :language
	belongs_to :user
	has_many :votes
end
