# -*- encoding : utf-8 -*-
class AddScoreToSongs < ActiveRecord::Migration
  def change
    add_column :songs, :score, :integer
  end
end
