# -*- encoding : utf-8 -*-
class History < ActiveRecord::Base
	has_many :histories_tracks
	has_many :tracks, :through => :histories_tracks
	belongs_to :configuration
end
