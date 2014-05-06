# -*- encoding : utf-8 -*-
# The model of the synchronization configuration resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describes a configuration of a client.
class Configuration < ActiveRecord::Base
	include Base
	
	# associations
	belongs_to :user
	has_many :tracks
	has_many :playlists
	has_many :keyboard_shortcut_profiles
	has_many :equalizer_profiles
	has_many :devices
	belongs_to :current_track, :class_name => 'Song'
	belongs_to :current_shortcut_profile, :class_name => 'KeyboardShortcutProfile'
	belongs_to :current_equalizer_profile, :class_name => 'EqualizerProfile'
	
	# The text to display on the repeat button.
	def repeat_text
		case repeat
			when "NoRepeat" then I18n.t("media.repeat.disabled")
			when "RepeatAll" then I18n.t("media.repeat.all")
			else I18n.t("media.repeat.one")
		end
	end
	
	# The text to display on the shuffle button.
	def shuffle_text
		case shuffle
			when "Random" then I18n.t("media.shuffle.random")
			when "MindReader" then I18n.t("media.shuffle.mind_reader")
			else I18n.t("media.shuffle.disabled")
		end
	end
	
	# The state of the play/pause button.
	def media_button
		case media_state
			when "Playing" then "pause"
			else "play"
		end
	end
	
	# The text describing what's currently being played.
	def now_playing
		current_track ? current_track.full_name : I18n.t("media.nothing_playing")
	end
	
	# The string to display to users for representing the resource.
	def display
		name
	end
	
	# The options to use when the configuration is serialized.
	def serialize_options
		{
			:include => [ :current_track ],
			:methods => [ :kind, :display, :url ]
		}
	end
end
