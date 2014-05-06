# -*- encoding : utf-8 -*-
class SongsArtists < ActiveRecord::Migration
  def up
    create_table :songs_artists, :id => false do |t|
      t.references :song, :artist
    end
  end

  def down
	drop_table :songs_artists
  end
end
