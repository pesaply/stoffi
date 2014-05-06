# -*- encoding : utf-8 -*-
# The model of the donation resource.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

require 'base'

# Describes a donation made by a user to an artist.
class Donation < ActiveRecord::Base
	include Base
	
	# associations
	belongs_to :artist
	belongs_to :user
	has_many :link_backlogs, :as => :resource, :dependent => :destroy
	
	# Gets the list of artists to whom there are pending donations, ordered by the amount of donations which is pending.
	def self.pending_artists
		Artist.select("artists.id, artists.name, artists.picture, sum(donations.amount) AS donations_sum").
		where("donations.status = 'pending' AND donations.created_at < ?", Donation.revoke_time.ago).
		joins(:donations).
		group("artists.id").
		order("donations_sum DESC")
	end
	
	# The number of artists to whom there are pending donations.
	def self.pending_artists_count
		self.select("distinct(artist_id)").
		where("donations.status = 'pending' AND donations.created_at < ?", Donation.revoke_time.ago).
		count
	end
	
	# The artists whom have received donations, ordered by the amount of donations the artist has received.
	def self.artists
		Artist.select("artists.id, artists.name, artists.picture, sum(donations.amount) AS donations_sum").
		joins(:donations).
		group("artists.id").
		order("donations_sum DESC")
	end
	
	# Gets the list of pending donations.
	def self.pending_charity
		self.where("status = 'pending' AND created_at < ?", Donation.revoke_time.ago)
	end
	
	# Gets the total amount of charity from pending donations.
	def self.pending_charity_sum
		pending_charity.sum("amount * (charity_percentage / 100)").to_f.round(2)
	end
	
	# The duration for which a user can regret a donation.
	def self.revoke_time
		14.days
	end
	
	# The time left before the donation cannot be revoked anymore.
	def revoke_time
		((created_at + Donation.revoke_time - Time.now) / 86400).ceil
	end
	
	# The currency of the donation.
	def currency
		"USD"
	end
	
	# Whether or not the donation can be revoked.
	def revokable?
		status == "pending" && created_at >= Donation.revoke_time.ago
	end
	
	# The amount of the donation which should go to charity.
	def charity
		amount * (charity_percentage.to_f / 100)
	end
	
	# The amount of the donation which should go to the Stoffi project.
	def stoffi
		amount * (stoffi_percentage.to_f / 100)
	end
	
	# The amount of the donation which should go to the artist.
	def artist_share
		amount * (artist_percentage.to_f / 100)
	end
	
	# The string to display to users for representing the resource.
	def display
		""
	end
end
