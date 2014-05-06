# -*- encoding : utf-8 -*-
# The model of the translatee_param resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a parameter which can be part of a translatable string.
#
# The parameter will be replaced during the translation with either a fixed value (date, username, etc)
# or by another translated string.
class Admin::TranslateeParam < ActiveRecord::Base
	has_and_belongs_to_many :translatees
end
