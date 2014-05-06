# -*- encoding : utf-8 -*-
# The model of the link resource describing a link to a Last.FM account.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'digest/md5'
require 'net/http'

module Links::Lastfm
	extend ActiveSupport::Concern
	
	def share_song_on_lastfm(s, msg)
		req('user.shout', :post, :user)
	end
	
	def share_playlist_on_lastfm
	end
	
	def show_donation_on_lastfm
	end
	
	def start_listen_on_lastfm(l)
		params =
		{
			:artist => l.song.artist.name,
			:track => l.song.title,
			:duration => l.song.length.to_i,
			:timestamp => l.created_at.to_i
		}
		#params[:album] = l.album.name if l.album
		
		resp = req('track.updateNowPlaying', :post, params)
		raise resp["message"] if resp["error"]
		logger.debug "submitted now playing to last.fm: " + params[:artist] + " - " + params[:track]
	end
	
	def end_listen_on_lastfm(l)
		params =
		{
			:artist => l.song.artist.name,
			:track => l.song.title,
			:duration => l.song.length.to_i,
			:timestamp => l.created_at.to_i
			#:timestamp => Time.now.to_i
		}
		#params[:album] = l.album.name if l.album
		
		resp = req('track.scrobble', :post, params)
		raise resp["message"] if resp["error"]
		logger.debug "submitted listen to last.fm: " + params[:artist] + " - " + params[:track]
	end
	
	def create_playlist_on_lastfm
	end
	
	def update_playlist_on_lastfm
	end
	
	def delete_playlist_on_lastfm
	end
	
	def req(method, verb, params = {})
		params[:api_key] = creds[:id]
		params[:method] = method
		params[:sk] = access_token
		
		if verb == :get
			params[:format] = "json"
		end
		
		params[:api_sig] = Digest::MD5.hexdigest(params.stringify_keys.sort.flatten.join + creds[:key])
		
		if verb == :post
			params[:format] = "json"
		end
		
		response = case verb
		when :get
			uri = URI.parse(creds[:url] + "/2.0/?" + params.to_query)
			Net::HTTP.get(uri)
		
		when :post
			uri = URI.parse(creds[:url] + "/2.0/")
			Net::HTTP.post_form(uri, params).body
		
		else
			raise "Unsupported HTTP Verb"
		end
		
		return JSON.parse(response)
	end
end
