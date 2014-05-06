# -*- encoding : utf-8 -*-
class AddLastfmToArtists < ActiveRecord::Migration
  def change
    add_column :artists, :lastfm, :string
  end
end
