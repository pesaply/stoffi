# -*- encoding : utf-8 -*-
class AddCreatePlaylistToLinks < ActiveRecord::Migration
  def change
    add_column :links, :do_create_playlist, :boolean, :default => true
  end
end
