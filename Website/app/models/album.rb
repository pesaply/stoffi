# -*- encoding : utf-8 -*-
# The model of the album resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describes an album, created by one or more artists, containing songs.
class Album < ActiveRecord::Base
	include Base
	
	# associations
	has_and_belongs_to_many :artist, :uniq => true
	has_and_belongs_to_many :songs, :uniq => true
	
	# Returns an album matching a value.
	#
	# The value can be the ID (integer) or the name (string) of the artist.
	# The artist will be created if it is not found (unless <tt>value</tt> is an ID).
	def self.get(value)
		value = find(value) if value.is_a?(Integer)
		value = find_or_create_by_title(value) if value.is_a?(String)
		return value if value.is_a?(Playlist)
		return nil
	end
	
	# The string to display to users for representing the resource.
	def display
		title
	end
	
	# Paginates the songs of the album. Should be called before <tt>paginated_songs</tt> is called.
	#
	#   album.paginate_songs(10, 30)
	#   songs = album.paginated_songs # songs will now hold the songs 30-39 (starting from 0)
	def paginate_songs(limit, offset)
		@paginated_songs = Array.new
		songs.limit(limit).offset(offset).each do |song|
			@paginated_songs << song
		end
	end
	
	# Returns a slice of the album's songs which was created by <tt>paginated_songs</tt>.
	#
	#   album.paginate_songs(10, 30)
	#   songs = album.paginated_songs # songs will now hold the songs 30-39 (starting from 0)
	def paginated_songs
		return @paginated_songs
	end
end
