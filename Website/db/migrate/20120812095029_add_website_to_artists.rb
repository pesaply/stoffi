# -*- encoding : utf-8 -*-
class AddWebsiteToArtists < ActiveRecord::Migration
  def change
    add_column :artists, :website, :string
  end
end
