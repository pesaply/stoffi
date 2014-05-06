# -*- encoding : utf-8 -*-
class CreateSongs < ActiveRecord::Migration
  def self.up
    create_table :songs do |t|
      t.string :title
      t.integer :album_id
      t.string :genre
      t.integer :track
      t.integer :year
      t.float :length
      t.string :path
      t.integer :play_count
      t.datetime :last_played
      t.integer :views
      t.datetime :last_write
      t.integer :bitrate
      t.integer :channels
      t.integer :sample_rate
      t.string :codecs
      t.string :source
	  t.string :description

      t.timestamps
    end
  end

  def self.down
    drop_table :songs
  end
end
