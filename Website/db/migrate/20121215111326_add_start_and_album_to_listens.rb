# -*- encoding : utf-8 -*-
class AddStartAndAlbumToListens < ActiveRecord::Migration
  def change
    add_column :listens, :album_id, :integer
    add_column :listens, :album_position, :integer
    add_column :listens, :started_at, :datetime
  end
end
