# -*- encoding : utf-8 -*-
class SongsAlbums < ActiveRecord::Migration
  def up
    create_table :songs_albums, :id => false do |t|
      t.references :song, :album
    end
  end

  def down
	drop_table :songs_albums
  end
  
  def change
    remove_column :songs, :album_id
  end
end
