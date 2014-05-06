# -*- encoding : utf-8 -*-
class GlobalObserver < ActiveRecord::Observer
	observe :playlist, :artist, :song, :device, :album, :share, :user, :donation
	
	# Called before a record is created in the database.
	def before_save(record)
		clean_strings(record)
	end
	
	# Called before a record is created in the database.
	def before_create(record)
		clean_strings(record)
	end
	
	# Called before a record is updated in the database.
	def before_update(record)
		clean_strings(record)
	end
	
	# Decode the user controlled strings in the record.
	def clean_strings(record)
		case record
			when Artist
				record.name = e(record.name)
				
			when Playlist
				record.name = e(record.name)
			
			when Song
				record.title = e(record.title)
				record.genre = e(record.genre)
				
			when Album
				record.title = e(record.title)
				
			when Share
				record.message = e(record.message)
				
			when Donation
				record.message = e(record.message)
				
			when ClientApplication
				record.name = e(record.name)
				record.website = e(record.website)
				record.support_url = e(record.support_url)
				record.description = e(record.description)
				record.author = e(record.author)
				record.author_url = e(record.author_url)
				
			when User
				record.custom_name = e(record.custom_name)
		end
		true
	end
	
	# Decode a string for presentation.
	def d(str)
		return str unless str.is_a? String
		
		next_str = HTMLEntities.new.decode(str)
		while (next_str != str)
			str = next_str
			next_str = HTMLEntities.new.decode(str)
		end
		return next_str
	end
	
	# Encodes a string so it can be stored and transmitted.
	#
	# The string is first decoded until it doesn't change
	# anything and then a single encoding is performed.
	def e(str)
		return unless str
		
		if str.is_a? String
			str = HTMLEntities.new.encode(d(str), :decimal)
			str.gsub!(/[']/, "&#39;")
			str.gsub!(/["]/, "&#34;")
			str.gsub!(/[\\]/, "&#92;")
			return str
		end
		
		return str.map { |s| e(s) } if str.is_a?(Array)
		return str.each { |a,b| str[a] = e(b) } if str.is_a?(Hash)
		return str
	end
end
