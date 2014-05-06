# -*- encoding : utf-8 -*-
# The model of the link resource describing a link to a Facebook account.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# The module containing methods for interacting with the Facebook OAuth API.
module Links::Facebook
	extend ActiveSupport::Concern
	
	# Share a song on Facebook.
	def share_song_on_facebook(s, msg)
		logger.info "share song on facebook"
		
		l = s.resource.url
		n = s.resource.title
		c = "by #{s.resource.artist.name}"
		o = s.resource.source
		p = s.resource.picture
	
		post('/me/feed', :params =>
		{
			:message => msg,
			:link => l,
			:name => n,
			:caption => c,
			:source => o,
			:picture => p
		})
	end
	
	# Share a playlist on Facebook.
	def share_playlist_on_facebook(s, msg)
		logger.info "share playlist on facebook"
		
		p = s.resource
		return unless p
		l = p.url
		n = p.name
		c = "A playlist on Stoffi"
		i = p.picture
		
		post('/me/feed', :params =>
		{
			:message => msg,
			:link => l,
			:name => n,
			:caption => c,
			:picture => i
		})
	end
	
	# Let Facebook know that the user has started to play a song.
	def start_listen_on_facebook(l)
		params = { 
			:song => l.song.url, 
			:end_time => l.ended_at, 
			:start_time => l.created_at
		}
		if l.playlist
			params[:playlist] = l.playlist.url
		#elsif l.album
		#	params[:album] = l.album.url
		end
		logger.debug "starting listen on facebook"
		logger.debug params.inspect
		post('/me/music.listens', :params => params)
	end
	
	# Let Facebook know that the user has paused, resumed or otherwise update the current listen of a song.
	def update_listen_on_facebook(l)
		id = find_listen_by_url(l.song.url)
		if id == nil
			start_listen_on_facebook(l)
		else
			if l.ended_at - l.created_at < 15.seconds
				logger.debug "deleting listen on facebook"
				logger.debug id
				delete('/' + id)
			else
				params = { :end_time => l.ended_at }
				logger.debug "updating listen on facebook"
				logger.debug params.inspect
				post("/#{id}", :params => params)
			end
		end
	end
	
	# Let Facebook know that the user has paused, resumed or otherwise update the current listen of a song.
	def end_listen_on_facebook(l)
		id = find_listen_by_url(l.song.url)
		if id != nil
			logger.debug "#{l.ended_at - l.created_at} < #{15.seconds}"
			if l.ended_at - l.created_at < 15.seconds
				logger.debug "deleting listen on facebook"
				logger.debug id
				delete('/' + id)
			else
				params = { :end_time => l.ended_at }
				logger.debug "ending listen on facebook"
				logger.debug params.inspect
				post("/#{id}", :params => params)
			end
		end
	end
	
	# Remove the listen of a song from Facebook.
	#
	# This is mostly used when the user skipped a song or didn't play it long enough.
	def delete_listen_on_facebook(l)
		id = find_listen_by_url(l.song.url)
		logger.debug "deleting listen on facebook"
		logger.debug id
		delete('/' + id) unless id == nil
	end
	
	def show_donation_on_facebook(d)
		post('/me/stoffiplayer:support', :params =>
		{
			:artist => d.artist.url,
			:amount => '%.5f' % d.amount,
			:charity => '%.5f' % d.charity,
			:currency => d.currency
		})
	end
	
	def create_playlist_on_facebook(p)
		logger.debug "creating playlist on facebook"
		logger.debug p.url
		resp = post('/me/music.playlists', :params => { :playlist => p.url })
	end
	
	def update_playlist_on_facebook(p)
		id = find_playlist_by_url(p.url)
		if id == nil
			create_playlist_on_facebook(p)
		else
			logger.debug "scraping playlist on facebook"
			logger.debug id
			get("/?id=#{id}&scrape=true")
		end
	end
	
	def delete_playlist_on_facebook(p)
		id = find_playlist_by_url(p.url)
		logger.debug "deleting playlist on facebook"
		logger.debug id
		delete('/' + id) unless id == nil
	end
	
	def find_playlist_by_url(url)
		logger.debug "trying to find playlist on facebook"
		logger.debug url
		
		begin
			batch_size = 25
			offset = 0
			while true
				resp = get("/me/music.playlists?limit=#{batch_size}&offset=#{offset}")
				entries = resp['data']
				
				if entries.size == 0
					logger.warn "could not find playlist on facebook"
					return nil
				end
				
				entries.each do |entry|
					if entry['application']['id'] == creds[:id] and entry['data']['playlist']['url'].starts_with? url
						id = entry['id']
						logger.debug "found playlist: #{id}"
						return id
					end
				end
				
				offset += batch_size
			end
			
		rescue Exception => e
			logger.warn "could not find playlist '#{url}' on facebook"
			logger.warn e.inspect
		end
		
		return nil
	end
	
	def find_listen_by_url(url)
		logger.debug "trying to find listen on facebook"
		logger.debug url
		
		begin
			batch_size = 25
			offset = 0
			# facebook got weird urls, this is a temporary hack until they leave fb's cache
			messed_url = url.gsub("stoffiplayer.com/songs", "stoffiplayer.com/--/songs")
			while true
				resp = get("/me/music.listens?limit=#{batch_size}&offset=#{offset}")
				entries = resp['data']
				
				if entries.size == 0
					logger.warn "could not find listen on facebook: reached end"
					return nil
				end
				
				entries.each do |entry|
					if entry['application']['id'] == creds[:id] and (entry['data']['song']['url'].starts_with? url or entry['data']['song']['url'].starts_with? messed_url)
						id = entry['id']
						logger.debug "found listen: #{id}"
						return id
					end
				end
				
				offset += batch_size
			end
			
		rescue Exception => e
			logger.warn "could not find listen '#{url}' on facebook"
			logger.warn e.inspect
		end
		
		return nil
	end
	
	def fetch_encrypted_facebook_uid
		resp = get("/dmp?fields=third_party_id")
		update_attribute(:encrypted_uid, resp['third_party_id'])
	end
end
