# -*- encoding : utf-8 -*-
module PlaylistsHelper
	def playlist_item(playlist, options = {})
		@playlist = playlist
		render :partial => "playlists/item"
	end
end
