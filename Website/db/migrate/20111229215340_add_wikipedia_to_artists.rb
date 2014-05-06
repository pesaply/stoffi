# -*- encoding : utf-8 -*-
class AddWikipediaToArtists < ActiveRecord::Migration
  def change
    add_column :artists, :picture, :string
    add_column :artists, :description, :text
  end
end
