# -*- encoding : utf-8 -*-
class AddArtAndUrlToSongs < ActiveRecord::Migration
  def change
    add_column :songs, :url, :string
    add_column :songs, :art_url, :string
  end
end
