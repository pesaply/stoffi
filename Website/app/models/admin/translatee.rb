# -*- encoding : utf-8 -*-
# The model of the translatee resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a string which affords translation by users.
class Admin::Translatee < ActiveRecord::Base
	has_many :translations
	has_and_belongs_to_many :parameters, :class_name => "TranslateeParam"
	
	validates_presence_of :name
	validates_presence_of :description
end
