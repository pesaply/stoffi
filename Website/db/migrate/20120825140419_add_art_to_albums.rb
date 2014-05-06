# -*- encoding : utf-8 -*-
class AddArtToAlbums < ActiveRecord::Migration
  def change
    add_column :albums, :art_url, :string
  end
end
