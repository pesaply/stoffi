# -*- encoding : utf-8 -*-
class AddSocialToArtists < ActiveRecord::Migration
  def change
    add_column :artists, :facebook, :string
    add_column :artists, :twitter, :string
    add_column :artists, :googleplus, :string
    add_column :artists, :myspace, :string
    add_column :artists, :spotify, :string
    add_column :artists, :youtube, :string
    add_column :artists, :soundcloud, :string
  end
end
