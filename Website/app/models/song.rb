# -*- encoding : utf-8 -*-
# The model of the song resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describes a song in the database.
class Song < ActiveRecord::Base
	extend StaticBase
	include Base

	# associations
	has_and_belongs_to_many :albums, :uniq => true
	has_and_belongs_to_many :artists, :uniq => true
	has_and_belongs_to_many :users, :uniq => true
	has_and_belongs_to_many :playlists, :uniq => true
	has_many :listens
	has_many :shares, :as => :object
	
	# validations
	validates_presence_of :path
	
	scope :top5,
		select("songs.id, songs.title, songs.path, count(listens.id) AS listens_count").
		joins(:listens).
		group("songs.title").
		order("listens_count DESC").
		limit(5)
	
	# The URL of the song if it exists on an external service.
	def source
		if youtube?
			return "http://www.youtube.com/v/" + youtube_id + "?fs=1"
		else
			return ""
		end
	end
	
	# The art of the song.
	def picture(size = :medium)
		return "https://img.youtube.com/vi/" + youtube_id + "/default.jpg" if youtube?
		return art_url if (art_url.to_s != "" and art_url.to_s.downcase != "null")
		return "http://beta.stoffiplayer.com/assets/media/disc.png"
	end
	
	# The ID of the song on YouTube if it's on YouTube.
	def youtube_id
		if youtube?
			return path["stoffi:track:youtube:".length .. -1]
		else
			return ""
		end
	end
	
	# Whether or not the song is on YouTube.
	def youtube?
		return path && path.starts_with?("stoffi:track:youtube:")
	end
	
	# The ID of the song on SoundCloud if it's on SoundCloud.
	def soundcloud_id
		if soundcloud?
			return path["stoffi:track:soundcloud:".length .. -1]
		else
			return ""
		end
	end
	
	# Whether or not the song is on SoundCloud.
	def soundcloud?
		return path && path.starts_with?("stoffi:track:soundcloud:")
	end
	
	# A prettified description of the song.
	def pretty_name
		# TODO: internationalize
		s = title
		s += " by #{artist.name}" if artist
		return s
	end
	
	# The full name of the song, including the artist.
	def full_name
		s = title
		s = "#{artist.name} - #{s}" if artist
		return s
	end
	
	# The URL for opening the song in Stoffi Music Player.
	def play
		return "stoffi:track:youtube:#{youtube_id}" if youtube?
		return "stoffi:track:soundcloud:#{soundcloud_id}" if soundcloud?
		return url
	end
	
	# The artist of the song.
	def artist
		artists == nil ? nil : artists.first
	end
	
	# The album of the song.
	def album
		albums == nil ? nil : albums.first
	end
	
	# The string to display to users for representing the resource.
	def display
		title
	end
	
	# A long description of the song.
	def description
		s = "#{title}, a song "
		s+= "by #{artist.name} " if artist
		s+= "on Stoffi"
	end
	
	# The options to use when the song is serialized.
	def serialize_options
		{
			:methods => [ :kind, :display, :url, :picture ]
		}
	end
	
	# Searches for songs.
	def self.search(search, limit = 5)
		if search
			search = e(search)
			self.where("title LIKE ?", "%#{search}%").
			limit(limit)
		else
			scoped
		end
	end
	
	# Searches for songs which are files.
	def self.search_files(search, limit = 5)
		if search
			search = e(search)
			self.where("title LIKE ? AND path NOT LIKE 'stoffi:track:%'", "%#{search}%").
			limit(limit)
		else
			scoped
		end
	end
	
	# Returns a song matching a value.
	#
	# The value can be the ID (integer) or the name (string) of the song.
	# The song will be created if it is not found (unless <tt>value</tt> is an ID).
	def self.get(current_user, value)
		value = self.find(value) if value.is_a?(Integer)
		if value.is_a?(Hash)
			p = value
			
			if p.key? :path
				if p[:path].starts_with? "youtube://"
					id = p[:path]["youtube://".length .. -1]
					p[:path] = "stoffi:track:youtube:#{id}"
				
				elsif p[:path].starts_with? "soundcloud://"
					id = p[:path]["soundcloud://".length .. -1]
					p[:path] = "stoffi:track:soundcloud:#{id}"
					
				end
			end
			
			value = self.get_by_path(p[:path])
			
			# try to get local file by looking for path and length.
			# however, length is a float so we use a +- 0.01 interval to match lenth
			unless value.is_a? Song
				l = p[:length].to_f
				value = self.where(path: p[:path]).where("#{l-0.01} < length and length < #{l+0.01}").first
			end
			
			unless value.is_a?(Song)
			
				# fix artist, album objects
				if p[:artist].to_s != ""
					logger.debug "trying to find matching artist: #{p[:artist]}"
					artist = Artist.get(p[:artist])
					p.delete(:artist)
				end
				if p[:album].to_s != ""
					album = Album.get(p[:album])
					p.delete(:album)
				end
				
				# fix params
				p[:length] = p[:length].to_f if p[:length]
				p[:score] = p[:score].to_i if p[:score]
				
				value = Song.new
				
				value.art_url = p[:art_url] if p[:art_url]
				value.foreign_url = p[:foreign_url] if p[:foreign_url]
				value.length = p[:length] if p[:length]
				value.score = p[:score] if p[:score]
				value.title = p[:title] if p[:title]
				value.path = p[:path] if p[:path]
				
				if value.save
					value.artists << artist if artist and artist.songs.find_all_by_id(value.id).count == 0
					value.albums << album if album and albums.songs.find_all_by_id(value.id).count == 0
					artist.albums << album if artist and album and artist.albums.find_all_by_id(album.id).count == 0
				end
			end
		end
		if current_user and value.is_a?(Song) and current_user.songs.find_by_id(value.id) == nil
			current_user.songs << value
		end
		return value if value.is_a?(Song)
		return nil
	end
	
	# Finds a song given its path.
	#
	# If no song is found but exists on an external service then it will be created and saved to the database.
	def self.get_by_path(path)
		begin
			song = nil
			if path.start_with? "stoffi:track:" or path.start_with? "http://" or path.start_with? "https://"
				song = find_by_path(path)
			end
			unless song
				if path.start_with? "stoffi:track:youtube:"
					song = create(:path => path)
					
					http = Net::HTTP.new("gdata.youtube.com", 443)
					http.use_ssl = true
					data = http.get("/feeds/api/videos/#{song.youtube_id}?v=2&alt=json", {})
					feed = JSON.parse(data.body)
					
					artist, title = parse_title(feed['entry']['title']['$t'])
					artist = feed['entry']['author'][0]['name']['$t'] if artist.to_s == ''
					artist = Artist.get(artist) if artist.to_s != ''
					
					id = feed['entry']['media$group']['yt$videoid']['$t']
					
					song.foreign_url = "https://www.youtube.com/watch?v=#{id}"
					song.title = title
					song.length = feed['entry']['media$group']['yt$duration']['seconds']
					song.art_url = feed['entry']['media$group']['media$thumbnail'][0]['url']
					
					artist = Artist.get(artist)
					
					song.artists << artist if artist
			
				elsif path.start_with? "stoffi:track:soundcloud:"
					song = create(:path => path)
					
					client_id = "2ad7603ebaa9cd252eabd8dd293e9c40"
					http = Net::HTTP.new("api.soundcloud.com", 443)
					http.use_ssl = true
					data = http.get("/tracks/#{song.soundcloud_id}.json?client_id=#{client_id}", {})
					track = JSON.parse(data.body)
					
					artist, title = parse_title(track['title'])
					artist = track['user']['username'] if artist.to_s == ''
					artist = Artist.get(artist) if artist.to_s != ''
					
					song.foreign_url = track['permalink_url']
					song.title = title
					song.length = track['duration'].to_f / 1000.0
					song.genre = track['genre']
					song.art_url = track['artwork_url']
					
					song.artists << artist if artist
				end
				
			end
			song.save if song
			return song
		rescue
			return nil
		end
	end
	
	# Returns a top list of songs with most plays.
	#
	# If <tt>user</tt> is supplied then only listens of that user will be considered.
	def self.top(limit = 5, user = nil)
		self.select("songs.id, songs.title, songs.art_url, songs.path, count(listens.id) AS listens_count").
		joins("LEFT JOIN listens ON listens.song_id = songs.id").
		where(user == nil ? "" : "listens.user_id = #{user.id}").
		group("songs.id").
		order("listens_count DESC").
		limit(limit)
	end
	
	# Extracts the title and artists from a string.
	def self.parse_title(str)
		
		artist, title = split_title(str)
		
		# remove enclosings
		["'.*'", "\".*\"", "\\(.*\\)", "\\[.*\\]"].each do |e|
			artist = artist[1..-2] if artist.match(e)
			title = title[1..-2] if title.match(e)
		end
		
		# trim start and end
		chars = "-_\\s"
		artist.gsub!(/\A[#{chars}]+|[#{chars}]+\Z/, "")
		title.gsub!(/\A[#{chars}]+|[#{chars}]+\Z/, "")
		
		return artist, title
	end
	
	private
	
	def self.split_title(str)
		# remove meta phrases
		meta = ["official video", "lyrics", "with lyrics",
		"hq", "hd", "official", "official audio", "alternate official video"]
		meta = meta.map { |x| ["\\("+x+"\\)","\\["+x+"\\]"] }.flatten
		meta << "official video"
		meta.each { |m| str = str.gsub(/#{m}/i, "") }
		
		# remove multi whitespace
		str = str.split.join(" ")
		
		# split on - : ~ by
		separators = []
		["-", ":", "~"].each {|s| separators.concat [" "+s, s+" ", " "+s+" "]}
		separators.map! { |s| [s, true] } # true is for left=artist
		separators << [", by ", false] # false is for right=artist
		separators << [" by ", false]
		separators.each do |sep|
			next unless str.include? sep[0]
			
			s = str.split(sep[0], 2)
			artist = s[sep[1]?0:1]
			title = s[sep[1]?1:0]
			
			# stuff that gives us a hint that the string is an artist
			["by ", "ft ", "ft.", "feat ", "feat.", "with "].each do |artistText|
				
				# remove prefix
				if artist.downcase.starts_with? artistText
					return artist[artistText.length..-1], title
					
				# swap and remove prefix
				elsif title.downcase.starts_with? artistText
					return title, artist[artistText.length..-1]
					
				# swap
				elsif title.downcase.include?(" "+artistText)
					return title, artist
				end
			end
			
			return artist, title
		end
		
		# title in quotes
		# ex: Eminem "Not Afraid"
		t = "(\'(?<title>.+)\'|\"(?<title>.+)\")"
		a = "(?<artist>.+)"
		p = "(#{t}\\s+#{a}|#{a}\\s+#{t})"
		m = str.match(p)
		return m[:artist], m[:title] if m
		
		return "", str
	end
end
