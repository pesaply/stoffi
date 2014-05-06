# -*- encoding : utf-8 -*-
class AlbumsArtists < ActiveRecord::Migration
  def up
    create_table :albums_artists, :id => false do |t|
      t.references :album, :artist
    end
  end

  def down
	drop_table :albums_artists
  end
  
  def change
    remove_column :albums, :artist_id
  end
end
