# -*- encoding : utf-8 -*-
class CreateAlbums < ActiveRecord::Migration
  def self.up
    create_table :albums do |t|
      t.string :title
      t.integer :year
	  t.integer :artist_id
      t.string :description

      t.timestamps
    end
  end

  def self.down
    drop_table :albums
  end
end
