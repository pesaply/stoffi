# -*- encoding : utf-8 -*-
class TransformPathsInSongs < ActiveRecord::Migration
	def up
		Song.all.each do |song|
			if song.path.starts_with? "youtube://"
				id = song.path["youtube://".length .. -1]
				song.update_attribute(:path, "stoffi:track:youtube:#{id}")
			
			elsif song.path.starts_with? "soundcloud://"
				id = song.path["soundcloud://".length .. -1]
				song.update_attribute(:path, "stoffi:track:soundcloud:#{id}")
			
			end
		end
	end

	def down
		Song.all.each do |song|
			if song.path.starts_with? "stoffi:track:youtube:"
				id = song.path["stoffi:track:youtube:".length .. -1]
				song.update_attribute(:path, "youtube://#{id}")
			
			elsif song.path.starts_with? "stoffi:track:soundcloud:"
				id = song.path["stoffi:track:soundcloud:".length .. -1]
				song.update_attribute(:path, "soundcloud://#{id}")
			
			end
		end
	end
end
