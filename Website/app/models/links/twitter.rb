# -*- encoding : utf-8 -*-
# The model of the link resource describing a link to a Twitter account.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

module Links::Twitter
	extend ActiveSupport::Concern
	
	def share_song_on_twitter(s, msg)
		# shorten message to allow for an url of size 29
		max = 110
		msg = msg[0..max-3] + "..." if msg.length > max
		msg += " #{s.resource.url}"
		
		logger.debug "sharing song on twitter: " + msg
		post("/1.1/statuses/update.json", :params =>
		{
			:status => msg
		})
	end
	
	def share_playlist_on_twitter(s, msg)
		# shorten message to allow for an url of size 29
		max = 110
		msg = msg[0..max-3] + "..." if msg.length > max
		msg += " #{s.resource.url}"
		
		logger.debug "sharing playlist on twitter: " + msg
		post("/1.1/statuses/update.json", :params =>
		{
			:status => msg
		})
	end
	
	def show_donation_on_twitter(d)
		msg = "I just donated $#{d.amount} to #{d.artist.name}"
		logger.debug "sharing donation on twitter: " + msg
		post("/1.1/statuses/update.json", :params =>
		{
			:status => msg
		})
	end
end
