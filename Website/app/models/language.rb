# -*- encoding : utf-8 -*-
# The model of the language resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a language to which Stoffi has been (at least partly) translated.
class Language < ActiveRecord::Base
	has_many :translations

	# The flag of the language.
	def flag
		"/assets/flags/#{iso_tag}.png"
	end
end
