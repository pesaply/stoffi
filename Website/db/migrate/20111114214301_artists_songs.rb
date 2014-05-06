# -*- encoding : utf-8 -*-
class ArtistsSongs < ActiveRecord::Migration
  def self.up
    create_table :artists_songs, :id => false do |t|
      t.references :artist, :song
    end
  end

  def self.down
	drop_table :artists_songs
  end
end
